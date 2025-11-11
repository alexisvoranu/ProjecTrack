using Licenta3.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Licenta3.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        //public ApplicationDbContext()
        //{
        //}

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Licenta3.Models.Task> Tasks { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<TaskResource> TaskResources { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Relațiile Many-to-Many pentru TaskResource
            modelBuilder.Entity<TaskResource>()
                .HasKey(tr => new { tr.TaskId, tr.ResourceId });

            modelBuilder.Entity<TaskResource>()
                .HasOne(tr => tr.Task)
                .WithMany(t => t.TaskResources)
                .HasForeignKey(tr => tr.TaskId);

            modelBuilder.Entity<TaskResource>()
                .HasOne(tr => tr.Resource)
                .WithMany(r => r.TaskResources)
                .HasForeignKey(tr => tr.ResourceId);

            // Transformă toate string-urile în text (PostgreSQL)
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string) && property.GetColumnType() == null)
                    {
                        property.SetColumnType("text");
                    }
                }
            }
        }

    }
}
