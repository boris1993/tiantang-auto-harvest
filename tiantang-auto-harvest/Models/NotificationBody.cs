namespace tiantang_auto_harvest.Models
{
    public class NotificationBody
    {
        public NotificationBody() { }
        public NotificationBody(string content)
        {
            Content = content;
        }

        public string Title = "甜糖星愿自动收取";
        public string Content { get; set; }
    }
}
