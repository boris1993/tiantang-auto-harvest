﻿using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.Jobs
{
    [DisallowConcurrentExecution]
    public class SigninJob : IJob
    {
        private readonly ILogger<SigninJob> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TiantangRemoteCallService _tiantangRemoteCallService;

        public SigninJob(
            ILogger<SigninJob> logger, 
            IServiceProvider serviceProvider, 
            TiantangRemoteCallService tiantangRemoteCallService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _tiantangRemoteCallService = tiantangRemoteCallService;
        }

        public Task Execute(IJobExecutionContext context)
        {
            using var scope = _serviceProvider.CreateScope();
            var tiantangLoginInfoDbContext = scope.ServiceProvider.GetService<DefaultDbContext>();
            if (tiantangLoginInfoDbContext == null)
            {
                _logger.LogError("tiantangLoginInfoDbContext为null");
                return Task.CompletedTask;
            }

            TiantangLoginInfo tiantangLoginInfo = tiantangLoginInfoDbContext.TiantangLoginInfo.SingleOrDefault();
            if (tiantangLoginInfo == null)
            {
                _logger.LogInformation("未登录甜糖账号，跳过签到");
                return Task.CompletedTask;
            }

            _logger.LogInformation($"将签到甜糖账号 {tiantangLoginInfo.PhoneNumber}");

            JsonDocument responseJson;
            try
            {
                responseJson = _tiantangRemoteCallService.DailyCheckIn(tiantangLoginInfo.AccessToken);
            }
            catch (ExternalApiCallException)
            {
                _logger.LogError("签到失败，请参考日志");
                return Task.CompletedTask;
            }

            int earnedScore = responseJson.RootElement.GetProperty("data").GetInt32();
            _logger.LogInformation($"签到成功，获得{earnedScore}点星愿");

            return Task.CompletedTask;
        }
    }
}
