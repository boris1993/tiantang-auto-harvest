using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Threading.Tasks;
using tiantang_auto_harvest.Exceptions;
using tiantang_auto_harvest.Models;
using tiantang_auto_harvest.Models.Requests;
using tiantang_auto_harvest.Models.Responses;
using tiantang_auto_harvest.Service;

namespace tiantang_auto_harvest.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> logger;
        private readonly AppService appService;

        public ApiController(ILogger<ApiController> logger, AppService appService)
        {
            this.logger = logger;
            this.appService = appService;
        }

        [HttpPost]
        public async Task<ActionResult> SendSMS(SendSMSRequest sendSMSRequest)
        {
            await appService.RetrieveSMSCode(sendSMSRequest.PhoneNumber);
            return new EmptyResult();
        }

        [HttpPost]
        public async Task<ActionResult> VerifyCode(VerifyCodeRequest verifyCodeRequest)
        {
            await appService.VerifySMSCode(verifyCodeRequest.PhoneNumber, verifyCodeRequest.OTPCode);
            return new EmptyResult();
        }

        [HttpGet]
        public ActionResult GetLoginInfo(bool showToken = false)
        {
            TiantangLoginInfo tiantangLoginInfo = appService.GetCurrentLoginInfo();
            if (tiantangLoginInfo == null)
            {
                return new EmptyResult();
            }

            LoginInfoResponse response = new LoginInfoResponse();
            response.PhoneNumber = tiantangLoginInfo.PhoneNumber;
            if (showToken)
            {
                response.Token = tiantangLoginInfo.AccessToken;
            } 
            else
            {
                response.Token = "MASKED";
            }

            return new JsonResult(response);
        }

        [HttpPost]
        public ActionResult RegisterServerChan(PushChannelKeys pushChannelKeys)
        {
            appService.UpdateNotificationKeys(pushChannelKeys);
            return new EmptyResult();
        }

        [HttpGet]
        public ActionResult GetNotificationKeys()
        {
            return new JsonResult(appService.GetNotificationKeys());
        }
    }
}
