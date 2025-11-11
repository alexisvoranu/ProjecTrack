using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Licenta3.Models;

namespace Licenta3.Data
{
    public class IdentityDbContextOnly : IdentityDbContext<ApplicationUser>
    {
        public IdentityDbContextOnly(DbContextOptions<IdentityDbContextOnly> options)
            : base(options)
        {
        }

        // opțional — poți lăsa gol dacă doar vrei tabela AspNetUsers
    }
}
