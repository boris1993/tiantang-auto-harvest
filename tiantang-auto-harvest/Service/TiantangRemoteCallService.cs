using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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

        public JsonDocument RefreshLogin(string unionId)
        {
            return SendRequestWithoutToken(
                new Uri(Constants.TiantangBackendURLs.RefreshLogin),
                HttpMethod.Post,
                JsonContent.Create(new { union_id = unionId }));
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

        public JsonDocument HarvestPromotionScore(int promotionScore, string accessToken)
        {
            var body = new Dictionary<string, int>();
            body["score"] = promotionScore;

            return SendRequest(new Uri(Constants.TiantangBackendURLs.HarvestPromotionScores), HttpMethod.Post, accessToken, body);
        }

        public JsonDocument RetrieveAllBonusCards(string accessToken)
        {
            return SendRequest(new Uri(Constants.TiantangBackendURLs.GetActivatedBonusCards), HttpMethod.Get, accessToken);
        }

        public JsonDocument RetrieveActivatedBonusCards(string accessToken)
        {
            return SendRequest(new Uri(Constants.TiantangBackendURLs.GetActivatedBonusCardStatus), HttpMethod.Get, accessToken);
        }

        public void HarvestDeviceScore(Dictionary<string, int> devices, string accessToken)
        {
            var uri = new Uri(Constants.TiantangBackendURLs.HarvestDeviceScores);
            foreach (KeyValuePair<string, int> device in devices)
            {
                if (device.Value == 0)
                {
                    logger.LogInformation($"设备{device.Key}无可收取星愿");
                    continue;
                }

                logger.LogInformation($"正在收取设备{device.Key}的{device.Value}点星愿");
                var body = new Dictionary<string, object>();
                body["device_id"] = device.Key;
                body["score"] = device.Value;
                SendRequest(uri, HttpMethod.Post, accessToken, body);
            }
        }

        public void ActiveElectricBillBonusCard(string accessToken)
        {
            SendRequest(new Uri(Constants.TiantangBackendURLs.ActiveElectricBillBonusCard), HttpMethod.Put, accessToken);
        }

        private JsonDocument SendRequest(Uri uri, HttpMethod httpMethod, string accessToken, object body = null)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = uri,
                Headers =
                {
                    { HttpRequestHeader.Authorization.ToString(), accessToken }
                },
                Content = JsonContent.Create(body)
            };
            var response = httpClient.SendAsync(httpRequestMessage).Result;
            EnsureSuccessfulResponse(response, out JsonDocument responseJson);
            return responseJson;
        }

        private JsonDocument SendRequestWithoutToken(Uri uri, HttpMethod httpMethod, HttpContent body = null)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = uri,
                Content = body
            };
            var response = httpClient.SendAsync(httpRequestMessage).Result;
            EnsureSuccessfulResponse(response, out JsonDocument responseJson);
            return responseJson;
        }

        private void EnsureSuccessfulResponse(HttpResponseMessage response, out JsonDocument responseJson)
        {
            string responseBody = response.Content.ReadAsStringAsync().Result;
            responseJson = JsonDocument.Parse(responseBody);
            int errCode = responseJson.RootElement.GetProperty("errCode").GetInt32();
            string errorMessage = responseJson.RootElement.GetProperty("msg").GetString();

            if (!response.IsSuccessStatusCode)
            {
                HttpStatusCode statusCode = response.StatusCode;
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
