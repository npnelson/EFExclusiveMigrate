using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Linq;

namespace NetToolBox.EFExclusiveMigrate.Core
{
    public class EnsureExclusiveMigration
    {
        private readonly DbContext _transactionContext;
        private readonly DbContext _migrationContext;
        private readonly ILogger<EnsureExclusiveMigration> _logger;

        public EnsureExclusiveMigration(DbContext transactionContext, DbContext migrationContext, ILogger<EnsureExclusiveMigration> logger)
        {
            _transactionContext = transactionContext;
            _migrationContext = migrationContext;
            _logger = logger;
        }

        public void ExclusiveMigrate()
        {

            //do we have any pending migrations?
            if (!_migrationContext.Database.GetPendingMigrations().Any())
            {
                _logger.LogInformation("No pending migrations detected, execution will resume normally");
                return;
            }

            _logger.LogInformation("Pending migrations found. Attempting to obtain exclusive lock on MigrationsHistory Table");

            using var dbTran = _transactionContext.Database.BeginTransaction(IsolationLevel.Serializable);
            _transactionContext.Database.ExecuteSqlRaw("select top 1 * from __EFMigrationsHistory WITH (XLOCK,ROWLOCK)"); //first migration must be applied manually, otherwise this will throw an exception
            //there is also an edge case that the attempt to grab the lock times out, if that happens, it will throw an exception and the app will need to be restarted, that could get ugly
            _logger.LogInformation("Exclusive lock obtained");

            if (!_migrationContext.Database.GetPendingMigrations().Any()) //check again in case another process was able to apply the migration
            {
                _logger.LogInformation("No Migrations Available, it must have been migrated by another process between the last check and the exclusive lock obtained by this process");
                dbTran.Rollback();
                return;
            }

            _migrationContext.Database.Migrate();
            _logger.LogInformation("Database successfully migrated");
            dbTran.Rollback(); //release the lock
        }
    }
}
