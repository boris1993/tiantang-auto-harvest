using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private readonly TiantangLoginInfoDbContext tiantangLoginInfoDbContext;
        private readonly HttpClient httpClient;

        public AppService(
            ILogger<AppService> logger,
            TiantangLoginInfoDbContext tiantangLoginInfoDbContext,
            HttpClient httpClient
        )
        {
            this.logger = logger;
            this.tiantangLoginInfoDbContext = tiantangLoginInfoDbContext;
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
            tiantangLoginInfoDbContext.TiantangLoginInfo.RemoveRange(tiantangLoginInfoDbContext.TiantangLoginInfo);
            TiantangLoginInfo tiantangLoginInfo = tiantangLoginInfo = new TiantangLoginInfo();
            tiantangLoginInfo.PhoneNumber = phoneNumber;
            tiantangLoginInfo.AccessToken = token;
            logger.LogInformation($"正在保存 {phoneNumber} 的记录到数据库");
            tiantangLoginInfoDbContext.Add(tiantangLoginInfo);
            tiantangLoginInfoDbContext.SaveChanges();
        }

        public TiantangLoginInfo GetCurrentLoginInfo()
        {
            TiantangLoginInfo tiantangLoginInfo = tiantangLoginInfoDbContext.TiantangLoginInfo.SingleOrDefault();
            if (tiantangLoginInfo == null)
            {
                return null;
            }

            return tiantangLoginInfo;
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
                
                logger.LogError($"验证码发送失败，HTTP返回码 {statusCode} ，错误信息：{errorMessage}");
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
