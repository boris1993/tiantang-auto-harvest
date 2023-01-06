using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using tiantang_auto_harvest.Constants;
using tiantang_auto_harvest.EventListeners;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Jobs;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Models.Responses;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            // Create the folder for storing the data
            Directory.CreateDirectory($"{AppContext.BaseDirectory}/data");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddDbContext<DefaultDbContext>(options =>
                options.UseSqlite($"Data Source={AppContext.BaseDirectory}/data/database.db"));

            services.AddScoped<AppService>();
            services.AddScoped<NotificationRemoteCallService>();
            services.AddHttpClient<AppService>(client =>
            {
                client.BaseAddress = new Uri(TiantangBackendURLs.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add(HttpRequestHeader.UserAgent.ToString(), TiantangBackendURLs.UserAgent);
                client.DefaultRequestHeaders.Add(HttpRequestHeader.AcceptEncoding.ToString(), TiantangBackendURLs.AcceptEncoding);
                
            });
            services.AddSingleton<TiantangRemoteCallService>();
            services.AddSingleton<ScoreLoadedEventHandler>();

            services.AddLogging(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                });
            });

            #region Quartz Configurations

            services.AddSingleton<IJobFactory, QuartzJobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();
            services.AddSingleton<SigninJob>();
            services.AddSingleton<HarvestJob>();
            services.AddSingleton<ApplyBonusCardsJob>();
            services.AddSingleton<RefreshLoginJob>();
            // 每日03:00签到
            services.AddSingleton(new JobSchedule(
                jobType: typeof(SigninJob),
                cronExpression: "0 0 3 * * ?"
            ));
            // 每日10:00收取星愿
            services.AddSingleton(new JobSchedule(
                jobType: typeof(HarvestJob),
                cronExpression: "0 0 10 * * ?"
            ));
            // 每日10:00检查并激活电费卡
            services.AddSingleton(new JobSchedule(
                jobType: typeof(ApplyBonusCardsJob),
                cronExpression: "0 0 10 * * ?"
            ));
            // 每日01:00检查是否需要刷新token
            services.AddSingleton(new JobSchedule(
                jobType: typeof(RefreshLoginJob),
                cronExpression: "0 0 1 * * ?"
            ));
            services.AddHostedService<QuartzHostedService>();

            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            DefaultDbContext defaultDbContext
        )
        {
            app.UseExceptionHandler(applicationBuilder => applicationBuilder.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
                var errorResponseBody = new ErrorResponse(exception?.Message);

                HttpStatusCode statusCode;
                if (exception is BaseAppException baseAppException)
                {
                    statusCode = baseAppException.ResponseStatusCode;
                }
                else
                {
                    statusCode = HttpStatusCode.InternalServerError;
                }

                context.Response.StatusCode = (int) statusCode;
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
            defaultDbContext.Database.Migrate();

            app.UseScoreLoadedEventHandler();
        }
    }
}