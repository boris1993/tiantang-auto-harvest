using Microsoft.EntityFrameworkCore;

namespace tiantang_auto_harvest.Models
{
    public class PushChannelKeys
    {
        public int Id { get; set; }
        public string ServerChanSendKey { get; set; }
        public string TelegramBotToken { get; set; }
    }

    public class PushChannelKeysDbContext : DbContext
    {
        public DbSet<PushChannelKeys> PushChannels { get; set; }

        public PushChannelKeysDbContext(DbContextOptions<PushChannelKeysDbContext> options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PushChannelKeys>()
                .HasIndex(entity => entity.ServerChanSendKey)
                .IsUnique();
            modelBuilder.Entity<PushChannelKeys>()
                .HasIndex(entity => entity.TelegramBotToken)
                .IsUnique();
        }
    }
}
