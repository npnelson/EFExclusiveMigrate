using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetToolBox.EFExclusiveMigrate.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EnsureExclusiveMigrationExtensions
    {
        public static void EnsureExclusiveMigration<T>(this IApplicationBuilder app) where T : DbContext
        {
            using var transactionScope = app.ApplicationServices.CreateScope();
            var transactionContext = transactionScope.ServiceProvider.GetRequiredService<T>();
            using var migrationScope = app.ApplicationServices.CreateScope();
            var migrationContext = migrationScope.ServiceProvider.GetRequiredService<T>();
            var logger = transactionScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<EnsureExclusiveMigration>();

            var migrator = new EnsureExclusiveMigration(transactionContext, migrationContext, logger);
            migrator.ExclusiveMigrate();

        }
    }
}
