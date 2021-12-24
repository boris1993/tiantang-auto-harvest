using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tiantang_auto_harvest.Jobs;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.EventListeners
{
    public class ScoreLoadedEventHandler
    {
        private readonly ILogger<ScoreLoadedEventHandler> logger;
        private readonly IServiceProvider serviceProvider;
        private readonly TiantangRemoteCallService tiantangRemoteCallService;

        public ScoreLoadedEventHandler(
            ILogger<ScoreLoadedEventHandler> logger,
            IServiceProvider serviceProvider,
            TiantangRemoteCallService tiantangRemoteCallService
        )
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.tiantangRemoteCallService = tiantangRemoteCallService;
        }

        public void HandleScoresLoadedEvent(object sender, EventArgs eventArgs)
        {
            if (eventArgs.GetType() != typeof(TiantangScores))
            {
                logger.LogError("传入的eventArgs不是TiantangScores类型");
                return;
            }

            var tiantangScores = (TiantangScores)eventArgs;
            var accessToken = tiantangScores.AccessToken;

            var promoteScore = tiantangScores.PromotionScore;
            if (promoteScore <= 0)
            {
                logger.LogInformation("无可收取的推广星愿");
            }
            else
            {
                logger.LogInformation($"正在收取{tiantangScores.PromotionScore}点推广星愿");
                tiantangRemoteCallService.HarvestPromotionScore(tiantangScores.PromotionScore, tiantangScores.AccessToken);
            }

            var deviceScores = tiantangScores.DeviceScores;
            tiantangRemoteCallService.HarvestDeviceScore(deviceScores, accessToken);

            var totalDeviceScores = deviceScores.Select(e => e.Value).Sum();
            using (var scope = serviceProvider.CreateScope())
            {
                var notificationRemoteCallService = scope.ServiceProvider.GetService<NotificationRemoteCallService>();
                Task.Run(async () =>
                {
                    await notificationRemoteCallService.SendNotificationToAllChannels(
                        new NotificationBody($"今日已收取{tiantangScores.PromotionScore}点推广星愿\n共收取{totalDeviceScores}点设备星愿"));
                });
            }
        }
    }

    public static class ScoreLoadedEventHandlerExtensions
    {
        public static void UseScoreLoadedEventHandler(this IApplicationBuilder app)
        {
            var serviceProvider = app.ApplicationServices;
            var harvestJob = serviceProvider.GetService<HarvestJob>();
            // 监听星愿检查完成事件
            harvestJob.ScoresLoadedEventHandler += (sender, args) =>
            {
                var handler = serviceProvider.GetService<ScoreLoadedEventHandler>();
                handler.HandleScoresLoadedEvent(sender, args);
            };

        }
    }

    public class TiantangScores : EventArgs
    {
        public string AccessToken { get; set; }

        /// <summary>
        /// 推广获得的点数
        /// </summary>
        public int PromotionScore { get; set; }

        /// <summary>
        /// Key是节点ID，value是此节点当日可收取的点数
        /// </summary>
        public Dictionary<string, int> DeviceScores = new Dictionary<string, int>();
    }
}
