///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: IUtilityService.cs
// 檔案描述: 公用程式服務介面定義
// 功能概述: 提供各種輔助功能和工具方法
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;

namespace DDSWebAPI.Interfaces
{
    /// <summary>
    /// 公用程式服務介面
    /// 提供各種輔助功能和工具方法
    /// </summary>
    public interface IUtilityService
    {
        /// <summary>
        /// 將儲存位置編號轉換為儲存位置物件
        /// </summary>
        /// <param name="storageNo">儲存位置編號字串</param>
        /// <returns>儲存位置物件，包含區域、層級、軌道等資訊</returns>
        /// <exception cref="ArgumentException">當 storageNo 格式不正確時拋出</exception>
        object StorageNoConverter(string storageNo);

        /// <summary>
        /// 產生唯一識別碼
        /// </summary>
        /// <returns>唯一識別碼字串</returns>
        string GenerateUniqueId();

        /// <summary>
        /// 記錄資訊日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        void LogInfo(string message);

        /// <summary>
        /// 記錄警告日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        void LogWarning(string message);

        /// <summary>
        /// 記錄錯誤日誌
        /// </summary>
        /// <param name="message">日誌訊息</param>
        void LogError(string message);

        /// <summary>
        /// 驗證資料格式
        /// </summary>
        /// <param name="data">要驗證的資料物件</param>
        /// <param name="validationType">驗證類型</param>
        /// <returns>驗證結果，true 表示通過驗證</returns>
        bool ValidateData(object data, string validationType);

        /// <summary>
        /// 格式化時間戳記
        /// </summary>
        /// <param name="timestamp">時間戳記</param>
        /// <param name="format">格式字串，預設為 "yyyy-MM-dd HH:mm:ss"</param>
        /// <returns>格式化後的時間字串</returns>
        string FormatTimestamp(DateTime timestamp, string format = "yyyy-MM-dd HH:mm:ss");
    }
}
