using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using tiantang_auto_harvest.Constants;
using tiantang_auto_harvest.Exceptions;

namespace tiantang_auto_harvest.Service
{
    public class TiantangRemoteCallService
    {
        private readonly ILogger<TiantangRemoteCallService> _logger;
        private readonly HttpClient _httpClient;

        public TiantangRemoteCallService(ILogger<TiantangRemoteCallService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<JsonDocument> RefreshLogin(string unionId, CancellationToken cancellationToken) =>
            await SendRequestWithoutToken(
                new Uri(TiantangBackendURLs.RefreshLogin),
                HttpMethod.Post,
                cancellationToken,
                JsonContent.Create(new {union_id = unionId}));

        public async Task<JsonDocument> DailyCheckIn(string accessToken, CancellationToken cancellationToken) =>
            await SendRequest(new Uri(TiantangBackendURLs.DailyCheckInUrl),
                HttpMethod.Post,
                accessToken,
                cancellationToken);

        public async Task<JsonDocument> RetrieveUserInfo(string accessToken, CancellationToken cancellationToken) =>
            await SendRequest(new Uri(TiantangBackendURLs.UserInfoUrl),
                HttpMethod.Post,
                accessToken,
                cancellationToken);

        public async Task<JsonDocument> RetrieveNodes(string accessToken, CancellationToken cancellationToken) =>
            await SendRequest(new Uri(TiantangBackendURLs.DevicesListUrl), HttpMethod.Get, accessToken, cancellationToken);

        public async Task<JsonDocument> HarvestPromotionScore(int promotionScore, string accessToken, CancellationToken cancellationToken)
        {
            var body = new Dictionary<string, int>
            {
                ["score"] = promotionScore,
            };

            return await SendRequest(new Uri(TiantangBackendURLs.HarvestPromotionScores),
                HttpMethod.Post,
                accessToken,
                cancellationToken,
                body);
        }

        public async Task<JsonDocument> RetrieveAllBonusCards(string accessToken, CancellationToken cancellationToken) =>
            await SendRequest(new Uri(TiantangBackendURLs.GetActivatedBonusCards),
                HttpMethod.Get,
                accessToken,
                cancellationToken);

        public async Task<JsonDocument> RetrieveActivatedBonusCards(string accessToken, CancellationToken cancellationToken) =>
            await SendRequest(new Uri(TiantangBackendURLs.GetActivatedBonusCardStatus),
                HttpMethod.Get,
                accessToken,
                cancellationToken);

        public async Task HarvestDeviceScore(Dictionary<string, int> devices, string accessToken, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("TiantangRemoteCallService#HarvestDeviceScore被cancel", null, cancellationToken);
            }
            
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
                await SendRequest(uri,
                    HttpMethod.Post,
                    accessToken,
                    cancellationToken,
                    body);
            }
        }

        public async Task ActiveElectricBillBonusCard(string accessToken, CancellationToken cancellationToken) =>
            await SendRequest(new Uri(TiantangBackendURLs.ActiveElectricBillBonusCard),
                HttpMethod.Put,
                accessToken,
                cancellationToken);

        private async Task<JsonDocument> SendRequest(
            Uri uri,
            HttpMethod httpMethod,
            string accessToken,
            CancellationToken cancellationToken,
            object body = null
        )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("SendRequest被cancel", null, cancellationToken);
            }
            
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

            try
            {
                using var response = await _httpClient.SendAsync(httpRequestMessage);
                _logger.LogDebug("Response = {Response}", response.ToString());

                await EnsureSuccessfulResponse(response, cancellationToken);
                return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) when (
                ex is ArgumentNullException
                      or InvalidOperationException
                      or HttpRequestException
                      or TaskCancelledException)
            {
                _logger.LogError("SendRequest方法发送请求失败。错误信息：{exception}", ex);
                throw;
            }
            catch (AggregateException ex)
            {
                _logger.LogError($"SendRequest方法发送请求失败。错误信息：");
                ex.InnerExceptions
                    .Select(innerException => innerException.Message)
                    .ToList()
                    .ForEach(message => _logger.LogError("{message}", message));
                throw;
            }
        }

        private async Task<JsonDocument> SendRequestWithoutToken(
            Uri uri,
            HttpMethod httpMethod,
            CancellationToken cancellationToken,
            HttpContent body = default
        )
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("SendRequestWithoutToken被cancel", null, cancellationToken);
            }
            
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = httpMethod,
                RequestUri = uri,
                Content = body,
            };
            _logger.LogDebug("httpRequestMessage = {HttpRequestMessage}", httpRequestMessage);

            try
            {
                using var response = await _httpClient.SendAsync(httpRequestMessage, cancellationToken);
                _logger.LogDebug("Response = {Response}", response.ToString());
                
                await EnsureSuccessfulResponse(response, cancellationToken);
                return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) when (
                ex is ArgumentNullException
                      or InvalidOperationException
                      or HttpRequestException
                      or TaskCancelledException)
            {
                _logger.LogError("SendRequest方法发送请求失败。错误信息：{exception}", ex);
                throw;
            }
            catch (AggregateException ex)
            {
                _logger.LogError($"SendRequest方法发送请求失败。错误信息：");
                ex.InnerExceptions
                    .Select(innerException => innerException.Message)
                    .ToList()
                    .ForEach(message => _logger.LogError(message));
                throw;
            }
        }

        private async Task EnsureSuccessfulResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("EnsureSuccessfulResponse被cancel", null, cancellationToken);
            }
            
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = response.StatusCode;
                _logger.LogError("请求失败，HTTP返回码 {StatusCode} ，错误信息：{ErrorMessage}", statusCode);
                throw new ExternalApiCallException(statusCode);
            }
            
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("ResponseBody = {ResponseBody}", responseBody);

            var responseJson = JsonDocument.Parse(responseBody);
            var errCode = responseJson.RootElement.GetProperty("errCode").GetInt32();
            var errorMessage = responseJson.RootElement.GetProperty("msg").GetString();

            _logger.LogDebug("HTTP状态码为 {StatusCode}", response.StatusCode);
            _logger.LogDebug("errCode={ErrCode}, errorMessage={ErrorMessage}", errCode, errorMessage);

            if (errCode != 0)
            {
                _logger.LogError("甜糖API返回码不为0，错误信息：{ErrorMessage}", errorMessage);
                throw new ExternalApiCallException(errorMessage);
            }
        }
    }
}
