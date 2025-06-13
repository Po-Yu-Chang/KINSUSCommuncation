///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: CustomApiRequestEventArgs.cs
// 檔案描述: 自訂 API 請求事件參數
// 功能概述: 用於傳遞自訂 API 請求資訊
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Net;

namespace DDSWebAPI.Events
{
    /// <summary>
    /// 自訂 API 處理事件參數
    /// 當收到無法識別的 API 請求時觸發
    /// 允許外部模組處理自訂的 API 端點
    /// </summary>
    public class CustomApiRequestEventArgs : EventArgs
    {
        /// <summary>
        /// HTTP 請求上下文
        /// </summary>
        public HttpListenerContext Context { get; set; }

        /// <summary>
        /// 請求路徑
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// HTTP 方法
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// 請求主體內容
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        /// 請求標頭
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// 請求時間戳記
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 是否已處理
        /// </summary>
        public bool IsHandled { get; set; }

        /// <summary>
        /// 處理結果
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// 建構函式
        /// </summary>
        public CustomApiRequestEventArgs()
        {
            Headers = new Dictionary<string, string>();
            Timestamp = DateTime.Now;
            IsHandled = false;
        }
    }
}
