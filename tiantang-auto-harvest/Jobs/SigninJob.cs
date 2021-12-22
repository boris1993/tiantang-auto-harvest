using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.Jobs
{
    [DisallowConcurrentExecution]
    public class SigninJob : IJob
    {
        private readonly ILogger<SigninJob> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly TiantangRemoteCallService tiantangRemoteCallService;

        public SigninJob(ILogger<SigninJob> logger, IServiceProvider serviceProvider, TiantangRemoteCallService tiantangRemoteCallService)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tiantangRemoteCallService = tiantangRemoteCallService;
        }

        public Task Execute(IJobExecutionContext context)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var tiantangLoginInfoDbContext = scope.ServiceProvider.GetService<DefaultDbContext>();

                TiantangLoginInfo tiantangLoginInfo = tiantangLoginInfoDbContext.TiantangLoginInfo.FirstOrDefault();
                if (tiantangLoginInfo == null)
                {
                    logger.LogInformation("未登录甜糖账号，跳过签到");
                    return Task.CompletedTask;
                }

                logger.LogInformation($"将签到甜糖账号 {tiantangLoginInfo.PhoneNumber}");

                Uri uri = new Uri(Constants.TiantangBackendURLs.DailyCheckInURL);
                JsonDocument responseJson;
                try
                {
                    responseJson = tiantangRemoteCallService.DailyCheckIn(tiantangLoginInfo.AccessToken);
                }
                catch (ExternalAPICallException)
                {
                    logger.LogError("签到失败，请参考日志");
                    return Task.CompletedTask;
                }

                int earnedScore = responseJson.RootElement.GetProperty("data").GetInt32();
                logger.LogInformation($"签到成功，获得{earnedScore}点星愿");

                return Task.CompletedTask;
            }
        }
    }
}
