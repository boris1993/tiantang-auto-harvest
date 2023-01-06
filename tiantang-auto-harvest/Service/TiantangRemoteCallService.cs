using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using tiantang_auto_harvest.Constants;
using tiantang_auto_harvest.Exceptions;

namespace tiantang_auto_harvest.Service
{
    public class TiantangRemoteCallService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public TiantangRemoteCallService(ILogger<TiantangRemoteCallService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public JsonDocument RefreshLogin(string unionId) =>
            SendRequestWithoutToken(new Uri(TiantangBackendURLs.RefreshLogin), HttpMethod.Post, JsonContent.Create(new {union_id = unionId}));

        public JsonDocument DailyCheckIn(string accessToken) =>
            SendRequest(new Uri(TiantangBackendURLs.DailyCheckInUrl), HttpMethod.Post, accessToken);

        public JsonDocument RetrieveUserInfo(string accessToken) =>
            SendRequest(new Uri(TiantangBackendURLs.UserInfoUrl), HttpMethod.Post, accessToken);

        public JsonDocument RetrieveNodes(string accessToken) =>
            SendRequest(new Uri(TiantangBackendURLs.DevicesListUrl), HttpMethod.Get, accessToken);

        public JsonDocument HarvestPromotionScore(int promotionScore, string accessToken)
        {
            var body = new Dictionary<string, int>
            {
                ["score"] = promotionScore,
            };

            return SendRequest(new Uri(TiantangBackendURLs.HarvestPromotionScores), HttpMethod.Post, accessToken, body);
        }

        public JsonDocument RetrieveAllBonusCards(string accessToken) =>
            SendRequest(new Uri(TiantangBackendURLs.GetActivatedBonusCards), HttpMethod.Get, accessToken);

        public JsonDocument RetrieveActivatedBonusCards(string accessToken) =>
            SendRequest(new Uri(TiantangBackendURLs.GetActivatedBonusCardStatus), HttpMethod.Get, accessToken);

        public void HarvestDeviceScore(Dictionary<string, int> devices, string accessToken)
        {
            var uri = new Uri(TiantangBackendURLs.HarvestDeviceScores);
            foreach (var device in devices)
            {
                if (device.Value == 0)
                {
                    _logger.LogInformation("设备{DeviceKey}无可收取星愿", device.Key);
                    continue;
                }

                _logger.LogInformation("正在收取设备{DeviceKey}的{DeviceValue}点星愿", device.Key, device.Value);
                var body = new Dictionary<string, object>
                {
                    ["device_id"] = device.Key,
                    ["score"] = device.Value,
                };
                SendRequest(uri, HttpMethod.Post, accessToken, body);
            }
        }

        public void ActiveElectricBillBonusCard(string accessToken) =>
            SendRequest(new Uri(TiantangBackendURLs.ActiveElectricBillBonusCard), HttpMethod.Put, accessToken);

        private JsonDocument SendRequest(Uri uri, HttpMethod httpMethod, string accessToken, object body = null)
        {
            _logger.LogDebug("正在构造httpRequestMessage");
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = uri,
                Headers =
                {
                    {HttpRequestHeader.Authorization.ToString(), accessToken},
                },
                Content = JsonContent.Create(body),
            };
            _logger.LogDebug("httpRequestMessage = {HttpRequestMessage}", httpRequestMessage);

            var response = _httpClient.SendAsync(httpRequestMessage).Result;
            _logger.LogDebug("Response = {Response}", response.ToString());

            EnsureSuccessfulResponse(response, out var responseJson);
            return responseJson;
        }

        private JsonDocument SendRequestWithoutToken(Uri uri, HttpMethod httpMethod, HttpContent body = null)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = uri,
                Content = body,
            };
            var response = _httpClient.SendAsync(httpRequestMessage).Result;
            EnsureSuccessfulResponse(response, out var responseJson);
            return responseJson;
        }

        private void EnsureSuccessfulResponse(HttpResponseMessage response, out JsonDocument responseJson)
        {
            var responseBody = response.Content.ReadAsStringAsync().Result;
            _logger.LogDebug("ResponseBody = {ResponseBody}", responseBody);

            responseJson = JsonDocument.Parse(responseBody);
            var errCode = responseJson.RootElement.GetProperty("errCode").GetInt32();
            var errorMessage = responseJson.RootElement.GetProperty("msg").GetString();

            _logger.LogDebug("HTTP状态码为 {StatusCode}", response.StatusCode);
            _logger.LogDebug("errCode={ErrCode}, errorMessage={ErrorMessage}", errCode, errorMessage);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = response.StatusCode;
                _logger.LogError("请求失败，HTTP返回码 {StatusCode} ，错误信息：{ErrorMessage}", statusCode, errorMessage);
                throw new ExternalApiCallException(errorMessage, statusCode);
            }

            if (errCode != 0)
            {
                _logger.LogError("甜糖API返回码不为0，错误信息：{ErrorMessage}", errorMessage);
                throw new ExternalApiCallException(errorMessage);
            }
        }
    }
}
