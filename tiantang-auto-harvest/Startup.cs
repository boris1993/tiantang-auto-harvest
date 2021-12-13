using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Net;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Jobs;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Models.Responses;
using tiantang_auto_harvest.Service;
using static tiantang_auto_harvest.Jobs.HarvestJob;

namespace tiantang_auto_harvest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddDbContext<TiantangLoginInfoDbContext>(options => options.UseSqlite($"Data Source={AppContext.BaseDirectory}/database.db"));
            services.AddDbContext<PushChannelKeysDbContext>(options => options.UseSqlite($"Data Source={AppContext.BaseDirectory}/database.db"));
            services.AddScoped<AppService>();
            services.AddHttpClient<AppService>(client =>
            {
                client.BaseAddress = new Uri(Constants.TiantangBackendURLs.BaseURL);
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddSingleton<TiantangRemoteCallService>();
            services.AddSingleton<ScoreLoadedEventHandler>();

            #region Quartz Configurations
            services.AddSingleton<IJobFactory, QuartzJobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<SigninJob>();
            services.AddSingleton<HarvestJob>();
            // Will sign in on 03:00 each day
            services.AddSingleton(new JobSchedule(
                jobType: typeof(SigninJob),
                cronExpression: "0 0 3 * * ?"
            ));
            services.AddSingleton(new JobSchedule(
                jobType: typeof(HarvestJob),
                cronExpression: "0 0 3 * * ?"
            ));
            services.AddHostedService<QuartzHostedService>();
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            TiantangLoginInfoDbContext tiantangLoginInfoDbContext,
            PushChannelKeysDbContext pushChannelKeysDbContext
        )
        {
            app.UseExceptionHandler(app => app.Run(async context =>
            {
                Exception exception = context.Features.Get<IExceptionHandlerPathFeature>().Error;
                ErrorResponse errorResponseBody = new ErrorResponse(exception.Message);

                HttpStatusCode statusCode;
                if (exception is BaseAppException)
                {
                    statusCode = ((BaseAppException)exception).ResponseStatusCode;
                }
                else
                {
                    statusCode = HttpStatusCode.InternalServerError;
                }

                context.Response.StatusCode = (int)statusCode;
                await context.Response.WriteAsJsonAsync(errorResponseBody);
            }));

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            // Migrate any database changes on startup (includes initial db creation)
            tiantangLoginInfoDbContext.Database.Migrate();
            pushChannelKeysDbContext.Database.Migrate();

            app.UseScoreLoadedEventHandler();
        }
    }
}
