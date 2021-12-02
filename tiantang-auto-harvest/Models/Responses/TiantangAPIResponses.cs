using Microsoft.AspNetCore.Identity;

namespace tiantang_auto_harvest.Models.Responses
{
    public class ErrorResponse
    {
        public string Message { get; set; }

        public ErrorResponse(string message)
        {
            Message = message;
        }
    }

    public class TiantangAPIResponse<T>
    {
        public string Msg { get; set; }
        public int ErrCode { get; set; }
        public T Data { get; set; }
    }

    public class EmptyResponseData
    {
    }

    public class VerifySMSCodeResponse
    {
        public int Id { get; set; }
        public string UnionId { get; set; }
        public string NickName { get; set; }
        public string Role { get; set; }
        public string ChannelId { get; set; }
        public string PhoneNum { get; set; }
        public string Site { get; set; }
        public string HeadPortrait { get; set; }
        public string BankCard { get; set; }
        public bool IsInit { get; set; }
        public int AddUpScore { get; set; }
        public int Score { get; set; }
        public int InactivedPromoteScore { get; set; }
        public int PromoteScore { get; set; }
        public string PromoteCode { get; set; }
        public int InviteUserId { get; set; }
        public string InviteCode { get; set; }
    }
}
