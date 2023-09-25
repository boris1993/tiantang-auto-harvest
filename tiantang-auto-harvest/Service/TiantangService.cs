using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tiantang_auto_harvest.EventListeners;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;

namespace tiantang_auto_harvest.Service
{
    public class TiantangService
    {
        private readonly ILogger<TiantangService> _logger;
        private readonly DefaultDbContext _dbContext;
        private readonly TiantangRemoteCallService _tiantangRemoteCallService;

        private EventHandler _scoresLoadedEventHandler { get; }

        public TiantangService(
            ILogger<TiantangService> logger,
            DefaultDbContext dbContext,
            ScoreLoadedEventHandler scoreLoadedEventHandler,
            TiantangRemoteCallService tiantangRemoteCallService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _tiantangRemoteCallService = tiantangRemoteCallService;

            _scoresLoadedEventHandler += async (sender, args) =>
            {
                await scoreLoadedEventHandler.HandleScoresLoadedEvent(sender, args);
            };
        }

        public async Task Signin()
        {
            var tiantangLoginInfo = await _dbContext.TiantangLoginInfo.SingleOrDefaultAsync();
            if (tiantangLoginInfo == null)
            {
                _logger.LogInformation("未登录甜糖账号，跳过签到");
                return;
            }
            
            _logger.LogInformation($"将签到甜糖账号 {tiantangLoginInfo.PhoneNumber}");
            
            JsonDocument responseJson;
            try
            {
                var cancellationToken = CancellationTokenHelper.GetCancellationToken();
                responseJson = await _tiantangRemoteCallService.DailyCheckIn(tiantangLoginInfo.AccessToken, cancellationToken);
            }
            catch (ExternalApiCallException)
            {
                _logger.LogError("签到失败，请参考日志");
                return;
            }
            
            var earnedScore = responseJson.RootElement.GetProperty("data").GetInt32();
            _logger.LogInformation($"签到成功，获得{earnedScore}点星愿");
        }

        public async Task Harvest()
        {
            var tiantangLoginInfo = await _dbContext.TiantangLoginInfo.SingleOrDefaultAsync();
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
            if (tiantangScores.PromotionScore > 0)
            {
                _logger.LogInformation($"正在收取{tiantangScores.PromotionScore}点推广星愿");
                await _tiantangRemoteCallService.HarvestPromotionScore(tiantangScores.PromotionScore, tiantangScores.AccessToken);
            }

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
            if (nodes.ValueKind == JsonValueKind.Null)
            {
                _logger.LogInformation("未获取到节点，跳过收取星愿");
                return;
            }
            
            _logger.LogInformation("获取到{NodeCount}个节点", nodes.GetArrayLength());
            foreach (var node in nodes.EnumerateArray())
            {
                var nodeId = node.GetProperty("id").GetString();
                var score = node.GetProperty("inactived_score").GetInt32();
                _logger.LogInformation("节点ID {NodeId} 可收取 {Score} 点星愿", nodeId, score);
                tiantangScores.DeviceScores[nodeId] = score;
            }
            
            await _tiantangRemoteCallService.HarvestDeviceScore(tiantangScores.DeviceScores, tiantangLoginInfo.AccessToken);

            // 星愿检查完成后发送事件
            _scoresLoadedEventHandler?.Invoke(this, tiantangScores);
        }
    }
}
