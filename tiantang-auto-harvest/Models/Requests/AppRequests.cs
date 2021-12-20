using System.ComponentModel.DataAnnotations;

namespace tiantang_auto_harvest.Models.Requests
{
    public class SendSMSRequest
    {
        [Required(ErrorMessage = "手机号码不可为空")]
        [Phone(ErrorMessage = "无效的手机号码")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "手机号码不足或超出11位")]
        public string PhoneNumber { get; set; }
    }

    public class VerifyCodeRequest
    {
        [Required(ErrorMessage = "手机号码不可为空")]
        [Phone(ErrorMessage = "无效的手机号码")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "手机号码不足或超出11位")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "短信验证码不可为空")]
        [RegularExpression(@"(?<!\S)\d{6}(?!\S)", ErrorMessage = "无效的短信验证码")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "短信验证码不足或超出6位")]
        public string OTPCode { get; set; }
    }

    public class AddNotificationChannelRequest
    {
        public string ServerChanSendKey { get; set; }
        public string TelegramBotToken { get; set; }
    }
}
