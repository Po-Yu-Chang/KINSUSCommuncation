///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: ApiCallFailureEventArgs.cs
// 檔案描述: API 呼叫失敗事件參數
// 功能概述: 用於傳遞 API 呼叫失敗的相關資訊
// 建立日期: 2025-06-16
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;

namespace DDSWebAPI.Events
{
    /// <summary>
    /// API 呼叫失敗事件參數
    /// 當 API 呼叫失敗時觸發
    /// 用於錯誤處理和日誌記錄
    /// </summary>
    public class ApiCallFailureEventArgs : EventArgs
    {
        /// <summary>
        /// API 端點
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// 事件發生時間
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 預設建構函式
        /// </summary>
        public ApiCallFailureEventArgs()
        {
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="endpoint">API 端點</param>
        /// <param name="error">錯誤訊息</param>
        public ApiCallFailureEventArgs(string endpoint, string error)
        {
            Endpoint = endpoint;
            Error = error;
            Timestamp = DateTime.Now;
        }
    }
}
