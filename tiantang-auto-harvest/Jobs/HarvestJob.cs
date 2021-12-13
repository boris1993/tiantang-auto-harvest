using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.Jobs
{
    [DisallowConcurrentExecution]
    public class HarvestJob : IJob
    {

        private readonly ILogger<HarvestJob> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly TiantangRemoteCallService tiantangRemoteCallService;

        public HarvestJob(ILogger<HarvestJob> logger, IServiceProvider serviceProvider, TiantangRemoteCallService tiantangRemoteCallService)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tiantangRemoteCallService = tiantangRemoteCallService;
        }

        public Task Execute(IJobExecutionContext context)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var tiantangLoginInfoDbContext = scope.ServiceProvider.GetService<TiantangLoginInfoDbContext>();

                var tiantangLoginInfo = tiantangLoginInfoDbContext.TiantangLoginInfo.FirstOrDefault();
                if (tiantangLoginInfo == null)
                {
                    logger.LogInformation("未登录甜糖账号，跳过收取星愿");
                    return Task.CompletedTask;
                }

                logger.LogInformation($"将收取甜糖账号 {tiantangLoginInfo.PhoneNumber}");
                var tiantangScores = new TiantangScores();
                JsonDocument responseJson;
                try
                {
                    responseJson = tiantangRemoteCallService.RetrieveUserInfo(tiantangLoginInfo.AccessToken);
                }
                catch (ExternalAPICallException)
                {
                    logger.LogError("获取用户信息失败，请参考日志");
                    return Task.CompletedTask;
                }

                tiantangScores.PromotionScore = responseJson.RootElement.GetProperty("data").GetProperty("inactivedPromoteScore").GetInt32();
                logger.LogInformation($"今日可收取{tiantangScores.PromotionScore}点推广星愿");

                try
                {
                    responseJson = tiantangRemoteCallService.RetrieveNodes(tiantangLoginInfo.AccessToken);
                }
                catch (ExternalAPICallException)
                {
                    logger.LogError("获取节点列表失败，请参考日志");
                    return Task.CompletedTask;
                }

                var nodes = responseJson.RootElement.GetProperty("data").GetProperty("data");
                logger.LogInformation($"获取到{nodes.GetArrayLength()}个节点");
                foreach (JsonElement node in nodes.EnumerateArray())
                {
                    var nodeId = node.GetProperty("id").GetString();
                    var score = node.GetProperty("inactived_score").GetInt32();
                    logger.LogInformation($"节点ID {nodeId} 可收取 {score} 点星愿");
                    tiantangScores.DeviceScores[nodeId] = score;
                }
            }

            return Task.CompletedTask;
        }

        public class TiantangScores
        {
            /// <summary>
            /// 推广获得的点数
            /// </summary>
            public int PromotionScore { get; set; }

            /// <summary>
            /// Key是节点ID，value是此节点当日可收取的点数
            /// </summary>
            public Dictionary<string, int> DeviceScores { get; set; }
        }
    }
}
