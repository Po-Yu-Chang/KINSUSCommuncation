///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: ClientConnectedEventArgs.cs
// 檔案描述: 用戶端連接事件參數
// 功能概述: 用於傳遞用戶端連接資訊
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;

namespace DDSWebAPI.Events
{
    /// <summary>
    /// 用戶端連接事件參數
    /// 當有新的用戶端建立連接時觸發
    /// 用於連接管理和統計
    /// </summary>
    public class ClientConnectedEventArgs : EventArgs
    {
        /// <summary>
        /// 用戶端識別碼
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// 用戶端 IP 位址
        /// </summary>
        public string ClientIp { get; set; }

        /// <summary>
        /// 連接時間
        /// </summary>
        public DateTime ConnectedTime { get; set; }

        /// <summary>
        /// 連接類型 (HTTP, WebSocket)
        /// </summary>
        public string ConnectionType { get; set; }

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="clientIp">用戶端 IP 位址</param>
        /// <param name="connectionType">連接類型</param>
        public ClientConnectedEventArgs(string clientId, string clientIp, string connectionType = "HTTP")
        {
            ClientId = clientId;
            ClientIp = clientIp;
            ConnectionType = connectionType;
            ConnectedTime = DateTime.Now;
        }
    }
}
