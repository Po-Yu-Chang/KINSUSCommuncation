///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: StorageInfo.cs
// 檔案描述: 儲存位置資訊模型
// 功能概述: 用於表示倉庫中的儲存位置詳細資訊
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using Newtonsoft.Json;

namespace DDSWebAPI.Models.Storage
{
    /// <summary>
    /// 儲存位置資訊模型
    /// 用於表示倉庫中的儲存位置詳細資訊
    /// </summary>
    public class StorageInfo
    {
        /// <summary>
        /// 儲存位置編號
        /// 格式範例: "A01-L02-T03" (區域A01-層級L02-軌道T03)
        /// </summary>
        [JsonProperty("storageNo")]
        public string StorageNo { get; set; }

        /// <summary>
        /// 儲存位置 (相容性屬性)
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>
        /// 儲存識別碼 (相容性屬性)
        /// </summary>
        [JsonProperty("storageId")]
        public string StorageId { get; set; }

        /// <summary>
        /// 物品代碼 (相容性屬性)
        /// </summary>
        [JsonProperty("itemCode")]
        public string ItemCode { get; set; }

        /// <summary>
        /// 數量 (相容性屬性)
        /// </summary>
        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// 最後更新時間 (相容性屬性)
        /// </summary>
        [JsonProperty("lastUpdated")]
        public System.DateTime LastUpdated { get; set; }

        /// <summary>
        /// 儲存區域識別碼
        /// 例如: "A01", "B02", "C03"
        /// </summary>
        [JsonProperty("area")]
        public string Area { get; set; }

        /// <summary>
        /// 儲存層級識別碼
        /// 例如: "L01", "L02", "L03"
        /// </summary>
        [JsonProperty("layer")]
        public string Layer { get; set; }

        /// <summary>
        /// 儲存軌道識別碼
        /// 例如: "T01", "T02", "T03"
        /// </summary>
        [JsonProperty("track")]
        public string Track { get; set; }

        /// <summary>
        /// 該位置的盒子資訊清單
        /// 包含該儲存位置中所有盒子的詳細資訊
        /// </summary>
        [JsonProperty("listBoxInfo")]
        public List<object> ListBoxInfo { get; set; }

        /// <summary>
        /// 儲存位置狀態
        /// 例如: "AVAILABLE", "OCCUPIED", "RESERVED", "MAINTENANCE"
        /// </summary>
        [JsonProperty("status")]
        public string Status { get; set; }

        /// <summary>
        /// 儲存容量 (可選)
        /// </summary>
        [JsonProperty("capacity")]
        public int? Capacity { get; set; }

        /// <summary>
        /// 目前使用量 (可選)
        /// </summary>
        [JsonProperty("currentUsage")]
        public int? CurrentUsage { get; set; }
    }
}
