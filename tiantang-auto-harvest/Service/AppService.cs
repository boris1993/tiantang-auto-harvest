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
using Newtonsoft.Json;
using tiantang_auto_harvest.Constants;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Models.Requests;

namespace tiantang_auto_harvest.Service
{
    public class AppService
    {
        private readonly ILogger<AppService> _logger;
        private readonly DefaultDbContext _defaultDbContext;
        private readonly TiantangRemoteCallService _tiantangRemoteCallService;
        private readonly NotificationRemoteCallService _notificationRemoteCallService;
        private readonly HttpClient _httpClient;

        public AppService(
            ILogger<AppService> logger,
            DefaultDbContext defaultDbContext,
            TiantangRemoteCallService tiantangRemoteCallService,
            NotificationRemoteCallService notificationRemoteCallService,
            HttpClient httpClient)
        {
            _logger = logger;
            _defaultDbContext = defaultDbContext;
            _tiantangRemoteCallService = tiantangRemoteCallService;
            _notificationRemoteCallService = notificationRemoteCallService;
            _httpClient = httpClient;
        }

        public async Task<(string captchaId, string captchaUrl)> GetCaptchaImage()
        {
            _logger.LogInformation("正在获取验证码图片");

            var uri = new Uri(TiantangBackendURLs.GetCaptchaImageUrl);

            var response = await _httpClient.GetAsync(uri);
            EnsureSuccessfulResponse(response);

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseBody);

            var captchaId = responseJson.RootElement.GetProperty("data").GetProperty("captchaId").GetString();
            var captchaUrl = responseJson.RootElement.GetProperty("data").GetProperty("captchaUrl").GetString();
            var captchaFullUrl = $"{TiantangBackendURLs.BaseUrl}{captchaUrl}";

            return (captchaId, captchaFullUrl);
        }

        public async Task RetrieveSMSCode(string phone, string captchaId, string captchaCode)
        {
            _logger.LogInformation("正在向 {PhoneNumber} 发送验证码短信", phone);

            var uri = new Uri(TiantangBackendURLs.SendSmsUrl);
            var body = JsonContent.Create(new {phone, captchaId, captchaCode});

            await _httpClient.PostAsync(uri, body);

            _logger.LogInformation("短信发送成功");
        }

        public async Task VerifySMSCode(string phoneNumber, string smsCode)
        {
            _logger.LogInformation("正在校验验证码 {SmsCode}", smsCode);

            var uriBuilder = new UriBuilder(TiantangBackendURLs.VerifySmsCodeUrl)
            {
                Query = $"phone={phoneNumber}&authCode={smsCode}",
            };
            var uri = uriBuilder.Uri;

            var response = await _httpClient.PostAsync(uri, null);
            EnsureSuccessfulResponse(response);

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseJson = JsonDocument.Parse(responseBody);

            var token = responseJson.RootElement.GetProperty("data").GetProperty("token").GetString();
            var unionId = responseJson.RootElement.GetProperty("data").GetProperty("union_id").GetString();
            _logger.LogInformation("Token是 {Token} , union ID是 {UnionId}", token, unionId);

            // Remove all records before inserting the new one
            _defaultDbContext.TiantangLoginInfo.RemoveRange(_defaultDbContext.TiantangLoginInfo);
            var tiantangLoginInfo = new TiantangLoginInfo
            {
                PhoneNumber = phoneNumber,
                AccessToken = token,
                UnionId = unionId,
            };
            _logger.LogInformation("正在保存 {PhoneNumber} 的记录到数据库", phoneNumber);
            _defaultDbContext.Add(tiantangLoginInfo);
            await _defaultDbContext.SaveChangesAsync();
        }

        public async Task RefreshLogin(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("RefreshLogin被cancel", null, cancellationToken);
            }
            
            var tiantangLoginInfo = _defaultDbContext.TiantangLoginInfo.SingleOrDefault();
            if (tiantangLoginInfo == null)
            {
                return;
            }

            _logger.LogInformation("正在刷新{PhoneNumber}的token", tiantangLoginInfo.PhoneNumber);

            var unionId = tiantangLoginInfo.UnionId;
            var responseJson = await _tiantangRemoteCallService.RefreshLogin(unionId, cancellationToken);

