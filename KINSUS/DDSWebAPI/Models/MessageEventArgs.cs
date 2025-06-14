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
    }    /// <summary>
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

        public ClientConnectedEventArgs(string clientId, string clientIp)
        {
            ClientId = clientId;
            ClientIp = clientIp;
        }
    }    /// <summary>
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

        public ClientDisconnectedEventArgs(string clientId, string clientIp)
        {
            ClientId = clientId;
            ClientIp = clientIp;
        }
    }    /// <summary>
    /// 伺服器狀態變更事件參數類別
    /// </summary>
    public class ServerStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 伺服器是否執行中
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// 伺服器 URL
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// 狀態變更時間
        /// </summary>
        public DateTime StatusChangeTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 狀態描述
        /// </summary>
        public string Status { get; set; }

        public ServerStatusChangedEventArgs(bool isRunning, string serverUrl = null)
        {
            IsRunning = isRunning;
            ServerUrl = serverUrl;
            Status = isRunning ? "Running" : "Stopped";
        }

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
        Running,        /// <summary>
        /// 錯誤狀態
        /// </summary>
        Error
    }

    /// <summary>
    /// WebSocket 訊息事件參數
    /// </summary>
    public class WebSocketMessageEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public WebSocketMessageEventArgs(string clientId, string message)
        {
            ClientId = clientId;
            Message = message;
        }
    }    /// <summary>
    /// 自訂 API 請求事件參數
    /// </summary>
    public class CustomApiRequestEventArgs : EventArgs
    {
        public string ServiceName { get; set; }
        public string RequestData { get; set; }
        public string ClientId { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;

        public CustomApiRequestEventArgs(string serviceName, string requestData, string clientId)
        {
            ServiceName = serviceName;
            RequestData = requestData;
            ClientId = clientId;
        }
    }

    /// <summary>
    /// API 呼叫成功事件參數
    /// </summary>
    public class ApiCallSuccessEventArgs : EventArgs
    {
        public string Endpoint { get; set; }
        public string Request { get; set; }
        public string Response { get; set; }
        public DateTime CallTime { get; set; } = DateTime.Now;

        public ApiCallSuccessEventArgs(string endpoint, string request, string response)
        {
            Endpoint = endpoint;
            Request = request;
            Response = response;
        }
    }

    /// <summary>
    /// API 呼叫失敗事件參數
    /// </summary>
    public class ApiCallFailureEventArgs : EventArgs
    {
        public string Endpoint { get; set; }
        public string Request { get; set; }
        public string Error { get; set; }
        public Exception Exception { get; set; }
        public DateTime CallTime { get; set; } = DateTime.Now;

        public ApiCallFailureEventArgs(string endpoint, string request, string error, Exception exception = null)
        {
            Endpoint = endpoint;
            Request = request;
            Error = error;
            Exception = exception;
        }
    }    /// <summary>
    /// 日誌訊息事件參數
    /// </summary>
    public class LogMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public LogLevel Level { get; set; }
        public DateTime LogTime { get; set; } = DateTime.Now;

        public LogMessageEventArgs(string message, LogLevel level = LogLevel.Info)
        {
            Message = message;
            Level = level;
        }
    }

    /// <summary>
    /// 日誌等級列舉
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 除錯
        /// </summary>
        Debug,

        /// <summary>
        /// 資訊
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 錯誤
        /// </summary>
        Error,

        /// <summary>
        /// 嚴重錯誤
        /// </summary>
        Critical
    }
}
