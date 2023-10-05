using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tiantang_auto_harvest.Constants;
using tiantang_auto_harvest.EventListeners;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Extensions;
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

        public async Task Signin(CancellationToken cancellationToken = default)
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

        public async Task Harvest(CancellationToken cancellationToken = default)
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

        public async Task CheckAndApplyElectricBillBonus(CancellationToken cancellationToken = default)
        {
            var tiantangLoginInfo = await _dbContext.TiantangLoginInfo.SingleOrDefaultAsync(cancellationToken);
            if (tiantangLoginInfo == null)
            {
                _logger.LogInformation("未登录甜糖账号，跳过收取星愿");
                return;
            }

            var accessToken = tiantangLoginInfo.AccessToken;
            
            _logger.LogInformation("正在检查是否有已启用的加成卡");
            JsonDocument activatedBonusCardResponse;
            try
            {
                activatedBonusCardResponse = await _tiantangRemoteCallService.RetrieveActivatedBonusCards(accessToken, cancellationToken);
            }
            catch (ExternalApiCallException)
            {
                _logger.LogError("获取当前启用的加成卡失败，请参考日志");
                return;
            }

            _logger.LogInformation("正在获取全部加成卡");
            JsonDocument allBonusCardsResponse;
            try
            {
                allBonusCardsResponse = await _tiantangRemoteCallService.RetrieveAllBonusCards(accessToken, cancellationToken);
            }
            catch (ExternalApiCallException)
            {
                _logger.LogError("获取全部加成卡失败，请参考日志");
                return;
            }
            
            #region 检查电费卡数量
            var electricBillBonusCardInfo =
                allBonusCardsResponse
                    .RootElement
                    .GetProperty("data")
                    .EnumerateArray()
                    .SingleOrDefault(element => 
                        element.GetProperty("prop_id").GetInt32().ToString() == TiantangBonusCardTypes.ElectricBillBonus);

            if (electricBillBonusCardInfo.ValueKind == JsonValueKind.Undefined)
            {
                _logger.LogInformation("无可用电费卡，跳过检查和启用电费卡");
                return;
            }

            var remainingElectricBillBonusCardNumber = electricBillBonusCardInfo.GetProperty("count").GetInt32().ToString();
            _logger.LogInformation($"剩余{remainingElectricBillBonusCardNumber}张电费卡");
            #endregion

            #region 检查当前有无正在生效的电费卡
            var currentActivatedElectricBillBonus =
                activatedBonusCardResponse
                    .RootElement
                    .GetProperty("data")
                    .EnumerateArray()
                    .SingleOrDefault(element => element.GetProperty("name").GetString() == "电费卡");

            if (currentActivatedElectricBillBonus.ValueKind != JsonValueKind.Undefined)
            {
                var expireEpoch = currentActivatedElectricBillBonus.GetProperty("ended_at").GetInt32();
                var expireDate = DateTimeOffset.FromUnixTimeSeconds(expireEpoch).ToString("yyyy-MM-dd HH:mm:ss");
                _logger.LogInformation($"已有激活的电费卡，将于{expireDate}过期");
                return;
            }
            #endregion

            #region 激活电费卡
            var isElectricBillBonusCardExist = allBonusCardsResponse
                .RootElement
                .GetProperty("data")
                .EnumerateArray()
                .Any(element => element.GetProperty("prop_id").GetInt32().ToString() == TiantangBonusCardTypes.ElectricBillBonus);

            if (!isElectricBillBonusCardExist)
            {
                _logger.LogInformation("无可用电费卡");
                return;
            }

            _logger.LogInformation("正在激活电费卡");
            
            await _tiantangRemoteCallService.ActiveElectricBillBonusCard(accessToken, cancellationToken);
            #endregion
        }
        
        public async Task RefreshLogin(CancellationToken cancellationToken = default)
        {
            var tiantangLoginInfo = await _dbContext.TiantangLoginInfo.SingleOrDefaultAsync(cancellationToken);
            if (tiantangLoginInfo == null)
            {
                _logger.LogInformation("未登录甜糖账号，跳过收取星愿");
                return;
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
                return;
            }

            _logger.LogInformation("Token有效期不足24小时，将刷新登录");

            var unionId = tiantangLoginInfo.UnionId;
            var responseJson = await _tiantangRemoteCallService.RefreshLogin(unionId, cancellationToken);
            var newToken = responseJson.RootElement.GetProperty("data").GetProperty("token").GetString();
            tiantangLoginInfo.AccessToken = newToken;

            _logger.LogInformation("新token为 {NewToken}", newToken);
            
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
