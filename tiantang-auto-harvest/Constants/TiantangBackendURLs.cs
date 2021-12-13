namespace tiantang_auto_harvest.Constants
{
    public class TiantangBackendURLs
    {
        public const string BaseURL = "http://tiantang.mogencloud.com";
        public const string SendSMSURL = $"{BaseURL}/web/api/login/code";
        public const string VerifySMSCodeURL = $"{BaseURL}/web/api/login";
        public const string DailyCheckInURL = $"{BaseURL}/web/api/account/sign_in";
        public const string UserInfoURL = $"{BaseURL}/web/api/account/message/loading";
        public const string DevicesListURL = $"{BaseURL}/api/v1/devices";
        public const string DeviceLogsURL = $"{BaseURL}/api/v1/device_logs";
        public const string HarvestPromotionScores = $"{BaseURL}/api/v1/promote/score_logs";
        public const string HarvestDeviceScores = $"{BaseURL}/api/v1/score_logs";
    }

    public class ErrorMessages
    {
        public const string FieldCannotBeEmpty = "%s 不可为空";
    }
}
