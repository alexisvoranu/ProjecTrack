using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Licenta3.Data
{
    public class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
    {
        public TestDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

            // 🟢 Conexiunea ta Railway (copiaz-o exact din appsettings.json)
            var connectionString = "Host=switchyard.proxy.rlwy.net;Port=21522;Database=railway;Username=postgres;Password=EpXOmSxcuHNczuMduqAwwXCZEbsSojmZ;SSL Mode=Require;Trust Server Certificate=true";

            optionsBuilder.UseNpgsql(connectionString);

            return new TestDbContext(optionsBuilder.Options);
        }
    }
}
