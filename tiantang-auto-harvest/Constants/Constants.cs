namespace tiantang_auto_harvest.Constants
{
    public static class TiantangBackendURLs
    {
        public const string BaseUrl = "http://tiantang.mogencloud.com";
        public const string SendSmsUrl = $"{BaseUrl}/web/api/login/code";
        public const string VerifySmsCodeUrl = $"{BaseUrl}/web/api/login";
        public const string DailyCheckInUrl = $"{BaseUrl}/web/api/account/sign_in";
        public const string UserInfoUrl = $"{BaseUrl}/web/api/account/message/loading";
        public const string RefreshLogin = $"{BaseUrl}/api/v1/login";
        public const string DevicesListUrl = $"{BaseUrl}/api/v1/devices";
        public const string DeviceLogsUrl = $"{BaseUrl}/api/v1/device_logs";
        public const string HarvestPromotionScores = $"{BaseUrl}/api/v1/promote/score_logs";
        public const string HarvestDeviceScores = $"{BaseUrl}/api/v1/score_logs";
        public const string GetActivatedBonusCards = $"{BaseUrl}/api/v1/user_props";
        public const string GetActivatedBonusCardStatus = $"{BaseUrl}/api/v1/user_props/actived";
        public const string ActiveElectricBillBonusCard = $"{BaseUrl}/api/v1/user_props/{TiantangBonusCardTypes.ElectricBillBonus}/actived";
    }

    public static class TiantangBonusCardTypes
    {
        /// <summary>
        /// 星愿加成卡
        /// </summary>
        public const string Multiplier = "2";

        /// <summary>
        /// 电费卡
        /// </summary>
        public const string ElectricBillBonus = "5";
    }

    public static class NotificationURLs
    {
        /// <summary>
        /// Server酱推送地址
        /// </summary>
        public const string ServerChan = "https://sctapi.ftqq.com/{0}.send";

        /// <summary>
        /// Bark推送地址。{0}为推送Token，{1}为标题，{2}为内容
        /// </summary>
        public const string Bark = "https://api.day.app/{0}/{1}/{2}";

        /// <summary>
        /// 钉钉推送地址 - 加签方式
        /// </summary>
        public const string DingTalk = "https://oapi.dingtalk.com/robot/send?access_token={0}&timestamp={1}&sign={2}";
    }

    public static class NotificationChannelNames
    {
        public const string ServerChan = "serverchan";
        public const string Bark = "bark";
        public const string DingTalk = "DingTalk";
    }

    public class ErrorMessages
    {
        public const string FieldCannotBeEmpty = "%s 不可为空";
    }
}
