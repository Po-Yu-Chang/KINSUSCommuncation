using System;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 用戶端連接事件參數
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string IpAddress { get; set; }
        public DateTime ConnectTime { get; set; }

        public ClientConnectedEventArgs(string clientId, string ipAddress)
        {
            ClientId = clientId;
            IpAddress = ipAddress;
            ConnectTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 用戶端斷線事件參數
    /// </summary>
    public class ClientDisconnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string IpAddress { get; set; }
        public DateTime DisconnectTime { get; set; }

        public ClientDisconnectedEventArgs(string clientId, string ipAddress)
        {
            ClientId = clientId;
            IpAddress = ipAddress;
            DisconnectTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 伺服器狀態變更事件參數
    /// </summary>
    public class ServerStatusChangedEventArgs : EventArgs
    {
        public bool IsRunning { get; set; }
        public string ServerUrl { get; set; }
        public DateTime StatusChangeTime { get; set; }

        public ServerStatusChangedEventArgs(bool isRunning, string serverUrl)
        {
            IsRunning = isRunning;
            ServerUrl = serverUrl;
            StatusChangeTime = DateTime.Now;
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
        public DateTime CallTime { get; set; }

        public ApiCallSuccessEventArgs(string endpoint, string request, string response)
        {
            Endpoint = endpoint;
            Request = request;
            Response = response;
            CallTime = DateTime.Now;
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
        public DateTime CallTime { get; set; }

        public ApiCallFailureEventArgs(string endpoint, string request, string error, Exception exception = null)
        {
            Endpoint = endpoint;
            Request = request;
            Error = error;
            Exception = exception;
            CallTime = DateTime.Now;
        }
    }

    /// <summary>
    /// 日誌訊息事件參數
    /// </summary>
    public class LogMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public string Level { get; set; }
        public DateTime LogTime { get; set; }

        public LogMessageEventArgs(string message, string level = "INFO")
        {
            Message = message;
            Level = level;
            LogTime = DateTime.Now;
        }
    }
}
