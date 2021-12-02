using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using tiantang_auto_harvest.Models;

namespace tiantang_auto_harvest.Jobs
{
    [DisallowConcurrentExecution]
    public class SigninJob : IJob
    {
        private readonly ILogger<SigninJob> logger;
        private readonly HttpClient httpClient;
        private readonly IServiceProvider serviceProvider;

        public SigninJob(ILogger<SigninJob> logger, HttpClient httpClient, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.httpClient = httpClient;
            this.serviceProvider = serviceProvider;
        }

        public Task Execute(IJobExecutionContext context)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var tiantangLoginInfoDbContext = scope.ServiceProvider.GetService<TiantangLoginInfoDbContext>();

                TiantangLoginInfo tiantangLoginInfo = tiantangLoginInfoDbContext.TiantangLoginInfo.FirstOrDefault();
                if (tiantangLoginInfo == null)
                {
                    logger.LogInformation("未登录甜糖账号，跳过签到");
                    return Task.CompletedTask;
                }

                logger.LogInformation($"将签到甜糖账号 {tiantangLoginInfo.PhoneNumber}");

                Uri uri = new Uri(Constants.TiantangBackendURLs.DailyCheckInURL);
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = uri,
                    Headers =
                {
                    {  HttpRequestHeader.Authorization.ToString(), tiantangLoginInfo.AccessToken }
                }
                };

                HttpResponseMessage response = httpClient.SendAsync(httpRequestMessage).Result;
                HttpStatusCode httpStatusCode = response.StatusCode;
                string responseBody = response.Content.ReadAsStringAsync().Result;
                JsonDocument responseJson = JsonDocument.Parse(responseBody);
                int errCode = responseJson.RootElement.GetProperty("errCode").GetInt32();
                string errorMessage = responseJson.RootElement.GetProperty("msg").GetString();

                if (httpStatusCode != HttpStatusCode.OK)
                {
                    logger.LogError($"签到失败，状态码 {httpStatusCode}，错误信息：{errorMessage}");
                    return Task.CompletedTask;
                }

                if (errCode != 0)
                {
                    logger.LogError($"甜糖API返回码不为0，错误信息：{errorMessage}");
                    return Task.CompletedTask;
                }

                int earnedScore = responseJson.RootElement.GetProperty("data").GetInt32();
                logger.LogInformation($"签到成功，获得{earnedScore}点星愿");

                return Task.CompletedTask;
            }
        }
    }
}
