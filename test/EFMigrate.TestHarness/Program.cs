using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EFMigrate.TestHarness
{
    internal class Program
    {
        private async static Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(logging => logging.AddConsole());
            serviceCollection.AddDbContext<TestDbContext>(options => options.UseSqlServer("Server=(Localdb)\\MSSQLLocalDb;Database=TestDatabase;Integrated Security=true;").UseEFExclusiveMigrate());
            var sp = serviceCollection.BuildServiceProvider();
            var dbContext = sp.GetRequiredService<TestDbContext>();
            var models = await dbContext.TestModels.ToListAsync();
            Console.WriteLine("Retrieved Models");
            var models2 = dbContext.TestModels.ToList();
            Console.WriteLine("Retrieved Models2");
        }
    }
}