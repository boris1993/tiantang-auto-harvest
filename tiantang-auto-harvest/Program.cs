using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using tiantang_auto_harvest.Models;

namespace tiantang_auto_harvest
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var webApp = CreateHostBuilder(args).Build();

            using var scope = webApp.Services.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<DefaultDbContext>();
            dbContext.Database.Migrate();

            webApp.Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((hostBuilderContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                    });

                    logging.SetMinimumLevel(
                        hostBuilderContext.HostingEnvironment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);
                });
    }
}
