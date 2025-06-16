///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: ApiCallSuccessEventArgs.cs
// 檔案描述: API 呼叫成功事件參數
// 功能概述: 用於傳遞 API 呼叫成功的相關資訊
// 建立日期: 2025-06-16
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;

namespace DDSWebAPI.Events
{
    /// <summary>
    /// API 呼叫成功事件參數
    /// 當 API 呼叫成功時觸發
    /// 用於狀態更新和日誌記錄
    /// </summary>
    public class ApiCallSuccessEventArgs : EventArgs
    {
        /// <summary>
        /// API 端點
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// 回應內容
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// 事件發生時間
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 預設建構函式
        /// </summary>
        public ApiCallSuccessEventArgs()
        {
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="endpoint">API 端點</param>
        /// <param name="response">回應內容</param>
        public ApiCallSuccessEventArgs(string endpoint, string response)
        {
            Endpoint = endpoint;
            Response = response;
            Timestamp = DateTime.Now;
        }
    }
}
