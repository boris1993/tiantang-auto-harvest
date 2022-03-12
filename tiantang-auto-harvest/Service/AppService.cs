using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Models.Requests;

namespace tiantang_auto_harvest.Service
{
    public class AppService
    {
        private readonly ILogger logger;
        private readonly DefaultDbContext defaultDbContext;
        private readonly TiantangRemoteCallService tiantangRemoteCallService;
        private readonly NotificationRemoteCallService notificationRemoteCallService;
        private readonly HttpClient httpClient;

        public AppService(
            ILogger<AppService> logger,
            DefaultDbContext defaultDbContext,
            TiantangRemoteCallService tiantangRemoteCallService,
            NotificationRemoteCallService notificationRemoteCallService,
            HttpClient httpClient
        )
        {
            this.logger = logger;
            this.defaultDbContext = defaultDbContext;
            this.tiantangRemoteCallService = tiantangRemoteCallService;
            this.notificationRemoteCallService = notificationRemoteCallService;
            this.httpClient = httpClient;
        }

        public async Task RetrieveSMSCode(string phoneNumber)
        {
            logger.LogInformation($"正在向 {phoneNumber} 发送验证码短信");

            UriBuilder uriBuilder = new UriBuilder(Constants.TiantangBackendURLs.SendSmsUrl);
            uriBuilder.Query = $"phone={phoneNumber}";
            Uri uri = uriBuilder.Uri;

            HttpResponseMessage response = await httpClient.PostAsync(uri, null);
            EnsureSuccessfulResponse(response);

            logger.LogInformation("短信发送成功");
        }

        public async Task VerifySMSCode(string phoneNumber, string smsCode)
        {
            logger.LogInformation($"正在校验验证码 {smsCode}");

            UriBuilder uriBuilder = new UriBuilder(Constants.TiantangBackendURLs.VerifySmsCodeUrl);
            uriBuilder.Query = $"phone={phoneNumber}&authCode={smsCode}";
            Uri uri = uriBuilder.Uri;

            HttpResponseMessage response = await httpClient.PostAsync(uri, null);
            EnsureSuccessfulResponse(response);

            string responseBody = await response.Content.ReadAsStringAsync();
            JsonDocument responseJson = JsonDocument.Parse(responseBody);

            string token = responseJson.RootElement.GetProperty("data").GetProperty("token").GetString();
            string unionId = responseJson.RootElement.GetProperty("data").GetProperty("union_id").GetString();
            logger.LogInformation($"Token是 {token} , union ID是 {unionId}");

            // Remove all records before inserting the new one
            defaultDbContext.TiantangLoginInfo.RemoveRange(defaultDbContext.TiantangLoginInfo);
            TiantangLoginInfo tiantangLoginInfo = new TiantangLoginInfo
            {
                PhoneNumber = phoneNumber,
                AccessToken = token,
                UnionId = unionId
            };
            logger.LogInformation($"正在保存 {phoneNumber} 的记录到数据库");
            defaultDbContext.Add(tiantangLoginInfo);
            defaultDbContext.SaveChanges();
        }

        public void RefreshLogin()
        {
            TiantangLoginInfo tiantangLoginInfo = defaultDbContext.TiantangLoginInfo.SingleOrDefault();
            if (tiantangLoginInfo == null)
            {
                return;
            }

            logger.LogInformation($"正在刷新{tiantangLoginInfo.PhoneNumber}的token");

            string unionId = tiantangLoginInfo.UnionId;
            JsonDocument responseJson = tiantangRemoteCallService.RefreshLogin(unionId);

            string newToken = responseJson.RootElement.GetProperty("data").GetProperty("token").GetString();
            tiantangLoginInfo.AccessToken = newToken;
            logger.LogInformation($"新token是 {newToken}");

            defaultDbContext.SaveChanges();
        }

        public void UpdateNotificationKeys(SetNotificationChannelRequest setNotificationChannelRequest)
        {
            logger.LogInformation($"正在更新通知通道密钥\n{JsonConvert.SerializeObject(setNotificationChannelRequest)}");

            defaultDbContext.PushChannelKeys.RemoveRange(defaultDbContext.PushChannelKeys);
            List<PushChannelConfiguration> pushChannelConfigurations = new List<PushChannelConfiguration>
            {
                new(Constants.NotificationChannelNames.ServerChan,
                    setNotificationChannelRequest.ServerChan),
                
                new(Constants.NotificationChannelNames.Bark,
                    setNotificationChannelRequest.Bark),
                
                new(Constants.NotificationChannelNames.DingTalk,
                    setNotificationChannelRequest.DingTalk.AccessToken, 
                    setNotificationChannelRequest.DingTalk.Secret)
            };

            defaultDbContext.UpdateRange(pushChannelConfigurations);
            defaultDbContext.SaveChanges();
        }

        public async Task TestNotificationChannels()
        {
            var notificationBody = new NotificationBody("通知测试第一行\n通知测试第二行");
            await notificationRemoteCallService.SendNotificationToAllChannels(notificationBody);
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
                        response.ServerChan = pushChannelConfiguration.Token;
                        break;
                    case Constants.NotificationChannelNames.Bark:
                        response.Bark = pushChannelConfiguration.Token;
                        break;
                    case Constants.NotificationChannelNames.DingTalk:
                        response.DingTalk = new SetNotificationChannelRequest.DingTalkToken
                        {
                            AccessToken = pushChannelConfiguration.Token,
                            Secret = pushChannelConfiguration.Secret
                        };
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
                throw new ExternalApiCallException(errorMessage, statusCode);
            }

            if (errCode != 0)
            {
                logger.LogError($"甜糖API返回码不为0，错误信息：{errorMessage}");
                throw new ExternalApiCallException(errorMessage);
            }
        }
    }
}