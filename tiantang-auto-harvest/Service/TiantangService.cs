using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;

namespace tiantang_auto_harvest.Service
{
    public class TiantangService
    {
        private readonly ILogger<TiantangService> _logger;
        private readonly DefaultDbContext _dbContext;
        private readonly TiantangRemoteCallService _tiantangRemoteCallService;

        public TiantangService(
            ILogger<TiantangService> logger,
            DefaultDbContext dbContext,
            TiantangRemoteCallService tiantangRemoteCallService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _tiantangRemoteCallService = tiantangRemoteCallService;
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
    }
}
