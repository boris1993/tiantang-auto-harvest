using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;
using tiantang_auto_harvest.Constants;
using tiantang_auto_harvest.Models;

namespace tiantang_auto_harvest.Service
{
    public class NotificationRemoteCallService
    {
        private readonly ILogger _logger;
        private readonly DefaultDbContext _defaultDbContext;
        private readonly HttpClient _httpClient;

        public NotificationRemoteCallService(
            ILogger<NotificationRemoteCallService> logger,
            DefaultDbContext defaultDbContext,
            HttpClient httpClient
        )
        {
            _logger = logger;
            _defaultDbContext = defaultDbContext;
            _httpClient = httpClient;
        }

        public async Task SendNotificationToAllChannels(NotificationBody notificationBody)
        {
            await SendNotificationViaServerChan(notificationBody);
            await SendNotificationViaBark(notificationBody);
            await SendNotificationViaDingTalk(notificationBody);
        }

        private async Task SendNotificationViaServerChan(NotificationBody notificationBody)
        {
            var serverChanConfig =
                _defaultDbContext
                    .PushChannelKeys
                    .SingleOrDefault(p => p.ServiceName == NotificationChannelNames.ServerChan);

            if (serverChanConfig == null || string.IsNullOrEmpty(serverChanConfig.Token))
            {
                return;
            }

            var requestBody = new List<KeyValuePair<string, string>>
            {
                new("title", notificationBody.Title),
                new("desp", notificationBody.Content.Replace("\n", "\n\n"))
            };

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(string.Format(NotificationURLs.ServerChan, serverChanConfig.Token)),
                Headers =
                {
                    {HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded;charset=UTF-8"}
                },
                Content = new FormUrlEncodedContent(requestBody)
            };

            await _httpClient.SendAsync(httpRequestMessage);
        }

        private async Task SendNotificationViaBark(NotificationBody notificationBody)
        {
            var barkConfig = _defaultDbContext
                .PushChannelKeys
                .SingleOrDefault(p => p.ServiceName == NotificationChannelNames.Bark);
            if (barkConfig == null || string.IsNullOrEmpty(barkConfig.Token))
            {
                return;
            }

            var uri = new Uri(string.Format(NotificationURLs.Bark, barkConfig.Token, notificationBody.Title,
                notificationBody.Content));

            await _httpClient.GetAsync(uri);
        }

        private async Task SendNotificationViaDingTalk(NotificationBody notificationBody)
        {
            var dingTalkConfig =
                _defaultDbContext
                    .PushChannelKeys
                    .SingleOrDefault(p => p.ServiceName == NotificationChannelNames.DingTalk);

            if (dingTalkConfig == null
                || string.IsNullOrEmpty(dingTalkConfig.Token)
                || string.IsNullOrEmpty(dingTalkConfig.Secret))
            {
                return;
            }

            var accessToken = dingTalkConfig.Token;

            var timestamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
            var secret = dingTalkConfig.Secret;
            var stringToSign = $"{timestamp}\n{secret}";

            var utf8Encoding = new UTF8Encoding();

            using var hmac256 = new HMACSHA256(utf8Encoding.GetBytes(secret));
            var signatureBytes = hmac256.ComputeHash(utf8Encoding.GetBytes(stringToSign));
            var base64EncodedSignature = System.Convert.ToBase64String(signatureBytes);
            var signatureString = HttpUtility.UrlEncode(base64EncodedSignature, Encoding.UTF8);

            var uri = new Uri(
                string.Format(
                    NotificationURLs.DingTalk,
                    accessToken,
                    timestamp,
                    signatureString));

            var requestBody = new
            {
                msgtype = "markdown",
                markdown = new
                {
                    title = notificationBody.Title,
                    text = $"# {notificationBody.Title}\n{notificationBody.Content.Replace("\n", "\n\n")}"
                }, 
            };

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Headers =
                {
                    {HttpRequestHeader.ContentType.ToString(), MediaTypeNames.Application.Json}
                },
                Content = JsonContent.Create(requestBody)
            };
            
            await _httpClient.SendAsync(httpRequestMessage);
        }
    }
}