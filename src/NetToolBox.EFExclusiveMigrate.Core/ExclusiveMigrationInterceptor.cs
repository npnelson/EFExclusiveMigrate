using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetToolBox.EFExclusiveMigrate.Core
{
    public sealed class ExclusiveMigrationInterceptor : DbConnectionInterceptor
    {
        private static volatile bool _migrationSuccessfullyCheckedAndAppliedIfNeeded;

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            if (_migrationSuccessfullyCheckedAndAppliedIfNeeded)
            {
                //no op if the process has already checked/applied migrations
                return;
            }
            ILogger<ExclusiveMigrationInterceptor> logger = null!;

            try
            {
                logger = eventData.Context.GetService<ILogger<ExclusiveMigrationInterceptor>>();
                var dbContext = eventData.Context;
                logger.LogInformation("Checking to see if migrations are pending. If this command takes a long time, it could be because another process has already taken an exclusive lock to migrate");
                if (!dbContext.Database.GetPendingMigrations().Any())
                {
                    logger.LogInformation("No pending migrations detected, execution will resume normally");
                    _migrationSuccessfullyCheckedAndAppliedIfNeeded = true;
                    return;
                }

                logger.LogInformation("Pending migrations found. Attempting to obtain exclusive lock on MigrationsHistory Table");

                using var dbTran = dbContext.Database.BeginTransaction(IsolationLevel.Serializable);
                dbContext.Database.ExecuteSqlRaw("select top 1 * from __EFMigrationsHistory WITH (XLOCK,ROWLOCK)"); //first migration must be applied manually, otherwise this will throw an exception
                                                                                                                    //there is also an edge case that the attempt to grab the lock times out, if that happens, it will throw an exception and the app will need to be restarted, that could get ugly
                logger.LogInformation("Exclusive database lock obtained");

                var pendingMigrations = dbContext.Database.GetPendingMigrations();
                if (!pendingMigrations.Any()) //check again in case another process was able to apply the migration, although the exclusive lock obtained earlier should prevent anyone else from even checking
                {
                    logger.LogInformation("No Migrations Available, it must have been migrated by another process between the last check and the exclusive lock obtained by this process");
                    _migrationSuccessfullyCheckedAndAppliedIfNeeded = true;
                    dbTran.Rollback();
                    return;
                }
                var allMigrations = dbContext.Database.GetMigrations().ToArray();
                string startingMigration = string.Empty;
                for (var counter = allMigrations.Length - 1; counter != 0; counter = counter - 1)
                {
                    if (allMigrations[counter] == pendingMigrations.First())
                    {
                        startingMigration = allMigrations[counter - 1];
                    }
                }
                //https://github.com/dotnet/efcore/issues/6322#issuecomment-458555963 https://github.com/dotnet/efcore/issues/12325 --migrate still doesn't work quite right with transactions even in EF 5 - they will eventually fix it, but for now, we need to generate our own script
                //also watch out for ef core 5.0 rc1, we might need to do something different with the script https://github.com/dotnet/efcore/issues/7681

                var migrationSql = dbContext.GetService<IMigrator>().GenerateScript(startingMigration, pendingMigrations.Last(), false);

                //need this, because script contains "GO" statement for each migration that leads to execution error
                migrationSql = migrationSql.Replace("\r\nGO\r\n", "");

                migrationSql = migrationSql.Replace("\nGO\n", ""); //catch the linux variant for line endings

                dbContext.Database.ExecuteSqlRaw(migrationSql);
                logger.LogInformation("Database successfully migrated");
                _migrationSuccessfullyCheckedAndAppliedIfNeeded = true;
                dbTran.Commit(); //release the lock
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "Exception while running migration interceptor");

                throw;
            }
        }

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            //it's ok to run the async code synchronously since it should only run on the first connection for the process
            if (_migrationSuccessfullyCheckedAndAppliedIfNeeded)
            {
                //no op if the process has already checked/applied migrations
                return Task.CompletedTask;
            }

            ConnectionOpened(connection, eventData);
            return Task.CompletedTask;
        }
    }
}