using Microsoft.EntityFrameworkCore;
using userauth.Models; // Changed from userauth.Models to userauth.Models as per original project name

namespace userauth.Data // Changed namespace to userauth.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure the IdentityUser table name if you want a different one than AspNetUsers
            // builder.Entity<User>().ToTable("Users");

            // You can add more model configurations here if needed
        }
    }
}