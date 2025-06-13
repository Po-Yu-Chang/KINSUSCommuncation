///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: IWarehouseQueryService.cs
// 檔案描述: 倉庫查詢服務介面定義
// 功能概述: 提供倉庫管理相關的業務邏輯功能
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DDSWebAPI.Interfaces
{
    /// <summary>
    /// 倉庫查詢服務介面
    /// 提供倉庫管理相關的業務邏輯功能
    /// </summary>
    public interface IWarehouseQueryService
    {
        /// <summary>
        /// 非同步執行出庫對話操作
        /// </summary>
        /// <param name="pin">針具物件資訊</param>
        /// <param name="boxQty">出庫盒數數量</param>
        /// <returns>操作是否成功，true 表示成功，false 表示失敗</returns>
        /// <exception cref="ArgumentNullException">當 pin 參數為 null 時拋出</exception>
        /// <exception cref="ArgumentException">當 boxQty 小於或等於 0 時拋出</exception>
        Task<bool> OutWarehouseDialogAsync(object pin, int boxQty);

        /// <summary>
        /// 非同步取得軌道位置資訊
        /// </summary>
        /// <param name="pin">針具物件資訊</param>
        /// <param name="boxStatus">盒子狀態篩選條件</param>
        /// <param name="includeEmpty">是否包含空的位置</param>
        /// <returns>軌道位置資訊清單</returns>
        Task<List<object>> GetTrackLocationAsync(object pin, object boxStatus, bool includeEmpty);

        /// <summary>
        /// 非同步取得倉庫完整資訊
        /// </summary>
        /// <returns>倉庫資訊物件清單</returns>
        Task<List<object>> GetWarehouseInfoAsync();

        /// <summary>
        /// 非同步取得指定區域的庫存統計
        /// </summary>
        /// <param name="area">倉庫區域識別碼</param>
        /// <returns>庫存統計資訊</returns>
        Task<object> GetInventoryStatisticsAsync(string area);
    }
}
