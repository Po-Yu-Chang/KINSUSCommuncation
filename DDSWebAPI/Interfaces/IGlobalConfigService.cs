///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: IGlobalConfigService.cs
// 檔案描述: 全域配置服務介面定義
// 功能概述: 管理系統全域設定和狀態
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System.Threading.Tasks;

namespace DDSWebAPI.Interfaces
{
    /// <summary>
    /// 全域配置服務介面
    /// 管理系統全域設定和狀態
    /// </summary>
    public interface IGlobalConfigService
    {
        /// <summary>
        /// 取得或設定是否為連續入庫模式
        /// true: 連續入庫，false: 指定數量入庫
        /// </summary>
        bool IsContinueIntoWarehouse { get; set; }

        /// <summary>
        /// 取得或設定入庫盒數數量 (當非連續模式時使用)
        /// </summary>
        int IntoWarehouseBoxQty { get; set; }

        /// <summary>
        /// 取得或設定系統運行模式
        /// </summary>
        string SystemMode { get; set; }

        /// <summary>
        /// 取得或設定設備狀態
        /// </summary>
        string DeviceStatus { get; set; }

        /// <summary>
        /// 非同步載入系統配置
        /// </summary>
        /// <returns>載入操作的任務</returns>
        Task LoadConfigAsync();

        /// <summary>
        /// 非同步儲存系統配置
        /// </summary>
        /// <returns>儲存操作的任務</returns>
        Task SaveConfigAsync();
    }
}
