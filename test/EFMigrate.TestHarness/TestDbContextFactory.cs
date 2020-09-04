using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EFMigrate.TestHarness
{
    public sealed class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
    {
        public TestDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
            optionsBuilder.UseSqlServer("Server=(Localdb)\\MSSQLLocalDb;Database=TestDatabase;Integrated Security=true;");
            return new TestDbContext(optionsBuilder.Options);
        }
    }
}