///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: LogMessageEventArgs.cs
// 檔案描述: 日誌訊息事件參數
// 功能概述: 用於傳遞日誌訊息
// 建立日期: 2025-06-16
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;

namespace DDSWebAPI.Events
{
    /// <summary>
    /// 日誌等級
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 調試訊息
        /// </summary>
        Debug,

        /// <summary>
        /// 資訊訊息
        /// </summary>
        Info,

        /// <summary>
        /// 警告訊息
        /// </summary>
        Warning,

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        Error
    }

    /// <summary>
    /// 日誌訊息事件參數
    /// 用於傳遞日誌訊息
    /// </summary>
    public class LogMessageEventArgs : EventArgs
    {
        /// <summary>
        /// 日誌等級
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// 日誌訊息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 事件發生時間
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="level">日誌等級</param>
        /// <param name="message">日誌訊息</param>
        public LogMessageEventArgs(LogLevel level, string message)
        {
            Level = level;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }
}
