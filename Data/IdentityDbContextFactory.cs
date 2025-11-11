using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Licenta3.Data
{
    public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContextOnly>
    {
        public IdentityDbContextOnly CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContextOnly>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseNpgsql(connectionString);

            return new IdentityDbContextOnly(optionsBuilder.Options);
        }
    }
}
