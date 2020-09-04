using Microsoft.EntityFrameworkCore;

namespace EFMigrate.TestHarness
{
    public sealed class TestDbContext : DbContext
    {
        public DbSet<TestModel> TestModels { get; set; }

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }
    }
}