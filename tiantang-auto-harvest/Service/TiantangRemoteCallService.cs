using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using tiantang_auto_harvest.Exceptions;

namespace tiantang_auto_harvest.Service
{
    public class TiantangRemoteCallService
    {
        private readonly ILogger logger;
        private readonly HttpClient httpClient;
        public TiantangRemoteCallService(ILogger<AppService> logger, HttpClient httpClient)
        {
            this.logger = logger;
            this.httpClient = httpClient;
        }

        public JsonDocument DailyCheckIn(string accessToken)
        {
            return SendRequest(new Uri(Constants.TiantangBackendURLs.DailyCheckInURL), HttpMethod.Post, accessToken);
        }

        public JsonDocument RetrieveUserInfo(string accessToken)
        {
            return SendRequest(new Uri(Constants.TiantangBackendURLs.UserInfoURL), HttpMethod.Post, accessToken);
        }

        public JsonDocument RetrieveNodes(string accessToken)
        {
            return SendRequest(new Uri(Constants.TiantangBackendURLs.DevicesListURL), HttpMethod.Get, accessToken);
        }

        private JsonDocument SendRequest(Uri uri, HttpMethod httpMethod, string accessToken)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = uri,
                Headers =
                {
                    { HttpRequestHeader.Authorization.ToString(), accessToken }
                }
            };
            var response = httpClient.SendAsync(httpRequestMessage).Result;
            EnsureSuccessfulResponse(response, out JsonDocument responseJson);
            return responseJson;
        }

        private JsonDocument SendRequestWithoutToken(Uri uri, HttpMethod httpMethod)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = uri
            };
            var response = httpClient.SendAsync(httpRequestMessage).Result;
            EnsureSuccessfulResponse(response, out JsonDocument responseJson);
            return responseJson;
        }

        private void EnsureSuccessfulResponse(HttpResponseMessage response, out JsonDocument responseJson)
        {
            HttpStatusCode statusCode = response.StatusCode;
            string responseBody = response.Content.ReadAsStringAsync().Result;
            responseJson = JsonDocument.Parse(responseBody);
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
