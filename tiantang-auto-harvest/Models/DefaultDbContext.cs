using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace tiantang_auto_harvest.Models
{

    public class DefaultDbContext : DbContext
    {
        public DbSet<TiantangLoginInfo> TiantangLoginInfo { get; set; }
        public DbSet<PushChannelConfiguration> PushChannelKeys { get; set; }
        public DbSet<UnsendNotification> UnsendNotifications { get; set; }

        public DefaultDbContext(DbContextOptions<DefaultDbContext> options) : base(options)
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

    public class PushChannelConfiguration
    {
        public PushChannelConfiguration() { }

        public PushChannelConfiguration(string serviceName, string token)
        {
            ServiceName = serviceName;
            Token = token;
        }

        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string Token { get; set; }
    }

    public class UnsendNotification
    {
        public UnsendNotification() { }
        public UnsendNotification(string content)
        {
            Content = content;
        }
        public int Id { get; set; }
        public string Content { get; set; }
    }
}

