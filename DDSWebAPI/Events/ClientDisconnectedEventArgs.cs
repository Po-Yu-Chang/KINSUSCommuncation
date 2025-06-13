///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: ClientDisconnectedEventArgs.cs
// 檔案描述: 用戶端斷線事件參數
// 功能概述: 用於傳遞用戶端斷線資訊
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;

namespace DDSWebAPI.Events
{
    /// <summary>
    /// 用戶端斷線事件參數
    /// 當用戶端連接中斷時觸發
    /// 用於資源清理和連接統計更新
    /// </summary>
    public class ClientDisconnectedEventArgs : EventArgs
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
        /// 斷線時間
        /// </summary>
        public DateTime DisconnectedTime { get; set; }

        /// <summary>
        /// 斷線原因
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 連接持續時間
        /// </summary>
        public TimeSpan ConnectionDuration { get; set; }

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="clientId">用戶端識別碼</param>
        /// <param name="clientIp">用戶端 IP 位址</param>
        /// <param name="reason">斷線原因</param>
        /// <param name="connectionDuration">連接持續時間</param>
        public ClientDisconnectedEventArgs(string clientId, string clientIp, string reason, TimeSpan connectionDuration = default)
        {
            ClientId = clientId;
            ClientIp = clientIp;
            Reason = reason;
            ConnectionDuration = connectionDuration;
            DisconnectedTime = DateTime.Now;
        }
    }
}
