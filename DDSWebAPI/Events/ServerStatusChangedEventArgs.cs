///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: ServerStatusChangedEventArgs.cs
// 檔案描述: 伺服器狀態變更事件參數
// 功能概述: 用於傳遞伺服器狀態變更資訊
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using DDSWebAPI.Enums;

namespace DDSWebAPI.Events
{
    /// <summary>
    /// 伺服器狀態變更事件參數
    /// 當伺服器狀態發生變化時觸發 (啟動、停止、錯誤等)
    /// 用於狀態監控和管理介面更新
    /// </summary>
    public class ServerStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 伺服器狀態
        /// </summary>
        public ServerStatus Status { get; set; }

        /// <summary>
        /// 狀態變更訊息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 狀態描述 (相容性屬性)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 事件發生時間
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 伺服器是否正在執行
        /// </summary>
        public bool IsRunning => Status == ServerStatus.Running;

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="status">伺服器狀態</param>
        /// <param name="message">狀態變更訊息</param>
        public ServerStatusChangedEventArgs(ServerStatus status, string message)
        {
            Status = status;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
}
