using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetToolBox.EFExclusiveMigrate.Core;
using System;

namespace NetToolBox.EFExclusiveMigrate.AzureFunctions
{
    public class EFMigrationConfiguration<T> : IExtensionConfigProvider where T : DbContext
    {
        private readonly IServiceProvider _serviceProvider;

        public EFMigrationConfiguration(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            using var transactionScope = _serviceProvider.CreateScope();
            var transactionContext = transactionScope.ServiceProvider.GetRequiredService<T>();
            using var migrationScope = _serviceProvider.CreateScope();
            var migrationContext = migrationScope.ServiceProvider.GetRequiredService<T>();
            var logger = transactionScope.ServiceProvider.GetRequiredService<ILogger<EnsureExclusiveMigration>>();

            var migrator = new EnsureExclusiveMigration(transactionContext, migrationContext, logger);
            migrator.ExclusiveMigrate();
        }
    }
}
