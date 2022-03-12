// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
function sendSMSCode() {
    let phoneNumber = document.getElementById("phone_number").value;
    let xhr = new XMLHttpRequest();
    xhr.open("POST", "/api/SendSMS", true);
    xhr.setRequestHeader("Content-Type", "application/json");
    xhr.send(JSON.stringify({
        phoneNumber: phoneNumber
    }));

    xhr.onreadystatechange = function () {
        if (xhr.readyState === XMLHttpRequest.DONE) {
            if (xhr.status === 400) {
                let responseJson = JSON.parse(xhr.responseText);
                let errorMessages = responseJson["errors"]["PhoneNumber"];

                let alertMessage = "";
                for (let i = 0; i < errorMessages.length; i++) {
                    alertMessage += errorMessages[i] + "\n";
                }

                alert(alertMessage);
            } else if (xhr.status !== 200) {
                let alertMessage = "服务器出现错误：\n" + xhr.responseText;
                alert(alertMessage);
            }
        }
    }
}

function verifySMSCode() {
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
            if (xhr.status === 400) {
                let alertMessage = "";

                let responseJson = JSON.parse(xhr.responseText);
                let errorMessages = responseJson["errors"]["PhoneNumber"];
                for (let i = 0; i < errorMessages.length; i++) {
                    alertMessage += errorMessages[i] + "\n";
                }

                errorMessages = responseJson["errors"]["OTPCode"];
                for (let i = 0; i < errorMessages.length; i++) {
                    alertMessage += errorMessages[i] + "\n";
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
