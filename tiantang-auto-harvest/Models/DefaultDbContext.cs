using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace tiantang_auto_harvest.Models
{
    public class DefaultDbContext : DbContext, IDataProtectionKeyContext
    {
        public DbSet<TiantangLoginInfo> TiantangLoginInfo { get; set; }
        public DbSet<PushChannelConfiguration> PushChannelKeys { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

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
        [Required] public string PhoneNumber { get; set; }
        public string AccessToken { get; set; }
        public string UnionId { get; set; }
    }

    public class PushChannelConfiguration
    {
        public PushChannelConfiguration(string serviceName, string token, string secret = null)
        {
            ServiceName = serviceName;
            Token = token;
            Secret = secret;
        }

        public int Id { get; set; }
        public string ServiceName { get; set; }
        public string Token { get; set; }
        public string Secret { get; set; }
    }
}