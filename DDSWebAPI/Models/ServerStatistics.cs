///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: ServerStatistics.cs
// 檔案描述: 伺服器統計資訊
// 功能概述: 用於記錄和顯示伺服器運行統計資訊
// 建立日期: 2025-06-16
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;

namespace DDSWebAPI.Models
{
    /// <summary>
    /// 伺服器統計資訊
    /// 包含伺服器運行狀態、連線數、請求數等統計資料
    /// </summary>
    public class ServerStatistics
    {
        /// <summary>
        /// 伺服器是否正在執行
        /// </summary>
        public bool IsRunning { get; set; }

        /// <summary>
        /// 伺服器 URL
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// 已連接的用戶端數量
        /// </summary>
        public int ConnectedClients { get; set; }

        /// <summary>
        /// 伺服器運行時間
        /// </summary>
        public TimeSpan Uptime { get; set; }

        /// <summary>
        /// 總請求數
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// 活躍連線數
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// 格式化的運行時間字串
        /// </summary>
        public string FormattedUptime
        {
            get
            {
                if (Uptime.TotalDays >= 1)
                    return $"{(int)Uptime.TotalDays} 天 {Uptime.Hours:D2}:{Uptime.Minutes:D2}:{Uptime.Seconds:D2}";
                else
                    return $"{Uptime.Hours:D2}:{Uptime.Minutes:D2}:{Uptime.Seconds:D2}";
            }
        }
    }
}
