using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using tiantang_auto_harvest.EventListeners;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.Jobs
{
    [DisallowConcurrentExecution]
    public class HarvestJob : IJob
    {

        private readonly ILogger<HarvestJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TiantangRemoteCallService _tiantangRemoteCallService;

        public EventHandler ScoresLoadedEventHandler { get; set; }

        public HarvestJob(ILogger<HarvestJob> logger, IServiceProvider serviceProvider, TiantangRemoteCallService tiantangRemoteCallService)
        {
            this._logger = logger;
            this._serviceProvider = serviceProvider;
            this._tiantangRemoteCallService = tiantangRemoteCallService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _serviceProvider.CreateScope();
            var defaultDbContext = scope.ServiceProvider.GetService<DefaultDbContext>();

            var tiantangLoginInfo = defaultDbContext!.TiantangLoginInfo.SingleOrDefault();
            if (tiantangLoginInfo == null)
            {
                _logger.LogInformation("未登录甜糖账号，跳过收取星愿");
                return;
            }

            _logger.LogInformation("将收取甜糖账号 {PhoneNumber}", tiantangLoginInfo.PhoneNumber);
            var tiantangScores = new TiantangScoresEventArgs
            {
                AccessToken = tiantangLoginInfo.AccessToken,
            };

            JsonDocument responseJson;
            try
            {
                var cancellationToken = CancellationTokenHelper.GetCancellationToken();
                responseJson = await _tiantangRemoteCallService.RetrieveUserInfo(tiantangLoginInfo.AccessToken, cancellationToken);
            }
            catch (ExternalApiCallException)
            {
                _logger.LogError("获取用户信息失败，请参考日志");
                return;
            }

            tiantangScores.PromotionScore = responseJson.RootElement.GetProperty("data").GetProperty("inactivedPromoteScore").GetInt32();
            _logger.LogInformation("今日可收取{TiantangScoresPromotionScore}点推广星愿", tiantangScores.PromotionScore);

            try
            {
                responseJson = await _tiantangRemoteCallService.RetrieveNodes(tiantangLoginInfo.AccessToken);
            }
            catch (ExternalApiCallException)
            {
                _logger.LogError("获取节点列表失败，请参考日志");
                return;
            }

            var nodes = responseJson.RootElement.GetProperty("data").GetProperty("data");
            _logger.LogInformation("获取到{NodeCount}个节点", nodes.GetArrayLength());
            foreach (var node in nodes.EnumerateArray())
            {
                var nodeId = node.GetProperty("id").GetString();
                var score = node.GetProperty("inactived_score").GetInt32();
                _logger.LogInformation("节点ID {NodeId} 可收取 {Score} 点星愿", nodeId, score);
                tiantangScores.DeviceScores[nodeId] = score;
            }

            // 星愿检查完成后发送事件
            ScoresLoadedEventHandler?.Invoke(this, tiantangScores);
        }
    }
}
