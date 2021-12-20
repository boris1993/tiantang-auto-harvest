using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace tiantang_auto_harvest.Models
{

    public class DefaultDbContext : DbContext
    {
        public DbSet<TiantangLoginInfo> TiantangLoginInfo { get; set; }
        public DbSet<PushChannelKeys> PushChannelKeys { get; set; }
        public DbSet<ProxySettings> ProxySettings { get; set; }

        public DefaultDbContext(DbContextOptions<DefaultDbContext> options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TiantangLoginInfo>()
                .HasIndex(entity => entity.PhoneNumber)
                .IsUnique();

            modelBuilder.Entity<PushChannelKeys>()
                .HasIndex(entity => entity.ServerChanSendKey)
                .IsUnique();
            modelBuilder.Entity<PushChannelKeys>()
                .HasIndex(entity => entity.TelegramBotToken)
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

    public class PushChannelKeys
    {
        public int Id { get; set; }
        public string ServerChanSendKey { get; set; }
        public string TelegramBotToken { get; set; }
        public bool IsProxyNeeded { get; set; }
    }

    public class ProxySettings
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "代理类型不可为空")]
        public string Protocol { get; set; }
        [Required(ErrorMessage = "代理主机名不可为空")]
        public string Host { get; set; }
        [Required(ErrorMessage = "代理端口号不可为空"), Range(1, 65535, ErrorMessage = "端口号必须介于{1}~{2}")]
        public string Port { get; set; }
    }
}

