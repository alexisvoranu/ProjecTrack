using Licenta3.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Licenta3.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<Licenta3.Models.Task> Tasks { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<TaskResource> TaskResources { get; set; }  // 👈 adaugă asta

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurăm relația many-to-many
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
