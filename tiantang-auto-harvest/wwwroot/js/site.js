// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
function sendSMSCode() {
    document.getElementById("get_sms_code").disabled = true;
    
    let phoneNumber = document.getElementById("phone_number").value;
    let captchaId = document.getElementById("captcha_id").value;
    let captchaCode = document.getElementById("captcha_code").value;
    
    let xhr = new XMLHttpRequest();
    xhr.open("POST", "/api/SendSMS", true);
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.send(JSON.stringify({
        phoneNumber: phoneNumber,
        captchaId: captchaId,
        captchaCode: captchaCode,
    }));

    xhr.onreadystatechange = function () {
        if (xhr.readyState === XMLHttpRequest.DONE) {
            document.getElementById("get_sms_code").disabled = false;
            
            if (xhr.status === 400) {
                let responseJson = JSON.parse(xhr.responseText);

                let alertMessage = "";
                for (const [key, value] of Object.entries(responseJson["errors"])) {
                    alertMessage += `${value}\n`;
                }
                
                alert(alertMessage);
            } else if (xhr.status !== 200) {
                let alertMessage = "服务器出现错误：\n" + xhr.responseText;
                alert(alertMessage);
            }
        }
    }
}

function getCaptchaImage() {
    document.getElementById("get_captcha_image").disabled = true;
    document.getElementById("captcha_image").src = "";
    
    let xhr = new XMLHttpRequest();
    xhr.open("GET", "/Api/GetCaptchaImage", true);
    xhr.send();
    xhr.onreadystatechange = function () {
        if (xhr.readyState === XMLHttpRequest.DONE) {
            if (xhr.status !== 200) {
                let alertMessage = "服务器出现错误：\n" + xhr.responseText;
                alert(alertMessage);

                return;
            }

            let responseText = xhr.responseText;
            if (responseText) {
                let responseJson = JSON.parse(responseText);
                document.getElementById("captcha_image").src = responseJson["captchaUrl"];
                document.getElementById("captcha_id").value = responseJson["captchaId"];
                
                document.getElementById("captcha_div").style.display = "block";
                document.getElementById("captcha_div").style.textAlign = "center";
            }
        }

        document.getElementById("get_captcha_image").disabled = false;
    }
}

function verifySMSCode() {
    document.getElementById("verify_sms_code").disabled = true;
    
    let phoneNumber = document.getElementById("phone_number").value;
    let smsCode = document.getElementById("sms_code").value;
    let xhr = new XMLHttpRequest();
    xhr.open("POST", "/api/VerifyCode", true);
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.send(JSON.stringify({
        phoneNumber: phoneNumber,
        otpCode: smsCode
    }));

    xhr.onreadystatechange = function () {
        if (xhr.readyState === XMLHttpRequest.DONE) {
            document.getElementById("verify_sms_code").disabled = false;
            
            if (xhr.status === 400) {
                let responseJson = JSON.parse(xhr.responseText);

                let alertMessage = "";
                for (const [key, value] of Object.entries(responseJson["errors"])) {
                    alertMessage += `${value}\n`;
                }
                
                alert(alertMessage);

                return;
            } else if (xhr.status !== 200) {
                let alertMessage = "服务器出现错误：\n" + xhr.responseText;
                alert(alertMessage);

                return;
            }

            window.location.reload();
        }
    }
}

function getCurrentLogin() {
    let xhr = new XMLHttpRequest();
    xhr.open("GET", "/api/GetLoginInfo", true);
    xhr.send();

    xhr.onreadystatechange = function () {
        if (xhr.readyState === XMLHttpRequest.DONE) {
            let responseText = xhr.responseText;
            if (xhr.status !== 200) {
                let alertMessage = "服务器出现错误：\n" + responseText;
                alert(alertMessage);
                return;
            }

            if (responseText) {
                let responseJson = JSON.parse(responseText);
                document.getElementById("current_phone_number").value = responseJson["phoneNumber"];
            }
        }
    }
}

function updateNotificationKeys() {
    let serverChanSendKey = document.getElementById("server_chan_send_key").value;
    let barkToken = document.getElementById("bark_token").value;
    let dingTalkAccessToken = document.getElementById('dingtalk_token').value;
    let dingTalkSecret = document.getElementById('dingtalk_secret').value;

    let xhr = new XMLHttpRequest();
    xhr.open("POST", "/api/UpdateNotificationChannels", true);
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.send(JSON.stringify({
        ServerChan: serverChanSendKey,
        Bark: barkToken,
        DingTalk: {
            AccessToken: dingTalkAccessToken,
            Secret: dingTalkSecret
        }
    }));

    xhr.onreadystatechange = function () {
        if (xhr.readyState === XMLHttpRequest.DONE) {
            if (xhr.status !== 200) {
                let alertMessage = "服务器出现错误：\n" + xhr.responseText;
                alert(alertMessage);
                return;
            }

            window.location.reload();
        }
    }
}

function testNotificationKeys() {
    var xhr = new XMLHttpRequest();
    xhr.open("GET", "/api/TestNotificationChannels", true);
    xhr.send();
}

function loadNotificationKeys() {
    let xhr = new XMLHttpRequest();
    xhr.open("GET", "/api/GetNotificationKeys", true);
    xhr.send();

    xhr.onreadystatechange = function () {
        if (xhr.readyState === XMLHttpRequest.DONE) {
            let responseText = xhr.responseText;
            if (xhr.status !== 200) {
                let alertMessage = "服务器出现错误：\n" + responseText;
                alert(alertMessage);
                return;
            }

            if (responseText) {
                let responseJson = JSON.parse(responseText);

                let serverChanSendKey = responseJson["serverChan"];
                let barkToken = responseJson["bark"];

                let dingTalk = responseJson["dingTalk"];
                let dingTalkAccessToken = dingTalk ? dingTalk["accessToken"] : "";
                let dingTalkSecret = dingTalk ? dingTalk["secret"] : "";

                document.getElementById("server_chan_send_key").value = serverChanSendKey;
                document.getElementById("bark_token").value = barkToken;
                document.getElementById("dingtalk_token").value = dingTalkAccessToken;
                document.getElementById("dingtalk_secret").value = dingTalkSecret;
            }
        }
    }
}

window.onload = function () {
    getCurrentLogin();
    loadNotificationKeys();
}
// Write your JavaScript code.
