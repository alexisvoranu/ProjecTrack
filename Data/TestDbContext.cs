using Licenta3.Models;
using Microsoft.EntityFrameworkCore;

namespace Licenta3.Data
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        // Pornim doar cu un tabel (îl vom schimba pe rând)
        public DbSet<TaskResource> TaskResources { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // M:M
            modelBuilder.Entity<TaskResource>()
                .HasOne(tr => tr.Task)
                .WithMany(t => t.TaskResources)
                .HasForeignKey(tr => tr.TaskId);

            modelBuilder.Entity<TaskResource>()
                .HasOne(tr => tr.Resource)
                .WithMany(r => r.TaskResources)
                .HasForeignKey(tr => tr.ResourceId);
        }
    }
}
