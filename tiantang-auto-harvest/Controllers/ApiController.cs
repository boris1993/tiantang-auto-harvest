using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using tiantang_auto_harvest.Models.Requests;
using tiantang_auto_harvest.Models.Responses;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly AppService _appService;
        private readonly TiantangService _tiantangService;

        public ApiController(
            ILogger<ApiController> logger, 
            AppService appService, 
            TiantangService tiantangService)
        {
            _logger = logger;
            _appService = appService;
            _tiantangService = tiantangService;
        }

        [HttpGet]
        public async Task<ActionResult> GetCaptchaImage()
        {
            var (captchaId, captchaUrl) = await _appService.GetCaptchaImage();

            _logger.LogInformation("CaptchaId是{CaptchaId}", captchaId);
            return new ObjectResult(new {captchaId, captchaUrl});
        }

        [HttpPost]
        public async Task<ActionResult> SendSms(SendSMSRequest sendSmsRequest)
        {
            await _appService.RetrieveSMSCode(
                sendSmsRequest.PhoneNumber,
                sendSmsRequest.captchaId,
                sendSmsRequest.captchaCode);
            return new OkResult();
        }

        [HttpPost]
        public async Task<ActionResult> ManuallyRefreshLogin(CancellationToken cancellationToken)
        {
            await _appService.RefreshLogin(cancellationToken);
            return new OkResult();
        }

        [HttpPost]
        public async Task<ActionResult> VerifyCode(VerifyCodeRequest verifyCodeRequest)
        {
            await _appService.VerifySMSCode(verifyCodeRequest.PhoneNumber, verifyCodeRequest.OTPCode);
            return new OkResult();
        }

        [HttpGet]
        public ActionResult GetLoginInfo(bool showToken = false)
        {
            var tiantangLoginInfo = _appService.GetCurrentLoginInfo();
            if (tiantangLoginInfo == null)
            {
                return new EmptyResult();
            }

            var response = new LoginInfoResponse
            {
                PhoneNumber = tiantangLoginInfo.PhoneNumber,
                Token = showToken ? tiantangLoginInfo.AccessToken : "MASKED",
            };

            return new JsonResult(response);
        }

        [HttpPost]
        public ActionResult UpdateNotificationChannels(SetNotificationChannelRequest setNotificationChannelRequest)
        {
            _appService.UpdateNotificationKeys(setNotificationChannelRequest);
            return new OkResult();
        }

        [HttpGet]
        public async Task<ActionResult> TestNotificationChannels()
        {
            await _appService.TestNotificationChannels();
            return new OkResult();
        }

        [HttpGet]
        public ActionResult GetNotificationKeys() => new JsonResult(_appService.GetNotificationKeys());

        [HttpPost]
        public async Task<ActionResult> Signin(CancellationToken cancellationToken)
        {
            await _tiantangService.Signin(cancellationToken);
            return new OkResult();
        }

        [HttpPost]
        public async Task<ActionResult> Harvest(CancellationToken cancellationToken)
        {
            await _tiantangService.Harvest(cancellationToken);
            return new OkResult();
        }

        [HttpPost]
        public async Task<ActionResult> CheckAndApplyElectricBillBonus(CancellationToken cancellationToken)
        {
            await _tiantangService.CheckAndApplyElectricBillBonus(cancellationToken);
            return new OkResult();
        }

        [HttpPost]
        public async Task<ActionResult> RefreshLogin(CancellationToken cancellationToken)
        {
            await _tiantangService.RefreshLogin(cancellationToken);
            return new OkResult();
        }
    }
}
