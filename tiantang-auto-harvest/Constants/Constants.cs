namespace tiantang_auto_harvest.Constants
{
    public class TiantangBackendURLs
    {
        private TiantangBackendURLs() { }

        public const string BaseURL = "http://tiantang.mogencloud.com";
        public const string SendSMSURL = $"{BaseURL}/web/api/login/code";
        public const string VerifySMSCodeURL = $"{BaseURL}/web/api/login";
        public const string DailyCheckInURL = $"{BaseURL}/web/api/account/sign_in";
        public const string UserInfoURL = $"{BaseURL}/web/api/account/message/loading";
        public const string DevicesListURL = $"{BaseURL}/api/v1/devices";
        public const string DeviceLogsURL = $"{BaseURL}/api/v1/device_logs";
        public const string HarvestPromotionScores = $"{BaseURL}/api/v1/promote/score_logs";
        public const string HarvestDeviceScores = $"{BaseURL}/api/v1/score_logs";
        public const string GetActivatedBonusCards = $"{BaseURL}/api/v1/user_props";
        public const string GetActivatedBonusCardStatus = $"{BaseURL}/api/v1/user_props/actived";
        public const string ActiveElectricBillBonusCard = $"{BaseURL}/api/v1/user_props/{TiantangBonusCardTypes.ElectricBillBonus}/actived";
    }

    public class TiantangBonusCardTypes
    {
        private TiantangBonusCardTypes() { }

        /// <summary>
        /// 星愿加成卡
        /// </summary>
        public const string Multiplier = "2";

        /// <summary>
        /// 电费卡
        /// </summary>
        public const string ElectricBillBonus = "5";
    }

    public class NotificationURLs
    {
        /// <summary>
        /// Server酱推送地址
        /// </summary>
        public const string ServerChan = "https://sctapi.ftqq.com/{0}.send";

        /// <summary>
        /// Bark推送地址。{0}为推送Token，{1}为标题，{2}为内容
        /// </summary>
        public const string Bark = "https://api.day.app/{0}/{1}/{2}";
    }

    public class NotificationChannelNames
    {
        public const string ServerChan = "serverchan";
        public const string Bark = "bark";
    }

    public class ErrorMessages
    {
        public const string FieldCannotBeEmpty = "%s 不可为空";
    }
}
