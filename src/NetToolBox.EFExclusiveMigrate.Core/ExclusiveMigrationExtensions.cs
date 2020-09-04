using NetToolBox.EFExclusiveMigrate.Core;

namespace Microsoft.EntityFrameworkCore
{
    public static class ExclusiveMigrationExtensions
    {
        public static DbContextOptionsBuilder UseEFExclusiveMigrate(this DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(new ExclusiveMigrationInterceptor());
            return optionsBuilder;
        }
    }
}