using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace tiantang_auto_harvest.Models
{

    public class TiantangLoginInfoDbContext: DbContext
    {
        public DbSet<TiantangLoginInfo> TiantangLoginInfo { get; set; }

        public TiantangLoginInfoDbContext(DbContextOptions<TiantangLoginInfoDbContext> options): base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TiantangLoginInfo>()
                .HasIndex(entity => entity.PhoneNumber)
                .IsUnique();
        }
    } 

    public class TiantangLoginInfo
    {
        public int Id { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        public string AccessToken { get; set; }
    }
}

