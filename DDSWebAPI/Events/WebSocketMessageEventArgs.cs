///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: WebSocketMessageEventArgs.cs
// 檔案描述: WebSocket 訊息事件參數
// 功能概述: 用於傳遞 WebSocket 訊息資訊
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Net.WebSockets;

namespace DDSWebAPI.Events
{
    /// <summary>
    /// WebSocket 訊息接收事件參數
    /// 當 WebSocket 連接接收到訊息時觸發
    /// 用於即時通訊功能和訊息廣播
    /// </summary>
    public class WebSocketMessageEventArgs : EventArgs
    {
        /// <summary>
        /// 訊息內容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// WebSocket 連接物件
        /// </summary>
        public WebSocket WebSocket { get; set; }

        /// <summary>
        /// 用戶端識別碼
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// 訊息接收時間
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 訊息類型
        /// </summary>
        public WebSocketMessageType MessageType { get; set; }

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="message">訊息內容</param>
        /// <param name="webSocket">WebSocket 連接物件</param>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="messageType">訊息類型</param>
        public WebSocketMessageEventArgs(string message, WebSocket webSocket, string clientId, 
            WebSocketMessageType messageType = WebSocketMessageType.Text)
        {
            Message = message;
            WebSocket = webSocket;
            ClientId = clientId;
            MessageType = messageType;
            Timestamp = DateTime.Now;
        }
    }
}
