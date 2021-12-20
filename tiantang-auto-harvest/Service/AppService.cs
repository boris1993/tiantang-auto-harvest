using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;

namespace tiantang_auto_harvest.Service
{
    public class AppService
    {
        private readonly ILogger logger;
        private readonly DefaultDbContext defaultDbContext;
        private readonly HttpClient httpClient;

        public AppService(
            ILogger<AppService> logger,
            DefaultDbContext tiantangLoginInfoDbContext,
            HttpClient httpClient
        )
        {
            this.logger = logger;
            this.defaultDbContext = tiantangLoginInfoDbContext;
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

        public void UpdateNotificationKeys(PushChannelKeys pushChannelKeysRequest)
        {
            logger.LogInformation($"正在更新通知通道密钥\n{JsonConvert.SerializeObject(pushChannelKeysRequest)}");

            PushChannelKeys pushChannelKeys = defaultDbContext.PushChannelKeys.FirstOrDefault();
            if (pushChannelKeys == null)
            {
                pushChannelKeys = new PushChannelKeys();
            }

            pushChannelKeys.ServerChanSendKey = pushChannelKeysRequest.ServerChanSendKey;
            pushChannelKeys.TelegramBotToken = pushChannelKeysRequest.TelegramBotToken;
            defaultDbContext.Update(pushChannelKeys);
            defaultDbContext.SaveChanges();
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

        public PushChannelKeys GetNotificationKeys()
        {
            return defaultDbContext.PushChannelKeys.FirstOrDefault() ?? new PushChannelKeys();
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
