///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: IWorkflowTaskService.cs
// 檔案描述: 工作流程任務服務介面定義
// 功能概述: 提供設備操作和工作流程控制功能
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading.Tasks;

namespace DDSWebAPI.Interfaces
{
    /// <summary>
    /// 工作流程任務服務介面
    /// 提供設備操作和工作流程控制功能
    /// </summary>
    public interface IWorkflowTaskService
    {
        /// <summary>
        /// 非同步執行機器人夾具操作
        /// </summary>
        /// <param name="requestData">夾具操作請求資料物件</param>
        /// <returns>完成操作的任務</returns>
        /// <exception cref="ArgumentNullException">當 requestData 為 null 時拋出</exception>
        /// <exception cref="InvalidOperationException">當機器人不在可操作狀態時拋出</exception>
        Task OperationRobotClampAsync(object requestData);

        /// <summary>
        /// 非同步執行倉庫入庫作業流程
        /// </summary>
        /// <returns>完成入庫作業的任務</returns>
        /// <exception cref="InvalidOperationException">當倉庫系統忙碌時拋出</exception>
        Task WarehouseInputAsync();

        /// <summary>
        /// 非同步變更設備執行速度
        /// </summary>
        /// <param name="speed">目標速度值 (字串格式，如 "HIGH", "MEDIUM", "LOW" 或數值)</param>
        /// <returns>完成速度變更的任務</returns>
        /// <exception cref="ArgumentException">當速度值格式不正確時拋出</exception>
        Task ChangeSpeedAsync(string speed);

        /// <summary>
        /// 非同步停止所有進行中的工作流程
        /// </summary>
        /// <returns>完成停止操作的任務</returns>
        Task StopAllWorkflowsAsync();

        /// <summary>
        /// 非同步取得目前工作流程狀態
        /// </summary>
        /// <returns>工作流程狀態物件</returns>
        Task<object> GetWorkflowStatusAsync();
    }
}
