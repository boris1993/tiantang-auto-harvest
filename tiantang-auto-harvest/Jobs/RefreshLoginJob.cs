﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.Jobs
{
    public class RefreshLoginJob : IJob
    {
        private readonly ILogger<RefreshLoginJob> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RefreshLoginJob(ILogger<RefreshLoginJob> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            using var scope = _serviceProvider.CreateScope();
            var tiantangService = scope.ServiceProvider.GetService<TiantangService>();
            if (tiantangService == null)
            {
                _logger.LogError("未找到TiantangService实例，请检查Startup.cs中是否正确注册");
                return;
            }

            _logger.LogInformation("将执行刷新token定时任务");
            await tiantangService.RefreshLogin();
        }
    }
}
