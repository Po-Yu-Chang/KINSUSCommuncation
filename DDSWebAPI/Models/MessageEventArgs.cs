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

    /// <summary>
    /// 用戶端連接事件參數類別
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 用戶端唯一識別碼
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// 用戶端 IP 位址
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// 連接時間
        /// </summary>
        public DateTime ConnectTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 用戶端斷線事件參數類別
    /// </summary>
    public class ClientDisconnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 用戶端唯一識別碼
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// 用戶端 IP 位址
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// 斷線時間
        /// </summary>
        public DateTime DisconnectTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 斷線原因
        /// </summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// 伺服器狀態變更事件參數類別
    /// </summary>
    public class ServerStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 伺服器狀態
        /// </summary>
        public ServerStatus Status { get; set; }

        /// <summary>
        /// 狀態變更時間
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 狀態描述
        /// </summary>
        public string Description { get; set; }
    }

    /// <summary>
    /// 伺服器狀態列舉
    /// </summary>
    public enum ServerStatus
    {
        /// <summary>
        /// 已停止
        /// </summary>
        Stopped,

        /// <summary>
        /// 正在啟動
        /// </summary>
        Starting,

        /// <summary>
        /// 執行中
        /// </summary>
        Running,

        /// <summary>
        /// 錯誤狀態
        /// </summary>
        Error
    }
}
