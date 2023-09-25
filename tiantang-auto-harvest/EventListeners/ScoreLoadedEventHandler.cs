using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.EventListeners
{
    public class ScoreLoadedEventHandler
    {
        private readonly ILogger<ScoreLoadedEventHandler> logger;
        private readonly IServiceProvider serviceProvider;

        public ScoreLoadedEventHandler(
            ILogger<ScoreLoadedEventHandler> logger,
            IServiceProvider serviceProvider
        )
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
        }

        public async Task HandleScoresLoadedEvent(object _, EventArgs eventArgs)
        {
            if (eventArgs.GetType() != typeof(TiantangScoresEventArgs))
            {
                logger.LogError("传入的eventArgs不是TiantangScores类型");
                return;
            }

            var tiantangScores = (TiantangScoresEventArgs)eventArgs;
            var totalDeviceScores = tiantangScores.DeviceScores.Select(e => e.Value).Sum();

            using var scope = serviceProvider.CreateScope();
            var notificationRemoteCallService = scope.ServiceProvider.GetService<NotificationRemoteCallService>();
            await notificationRemoteCallService!.SendNotificationToAllChannels(
                new NotificationBody(
                    $"今日已收取{tiantangScores.PromotionScore + totalDeviceScores}点星愿\n" +
                    $"包括{tiantangScores.PromotionScore}点推广星愿，和{totalDeviceScores}点设备星愿"));
        }
    }

    public class TiantangScoresEventArgs : EventArgs
    {
        public string AccessToken { get; set; }

        /// <summary>
        /// 推广获得的点数
        /// </summary>
        public int PromotionScore { get; set; }

        /// <summary>
        /// Key是节点ID，value是此节点当日可收取的点数
        /// </summary>
        public Dictionary<string, int> DeviceScores { get; } = new Dictionary<string, int>();
    }
}
