using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using tiantang_auto_harvest.Models;

namespace tiantang_auto_harvest.Service
{
    public class NotificationRemoteCallService
    {
        private readonly ILogger logger;
        private readonly DefaultDbContext defaultDbContext;
        private readonly HttpClient httpClient;

        public NotificationRemoteCallService(
            ILogger<NotificationRemoteCallService> logger,
            DefaultDbContext defaultDbContext,
            HttpClient httpClient
        )
        {
            this.logger = logger;
            this.defaultDbContext = defaultDbContext;
            this.httpClient = httpClient;
        }

        public async Task SendNotificationToAllChannels(NotificationBody notificationBody)
        {
            await SendNotificationViaServerChan(notificationBody);
            await SendNotificationViaBark(notificationBody);
        }

        public async Task SendNotificationViaServerChan(NotificationBody notificationBody)
        {
            var serverChanConfig = defaultDbContext.PushChannelKeys.Where(p => p.ServiceName == Constants.NotificationChannelNames.ServerChan).SingleOrDefault();
            if (serverChanConfig == null || string.IsNullOrEmpty(serverChanConfig.Token))
            {
                logger.LogInformation("Server酱配置为空，跳过通过Server酱发送通知");
                return;
            }

            var requestBody = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("title", notificationBody.Title),
                new KeyValuePair<string, string>("desp", notificationBody.Content.Replace("\n", "\n\n"))
            };

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(string.Format(Constants.NotificationURLs.ServerChan, serverChanConfig.Token)),
                Headers =
                {
                    { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded;charset=UTF-8" }
                },
                Content = new FormUrlEncodedContent(requestBody)
            };

            await httpClient.SendAsync(httpRequestMessage);
        }

        public async Task SendNotificationViaBark(NotificationBody notificationBody)
        {
            var barkConfig = defaultDbContext.PushChannelKeys.Where(p => p.ServiceName == Constants.NotificationChannelNames.Bark).SingleOrDefault();
            if (barkConfig == null || string.IsNullOrEmpty(barkConfig.Token))
            {
                logger.LogInformation("Bark配置为空，跳过通过Bark发送通知");
                return;
            }

            var uri = new Uri(string.Format(Constants.NotificationURLs.Bark, barkConfig.Token, notificationBody.Title, notificationBody.Content));

            await httpClient.GetAsync(uri);
        }
    }
}
