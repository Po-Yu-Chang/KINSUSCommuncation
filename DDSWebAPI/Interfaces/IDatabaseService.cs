///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: IDatabaseService.cs
// 檔案描述: 資料庫查詢服務介面定義
// 功能概述: 提供基本的資料庫操作功能，支援泛型查詢與指令執行
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DDSWebAPI.Interfaces
{
    /// <summary>
    /// 資料庫查詢服務介面
    /// 提供基本的資料庫操作功能，支援泛型查詢
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// 非同步執行 SQL 查詢並返回指定類型的物件清單
        /// </summary>
        /// <typeparam name="T">返回的物件類型</typeparam>
        /// <param name="sql">SQL 查詢語句</param>
        /// <returns>查詢結果物件清單</returns>
        /// <exception cref="InvalidOperationException">當資料庫連線失敗時拋出</exception>
        /// <exception cref="ArgumentException">當 SQL 語句為空或無效時拋出</exception>
        Task<List<T>> GetAllAsync<T>(string sql);

        /// <summary>
        /// 非同步執行 SQL 查詢並返回單一物件
        /// </summary>
        /// <typeparam name="T">返回的物件類型</typeparam>
        /// <param name="sql">SQL 查詢語句</param>
        /// <returns>查詢結果物件，如果沒有找到則返回 default(T)</returns>
        Task<T> GetSingleAsync<T>(string sql);

        /// <summary>
        /// 非同步執行 SQL 指令 (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="sql">SQL 指令語句</param>
        /// <returns>受影響的資料列數</returns>
        Task<int> ExecuteAsync(string sql);
    }
}
