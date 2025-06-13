using System;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 訊息接收事件參數類別
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        /// <summary>
        /// 接收到的訊息內容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 用戶端唯一識別碼
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// 用戶端 IP 位址
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// 訊息接收時間
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
