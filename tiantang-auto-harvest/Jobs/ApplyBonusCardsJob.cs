using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.Jobs
{
    [DisallowConcurrentExecution]
    public class ApplyBonusCardsJob : IJob
    {
        private readonly ILogger<SigninJob> _logger;
        private readonly IServiceProvider serviceProvider;
        
        public ApplyBonusCardsJob(ILogger<SigninJob> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = serviceProvider.CreateScope();
            var tiantangService = scope.ServiceProvider.GetService<TiantangService>();
            if (tiantangService == null)
            {
                _logger.LogError("未找到TiantangService实例，请检查Startup.cs中是否正确注册");
                return;
            }
            
            _logger.LogInformation("将执行激活电费卡定时任务");
            await tiantangService.CheckAndApplyElectricBillBonus(CancellationTokenHelper.GetCancellationToken());
        }
    }
}
