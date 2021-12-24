using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using tiantang_auto_harvest.Constants;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Jobs;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Models.Requests;

namespace tiantang_auto_harvest.Service
{
    public class AppService
    {
        private readonly ILogger logger;
        private readonly DefaultDbContext defaultDbContext;
        private readonly NotificationRemoteCallService notificationRemoteCallService;
        private readonly HttpClient httpClient;

        public AppService(
            ILogger<AppService> logger,
            DefaultDbContext defaultDbContext,
            NotificationRemoteCallService notificationRemoteCallService,
            HttpClient httpClient
        )
        {
            this.logger = logger;
            this.defaultDbContext = defaultDbContext;
            this.notificationRemoteCallService = notificationRemoteCallService;
            this.httpClient = httpClient;
        }

        public async Task RetrieveSMSCode(string phoneNumber)
        {
            logger.LogInformation($"正在向 {phoneNumber} 发送验证码短信");

            UriBuilder uriBuilder = new UriBuilder(Constants.TiantangBackendURLs.SendSMSURL);
            uriBuilder.Query = $"phone={phoneNumber}";
            Uri uri = uriBuilder.Uri;

            HttpResponseMessage response = await httpClient.PostAsync(uri, null);
            EnsureSuccessfulResponse(response);

            logger.LogInformation("短信发送成功");
        }

        public async Task VerifySMSCode(string phoneNumber, string smsCode)
        {
            logger.LogInformation($"正在校验验证码 {smsCode}");

            UriBuilder uriBuilder = new UriBuilder(Constants.TiantangBackendURLs.VerifySMSCodeURL);
            uriBuilder.Query = $"phone={phoneNumber}&authCode={smsCode}";
            Uri uri = uriBuilder.Uri;

            HttpResponseMessage response = await httpClient.PostAsync(uri, null);
            EnsureSuccessfulResponse(response);

            string responseBody = await response.Content.ReadAsStringAsync();
            JsonDocument responseJson = JsonDocument.Parse(responseBody);

            string token = responseJson.RootElement.GetProperty("data").GetProperty("token").GetString();
            logger.LogInformation($"Token是 {token}");

            // Remove all records before inserting the new one
            defaultDbContext.TiantangLoginInfo.RemoveRange(defaultDbContext.TiantangLoginInfo);
            TiantangLoginInfo tiantangLoginInfo = tiantangLoginInfo = new TiantangLoginInfo();
            tiantangLoginInfo.PhoneNumber = phoneNumber;
            tiantangLoginInfo.AccessToken = token;
            logger.LogInformation($"正在保存 {phoneNumber} 的记录到数据库");
            defaultDbContext.Add(tiantangLoginInfo);
            defaultDbContext.SaveChanges();
        }

        public void UpdateNotificationKeys(SetNotificationChannelRequest setNotificationChannelRequest)
        {
            logger.LogInformation($"正在更新通知通道密钥\n{JsonConvert.SerializeObject(setNotificationChannelRequest)}");

            defaultDbContext.PushChannelKeys.RemoveRange(defaultDbContext.PushChannelKeys);
            List<PushChannelConfiguration> pushChannelConfigurations = new List<PushChannelConfiguration>
            {
                new PushChannelConfiguration(NotificationChannelNames.ServerChan, setNotificationChannelRequest.ServerChan.Token),
                new PushChannelConfiguration(NotificationChannelNames.Bark, setNotificationChannelRequest.Bark.Token),
            };

            defaultDbContext.UpdateRange(pushChannelConfigurations);
            defaultDbContext.SaveChanges();
        }

        public async Task TestNotificationChannels()
        {
            var notificationBody = new NotificationBody("通知测试第一行\n通知测试第二行");
            await notificationRemoteCallService.SendNotificationViaServerChan(notificationBody);
            await notificationRemoteCallService.SendNotificationViaBark(notificationBody);
        }

        public TiantangLoginInfo GetCurrentLoginInfo()
        {
            TiantangLoginInfo tiantangLoginInfo = defaultDbContext.TiantangLoginInfo.SingleOrDefault();
            if (tiantangLoginInfo == null)
            {
                return null;
            }

            return tiantangLoginInfo;
        }

        public SetNotificationChannelRequest GetNotificationKeys()
        {
            var PushChannelConfigurations = defaultDbContext.PushChannelKeys.ToList<PushChannelConfiguration>();
            var response = new SetNotificationChannelRequest();
            foreach (var pushChannelConfiguration in PushChannelConfigurations)
            {
                switch (pushChannelConfiguration.ServiceName)
                {
                    case Constants.NotificationChannelNames.ServerChan:
                        response.ServerChan = new SetNotificationChannelRequest.NotificationChannelConfig(pushChannelConfiguration.Token);
                        break;
                    case Constants.NotificationChannelNames.Bark:
                        response.Bark = new SetNotificationChannelRequest.NotificationChannelConfig(pushChannelConfiguration.Token);
                        break;
                    default:
                        logger.LogWarning($"未知的通知渠道{pushChannelConfiguration.ServiceName}");
                        break;
                }
            }

            return response;
        }

        private void EnsureSuccessfulResponse(HttpResponseMessage response)
        {
            HttpStatusCode statusCode = response.StatusCode;
            string responseBody = response.Content.ReadAsStringAsync().Result;
            JsonDocument responseJson = JsonDocument.Parse(responseBody);
            int errCode = responseJson.RootElement.GetProperty("errCode").GetInt32();
            string errorMessage = responseJson.RootElement.GetProperty("msg").GetString();

            if (statusCode != HttpStatusCode.OK)
            {

                logger.LogError($"请求失败，HTTP返回码 {statusCode} ，错误信息：{errorMessage}");
                throw new ExternalAPICallException(errorMessage, statusCode);
            }

            if (errCode != 0)
            {
                logger.LogError($"甜糖API返回码不为0，错误信息：{errorMessage}");
                throw new ExternalAPICallException(errorMessage);
            }
        }
    }
}
