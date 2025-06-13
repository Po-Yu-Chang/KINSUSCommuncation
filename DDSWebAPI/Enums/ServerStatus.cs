///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: ServerStatus.cs
// 檔案描述: 伺服器狀態列舉
// 功能概述: 定義伺服器的各種狀態
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

namespace DDSWebAPI.Enums
{
    /// <summary>
    /// 伺服器狀態列舉
    /// 定義伺服器的各種運行狀態
    /// </summary>
    public enum ServerStatus
    {
        /// <summary>
        /// 已停止
        /// </summary>
        Stopped = 0,

        /// <summary>
        /// 啟動中
        /// </summary>
        Starting = 1,

        /// <summary>
        /// 執行中
        /// </summary>
        Running = 2,

        /// <summary>
        /// 停止中
        /// </summary>
        Stopping = 3,

        /// <summary>
        /// 重新啟動中
        /// </summary>
        Restarting = 4,

        /// <summary>
        /// 錯誤狀態
        /// </summary>
        Error = 5,

        /// <summary>
        /// 警告狀態
        /// </summary>
        Warning = 6,

        /// <summary>
        /// 資訊狀態
        /// </summary>
        Info = 7
    }
}
