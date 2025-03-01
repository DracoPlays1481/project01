using ESD_PROJECT.Models;
using Microsoft.EntityFrameworkCore;

namespace ESD_PROJECT.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
            : base(options)
        { 
        }

        public DbSet<Booking>? Bookings { get; set; }
    }
}
