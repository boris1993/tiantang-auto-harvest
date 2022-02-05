using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using tiantang_auto_harvest.Extensions;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.Jobs
{
    public class RefreshLoginJob : IJob
    {
        private readonly ILogger<RefreshLoginJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TiantangRemoteCallService _tiantangRemoteCallService;

        public RefreshLoginJob(
            ILogger<RefreshLoginJob> logger,
            IServiceProvider serviceProvider,
            TiantangRemoteCallService tiantangRemoteCallService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _tiantangRemoteCallService = tiantangRemoteCallService;
        }

        public Task Execute(IJobExecutionContext context)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var defaultDbContext = scope.ServiceProvider.GetService<DefaultDbContext>();
                var tiantangLoginInfo = defaultDbContext.TiantangLoginInfo.SingleOrDefault();
                if (tiantangLoginInfo == null)
                {
                    _logger.LogInformation("无甜糖星愿登录信息，跳过自动刷新登录");
                }

                var accessToken = tiantangLoginInfo.AccessToken;
                var tokenBody = accessToken.Split('.')[1].PaddingBase64String();
                var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(tokenBody));
                var jsonTokenBody = JsonDocument.Parse(decodedToken);
                var expireTime = DateTimeOffset.FromUnixTimeSeconds(jsonTokenBody.RootElement.GetProperty("exp").GetInt32());
                var currentDate = DateTime.Now;

                if ((expireTime - currentDate).TotalHours > 24)
                {
                    _logger.LogInformation("Token有效期大于24小时，跳过刷新");
                    return Task.CompletedTask;
                }

                _logger.LogInformation("Token有效期不足24小时，将刷新登录");

                var unionId = tiantangLoginInfo.UnionId;
                var responseJson = _tiantangRemoteCallService.RefreshLogin(unionId);
                var newToken = responseJson.RootElement.GetProperty("data").GetProperty("token").GetString();
                tiantangLoginInfo.AccessToken = newToken;

                _logger.LogInformation($"新token为 {newToken}");

                defaultDbContext.SaveChanges();
            }

            return Task.CompletedTask;
        }
    }
}
