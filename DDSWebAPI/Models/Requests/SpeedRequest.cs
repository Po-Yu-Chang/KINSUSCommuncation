///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: SpeedRequest.cs
// 檔案描述: 速度變更請求資料模型
// 功能概述: 用於封裝速度變更 API 的請求參數
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;

namespace DDSWebAPI.Models.Requests
{
    /// <summary>
    /// 速度變更請求資料模型
    /// 用於封裝速度變更 API 的請求參數
    /// </summary>
    public class SpeedRequest
    {
        /// <summary>
        /// 目標速度值
        /// 支援格式: "HIGH", "MEDIUM", "LOW" 或數值字串 "1-100"
        /// </summary>
        [JsonProperty("speed")]
        public string Speed { get; set; }

        /// <summary>
        /// 速度變更生效時間 (可選)
        /// ISO 8601 格式: "2025-06-13T10:30:00Z"
        /// </summary>
        [JsonProperty("effectiveTime")]
        public string EffectiveTime { get; set; }

        /// <summary>
        /// 速度變更原因說明 (可選)
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}