            var newToken = responseJson.RootElement.GetProperty("data").GetProperty("token").GetString();
            tiantangLoginInfo.AccessToken = newToken;
            _logger.LogInformation("新token是 {NewToken}", newToken);

            await _defaultDbContext.SaveChangesAsync(cancellationToken);
        }

        public void UpdateNotificationKeys(SetNotificationChannelRequest setNotificationChannelRequest)
        {
            _logger.LogInformation("正在更新通知通道密钥\\n{SerializeObject}", JsonConvert.SerializeObject(setNotificationChannelRequest));

            _defaultDbContext.PushChannelKeys.RemoveRange(_defaultDbContext.PushChannelKeys);
            var pushChannelConfigurations = new List<PushChannelConfiguration>
            {
                new PushChannelConfiguration(NotificationChannelNames.ServerChan, setNotificationChannelRequest.ServerChan),
                new PushChannelConfiguration(NotificationChannelNames.Bark, setNotificationChannelRequest.Bark),
                new PushChannelConfiguration(NotificationChannelNames.DingTalk, setNotificationChannelRequest.DingTalk.AccessToken, setNotificationChannelRequest.DingTalk.Secret)
            };

            _defaultDbContext.UpdateRange(pushChannelConfigurations);
            _defaultDbContext.SaveChanges();
        }

        public async Task TestNotificationChannels()
        {
            var notificationBody = new NotificationBody("通知测试第一行\n通知测试第二行");
            await _notificationRemoteCallService.SendNotificationToAllChannels(notificationBody);
        }

        public TiantangLoginInfo GetCurrentLoginInfo()
        {
            var tiantangLoginInfo = _defaultDbContext.TiantangLoginInfo.SingleOrDefault();
            return tiantangLoginInfo;
        }

        public SetNotificationChannelRequest GetNotificationKeys()
        {
            var pushChannelConfigurations = _defaultDbContext.PushChannelKeys.ToList<PushChannelConfiguration>();
            var response = new SetNotificationChannelRequest();
            foreach (var pushChannelConfiguration in pushChannelConfigurations)
            {
                switch (pushChannelConfiguration.ServiceName)
                {
                    case NotificationChannelNames.ServerChan:
                        response.ServerChan = pushChannelConfiguration.Token;
                        break;
                    case NotificationChannelNames.Bark:
                        response.Bark = pushChannelConfiguration.Token;
                        break;
                    case NotificationChannelNames.DingTalk:
                        response.DingTalk = new SetNotificationChannelRequest.DingTalkToken
                        {
                            AccessToken = pushChannelConfiguration.Token,
                            Secret = pushChannelConfiguration.Secret
                        };
                        break;
                    default:
                        _logger.LogWarning("未知的通知渠道{ServiceName}", pushChannelConfiguration.ServiceName);
                        break;
                }
            }

            return response;
        }

        private void EnsureSuccessfulResponse(HttpResponseMessage response)
        {
            string responseBody;
            JsonDocument responseJson;
            string errorMessage;
            
            var statusCode = response.StatusCode;
            
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (statusCode)
            {
                case HttpStatusCode.BadGateway:
                    _logger.LogError("请求失败，HTTP返回码 {StatusCode}", statusCode);
                    throw new ExternalApiCallException("请求发送失败，可能是网络不稳定，请重试");
                case HttpStatusCode.OK:
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    responseJson = JsonDocument.Parse(responseBody);
                    var errCode = responseJson.RootElement.GetProperty("errCode").GetInt32();
                    errorMessage = responseJson.RootElement.GetProperty("msg").GetString();
                    
                    if (errCode != 0)
                    {
                        _logger.LogError("甜糖API返回码不为0，错误信息：{ErrorMessage}", errorMessage);
                        throw new ExternalApiCallException(errorMessage);
                    }
                    
                    break;
                default:
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    responseJson = JsonDocument.Parse(responseBody);
                    errorMessage = responseJson.RootElement.GetProperty("msg").GetString();
                        
                    _logger.LogError("请求失败，HTTP返回码 {StatusCode} ，错误信息：{ErrorMessage}", statusCode, errorMessage);
                    throw new ExternalApiCallException(errorMessage, statusCode);
            }
        }
    }
}
