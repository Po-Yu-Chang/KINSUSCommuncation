///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: ClampRequest.cs
// 檔案描述: 夾具操作請求資料模型
// 功能概述: 用於封裝機器人夾具操作 API 的請求參數
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;

namespace DDSWebAPI.Models.Requests
{
    /// <summary>
    /// 夾具操作請求資料模型
    /// 用於封裝機器人夾具操作 API 的請求參數
    /// </summary>
    public class ClampRequest
    {
        /// <summary>
        /// 夾具操作狀態
        /// true: 夾取，false: 釋放
        /// </summary>
        [JsonProperty("checked")]
        public bool Checked { get; set; }

        /// <summary>
        /// 夾具識別碼或名稱
        /// 例如: "CLAMP_001", "LEFT_CLAMP", "RIGHT_CLAMP"
        /// </summary>
        [JsonProperty("clip")]
        public string Clip { get; set; }

        /// <summary>
        /// 夾取力度 (可選，範圍 1-100)
        /// </summary>
        [JsonProperty("force")]
        public int? Force { get; set; }

        /// <summary>
        /// 操作超時時間，單位秒 (可選，預設 30 秒)
        /// </summary>
        [JsonProperty("timeout")]
        public int? Timeout { get; set; }
    }
}
