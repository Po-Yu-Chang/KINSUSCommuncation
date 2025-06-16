///////////////////////////////////////////////////////////////////////////////
// 檔案名稱: ApiRequestHandler.cs
// 檔案描述: API 請求處理器
// 功能概述: 處理各種 API 請求的核心邏輯
// 建立日期: 2025-06-13
// 版本: 1.0.0
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DDSWebAPI.Interfaces;
using DDSWebAPI.Models;
using DDSWebAPI.Models.Requests;

namespace DDSWebAPI.Services.Handlers
{
    /// <summary>
    /// API 請求處理器類別
    /// 包含各種 API 端點的處理邏輯
    /// </summary>
    public class ApiRequestHandler
    {
        #region 私有欄位

        private readonly IDatabaseService _databaseService;
        private readonly IWarehouseQueryService _warehouseQueryService;
        private readonly IWorkflowTaskService _workflowTaskService;
        private readonly IGlobalConfigService _globalConfigService;
        private readonly IUtilityService _utilityService;

        #endregion

        #region 建構函式

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="databaseService">資料庫服務</param>
        /// <param name="warehouseQueryService">倉庫查詢服務</param>
        /// <param name="workflowTaskService">工作流程任務服務</param>
        /// <param name="globalConfigService">全域配置服務</param>
        /// <param name="utilityService">公用程式服務</param>
        public ApiRequestHandler(
            IDatabaseService databaseService,
            IWarehouseQueryService warehouseQueryService,
            IWorkflowTaskService workflowTaskService,
            IGlobalConfigService globalConfigService,
            IUtilityService utilityService)
        {
            _databaseService = databaseService;
            _warehouseQueryService = warehouseQueryService;
            _workflowTaskService = workflowTaskService;
            _globalConfigService = globalConfigService;
            _utilityService = utilityService;
        }

        #endregion

        #region 入料 API 處理

        /// <summary>
        /// 處理入料請求
        /// </summary>
        /// <param name="context">HTTP 請求上下文</param>
        /// <param name="requestBody">請求主體</param>
        /// <returns>API 回應任務</returns>
        public async Task<BaseResponse> HandleInMaterialRequestAsync(string requestBody)
        {
            try
            {
                // 解析請求資料
                var requestData = JsonConvert.DeserializeObject<InMaterialRequest>(requestBody);
                
                if (requestData == null)
                {
                    return CreateErrorResponse("無效的請求資料格式");
                }

                // 驗證請求資料
                if (!requestData.IsContinue && (!requestData.InBoxQty.HasValue || requestData.InBoxQty <= 0))
                {
                    return CreateErrorResponse("非連續模式下必須指定有效的入料盒數");
                }

                // 更新全域配置
                if (_globalConfigService != null)
                {
                    _globalConfigService.IsContinueIntoWarehouse = requestData.IsContinue;
                    if (requestData.InBoxQty.HasValue)
                    {
                        _globalConfigService.IntoWarehouseBoxQty = requestData.InBoxQty.Value;
                    }
                    await _globalConfigService.SaveConfigAsync();
                }

                // 執行入料作業
                if (_workflowTaskService != null)
                {
                    await _workflowTaskService.WarehouseInputAsync();
                }

                return CreateSuccessResponse("入料作業已啟動", new
                {
                    isContinue = requestData.IsContinue,
                    inBoxQty = requestData.InBoxQty,
                    priority = requestData.Priority,
                    targetArea = requestData.TargetArea,
                    timestamp = DateTime.Now
                });
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"JSON 解析錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"處理入料請求時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 出料 API 處理

        /// <summary>
        /// 處理出料請求
        /// </summary>
        /// <param name="requestBody">請求主體</param>
        /// <returns>API 回應任務</returns>
        public async Task<BaseResponse> HandleOutMaterialRequestAsync(string requestBody)
        {
            try
            {
                // 解析請求資料
                var requestData = JsonConvert.DeserializeObject<OutMaterialRequest>(requestBody);
                
                if (requestData == null)
                {
                    return CreateErrorResponse("無效的請求資料格式");
                }

                // 驗證請求資料
                if (requestData.Pin == null)
                {
                    return CreateErrorResponse("針具資訊不能為空");
                }

                if (requestData.BoxQty <= 0)
                {
                    return CreateErrorResponse("出料盒數必須大於 0");
                }

                // 執行出料作業
                bool result = false;
                if (_warehouseQueryService != null)
                {
                    result = await _warehouseQueryService.OutWarehouseDialogAsync(requestData.Pin, requestData.BoxQty);
                }

                if (result)
                {
                    return CreateSuccessResponse("出料作業已完成", new
                    {
                        pin = requestData.Pin,
                        boxQty = requestData.BoxQty,
                        priority = requestData.Priority,
                        sourceArea = requestData.SourceArea,
                        timestamp = DateTime.Now
                    });
                }
                else
                {
                    return CreateErrorResponse("出料作業執行失敗");
                }
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"JSON 解析錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"處理出料請求時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 速度控制 API 處理

        /// <summary>
        /// 處理速度變更請求
        /// </summary>
        /// <param name="requestBody">請求主體</param>
        /// <returns>API 回應任務</returns>
        public async Task<BaseResponse> HandleSpeedRequestAsync(string requestBody)
        {
            try
            {
                // 解析請求資料
                var requestData = JsonConvert.DeserializeObject<SpeedRequest>(requestBody);
                
                if (requestData == null)
                {
                    return CreateErrorResponse("無效的請求資料格式");
                }

                // 驗證速度值
                if (string.IsNullOrWhiteSpace(requestData.Speed))
                {
                    return CreateErrorResponse("速度值不能為空");
                }

                // 執行速度變更
                if (_workflowTaskService != null)
                {
                    await _workflowTaskService.ChangeSpeedAsync(requestData.Speed);
                }

                return CreateSuccessResponse("速度變更已完成", new
                {
                    speed = requestData.Speed,
                    effectiveTime = requestData.EffectiveTime,
                    reason = requestData.Reason,
                    timestamp = DateTime.Now
                });
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"JSON 解析錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"處理速度變更請求時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 夾具控制 API 處理

        /// <summary>
        /// 處理夾具操作請求
        /// </summary>
        /// <param name="requestBody">請求主體</param>
        /// <returns>API 回應任務</returns>
        public async Task<BaseResponse> HandleClampRequestAsync(string requestBody)
        {
            try
            {
                // 解析請求資料
                var requestData = JsonConvert.DeserializeObject<ClampRequest>(requestBody);
                
                if (requestData == null)
                {
                    return CreateErrorResponse("無效的請求資料格式");
                }

                // 驗證夾具識別碼
                if (string.IsNullOrWhiteSpace(requestData.Clip))
                {
                    return CreateErrorResponse("夾具識別碼不能為空");
                }

                // 執行夾具操作
                if (_workflowTaskService != null)
                {
                    await _workflowTaskService.OperationRobotClampAsync(requestData);
                }

                return CreateSuccessResponse("夾具操作已完成", new
                {
                    @checked = requestData.Checked,
                    clip = requestData.Clip,
                    force = requestData.Force,
                    timeout = requestData.Timeout,
                    timestamp = DateTime.Now
                });
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"JSON 解析錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"處理夾具操作請求時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 查詢 API 處理

        /// <summary>
        /// 取得針具資料
        /// </summary>
        /// <returns>API 回應任務</returns>
        public async Task<BaseResponse> GetOutPinsDataAsync()
        {
            try
            {
                if (_databaseService == null)
                {
                    return CreateErrorResponse("資料庫服務不可用");
                }

                // 查詢針具資料
                var pins = await _databaseService.GetAllAsync<object>("SELECT * FROM Pins WHERE IsActive = 1");
                
                return CreateSuccessResponse("針具資料查詢成功", pins);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"查詢針具資料時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 根據儲存位置查詢位置資訊
        /// </summary>
        /// <param name="requestBody">請求主體</param>
        /// <returns>API 回應任務</returns>
        public async Task<BaseResponse> GetLocationByStorageAsync(string requestBody)
        {
            try
            {
                // 解析請求資料
                var requestData = JsonConvert.DeserializeObject<dynamic>(requestBody);
                
                if (requestData?.storageNo == null)
                {
                    return CreateErrorResponse("儲存位置編號不能為空");
                }

                string storageNo = requestData.storageNo.ToString();
                
                // 轉換儲存位置編號
                object storageInfo = null;
                if (_utilityService != null)
                {
                    storageInfo = _utilityService.StorageNoConverter(storageNo);
                }

                return CreateSuccessResponse("位置資訊查詢成功", storageInfo);
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"JSON 解析錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"查詢位置資訊時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 根據針具資訊查詢位置
        /// </summary>
        /// <param name="requestBody">請求主體</param>
        /// <returns>API 回應任務</returns>
        public async Task<BaseResponse> GetLocationByPinAsync(string requestBody)
        {
            try
            {
                // 解析請求資料
                var requestData = JsonConvert.DeserializeObject<dynamic>(requestBody);
                
                if (requestData?.pin == null)
                {
                    return CreateErrorResponse("針具資訊不能為空");
                }

                if (_warehouseQueryService == null)
                {
                    return CreateErrorResponse("倉庫查詢服務不可用");
                }

                // 查詢軌道位置資訊
                var trackLocations = await _warehouseQueryService.GetTrackLocationAsync(
                    requestData.pin, 
                    requestData.boxStatus, 
                    requestData.includeEmpty ?? false);

                return CreateSuccessResponse("軌道位置查詢成功", trackLocations);
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"JSON 解析錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"查詢軌道位置時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region MES API 處理

        /// <summary>
        /// 處理 MES API 請求
        /// </summary>
        /// <param name="requestBody">請求主體</param>
        /// <param name="request">HTTP 請求物件</param>
        /// <returns>API 回應任務</returns>
        public async Task<BaseResponse> ProcessMesApiRequestAsync(string requestBody, HttpListenerRequest request)
        {
            try
            {
                // 解析請求資料
                var requestData = JsonConvert.DeserializeObject<BaseRequest>(requestBody);
                
                if (requestData == null)
                {
                    return CreateErrorResponse("無效的請求資料格式");
                }

                // 根據操作類型路由到不同的處理方法
                switch (requestData.OperationType?.ToLower())
                {
                    case "sync_time":
                        return await ProcessSyncTimeRequest(requestData);
                    
                    case "remote_info":
                        return await ProcessRemoteInfoRequest(requestData);
                    
                    case "device_control":
                        return await ProcessDeviceControlRequest(requestData);
                    
                    case "status_query":
                        return await ProcessStatusQueryRequest(requestData);
                    
                    default:
                        return CreateErrorResponse($"未支援的操作類型: {requestData.OperationType}");
                }
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"JSON 解析錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"處理 MES API 請求時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理時間同步請求
        /// </summary>
        /// <param name="requestData">請求資料</param>
        /// <returns>API 回應</returns>
        private async Task<BaseResponse> ProcessSyncTimeRequest(BaseRequest requestData)
        {
            try
            {
                // 處理時間同步邏輯
                var currentTime = DateTime.Now;
                
                return CreateSuccessResponse("時間同步完成", new
                {
                    serverTime = currentTime,
                    timestamp = currentTime.ToString("yyyy-MM-dd HH:mm:ss.fff")
                });
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"時間同步處理錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理遠程資訊請求
        /// </summary>
        /// <param name="requestData">請求資料</param>
        /// <returns>API 回應</returns>
        private async Task<BaseResponse> ProcessRemoteInfoRequest(BaseRequest requestData)
        {
            try
            {
                // 處理遠程資訊邏輯
                var remoteInfo = new
                {
                    deviceId = "DEVICE_001",
                    status = "ONLINE",
                    version = "1.0.0",
                    timestamp = DateTime.Now
                };
                
                return CreateSuccessResponse("遠程資訊查詢成功", remoteInfo);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"遠程資訊處理錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理設備控制請求
        /// </summary>
        /// <param name="requestData">請求資料</param>
        /// <returns>API 回應</returns>
        private async Task<BaseResponse> ProcessDeviceControlRequest(BaseRequest requestData)
        {
            try
            {
                // 處理設備控制邏輯
                if (_workflowTaskService != null)
                {
                    var controlData = requestData.Data?.ToString();
                    if (!string.IsNullOrEmpty(controlData))
                    {
                        await _workflowTaskService.OperationRobotClampAsync(controlData);
                    }
                }
                
                return CreateSuccessResponse("設備控制命令已執行", new
                {
                    controlData = requestData.Data,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"設備控制處理錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理狀態查詢請求
        /// </summary>
        /// <param name="requestData">請求資料</param>
        /// <returns>API 回應</returns>
        private async Task<BaseResponse> ProcessStatusQueryRequest(BaseRequest requestData)
        {
            try
            {
                // 處理狀態查詢邏輯
                object workflowStatus = null;
                if (_workflowTaskService != null)
                {
                    workflowStatus = await _workflowTaskService.GetWorkflowStatusAsync();
                }
                
                return CreateSuccessResponse("狀態查詢成功", workflowStatus);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"狀態查詢處理錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 標準 MES API v1 端點處理方法

        /// <summary>
        /// 處理發送訊息指令 (SEND_MESSAGE_COMMAND)
        /// </summary>
        public async Task<BaseResponse> ProcessSendMessageCommandAsync(string requestBody)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<SendMessageData>>(requestBody);
                
                if (request?.Data == null || request.Data.Count == 0)
                {
                    return CreateErrorResponse("訊息資料不能為空");
                }                foreach (var messageData in request.Data)
                {
                    _utilityService?.LogInfo($"收到訊息: {messageData.Message} (等級: {messageData.Level})");
                    
                    // 可以在此處加入實際的訊息處理邏輯
                    // 例如發送通知、記錄日誌等
                }

                return CreateSuccessResponse("訊息處理完成", new
                {
                    processedCount = request.Data.Count,
                    processTime = DateTime.Now,
                    requestId = request.RequestID
                });
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"訊息資料格式錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                _utilityService?.LogError($"處理訊息時發生錯誤: {ex.Message}");
                return CreateErrorResponse($"處理訊息時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理建立配針工單指令 (CREATE_NEEDLE_WORKORDER_COMMAND)
        /// </summary>
        public async Task<BaseResponse> ProcessCreateNeedleWorkorderCommandAsync(string requestBody)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<CreateNeedleWorkorderData>>(requestBody);
                
                if (request?.Data == null || request.Data.Count == 0)
                {
                    return CreateErrorResponse("工單資料不能為空");
                }

                var responses = new List<CreateWorkorderResponse>();               
                foreach (var workorderData in request.Data)
                {
                    // 驗證工單資料
                    if (string.IsNullOrEmpty(workorderData.WorkOrderNo))
                    {
                        return CreateErrorResponse("工單號碼不能為空");
                    }

                    _utilityService?.LogInfo($"建立配針工單: {workorderData.WorkOrderNo} (產品型號: {workorderData.ProductModel})");

                    // 建立工單回應
                    var workorderResponse = new CreateWorkorderResponse
                    {
                        WorkOrder = workorderData.WorkOrderNo,
                        TaskId = Guid.NewGuid().ToString(), // 自動產生任務ID
                        Status = "CREATED",
                        CreatedTime = DateTime.Now,
                        EstimatedStartTime = DateTime.Now.AddMinutes(5),
                        EstimatedDuration = TimeSpan.FromHours(2),
                        AssignedStations = new List<string> { "ST01", "ST02" },
                        ToolAllocation = new List<ToolAllocation>(),
                        Message = "工單建立成功"
                    };

                    responses.Add(workorderResponse);

                    // 可以在此處加入實際的工單建立邏輯
                    // 例如儲存到資料庫、分配資源等
                }

                return CreateSuccessResponse("工單建立完成", responses);
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"工單資料格式錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                _utilityService?.LogError($"建立工單時發生錯誤: {ex.Message}");
                return CreateErrorResponse($"建立工單時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理日期時間同步指令 (DATE_MESSAGE_COMMAND)
        /// </summary>
        public async Task<BaseResponse> ProcessDateMessageCommandAsync(string requestBody)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<DateTimeData>>(requestBody);
                
                if (request?.Data == null || request.Data.Count == 0)
                {
                    return CreateErrorResponse("時間同步資料不能為空");
                }

                foreach (var dateData in request.Data)
                {
                    _utilityService?.LogInfo($"收到時間同步要求: {dateData.CurrentTime}");
                    
                    // 可以在此處加入實際的時間同步邏輯
                    // 例如更新系統時間或記錄時間差異
                }                return CreateSuccessResponse("時間同步完成", new
                {
                    serverTime = DateTime.Now,
                    processTime = DateTime.Now,
                    requestId = request.RequestID
                });
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"時間同步資料格式錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                _utilityService?.LogError($"處理時間同步時發生錯誤: {ex.Message}");
                return CreateErrorResponse($"處理時間同步時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理切換配方指令 (SWITCH_RECIPE_COMMAND)
        /// </summary>
        public async Task<BaseResponse> ProcessSwitchRecipeCommandAsync(string requestBody)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<SwitchRecipeData>>(requestBody);
                
                if (request?.Data == null || request.Data.Count == 0)
                {
                    return CreateErrorResponse("配方切換資料不能為空");
                }                foreach (var recipeData in request.Data)
                {
                    _utilityService?.LogInfo($"切換配方: {recipeData.RecipeFileName} (版本: {recipeData.RecipeVersion})");
                    
                    // 可以在此處加入實際的配方切換邏輯
                    // 例如更新設備配方、重新配置參數等
                }

                return CreateSuccessResponse("配方切換完成", new
                {
                    processedCount = request.Data.Count,
                    processTime = DateTime.Now,
                    requestId = request.RequestID
                });
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"配方切換資料格式錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                _utilityService?.LogError($"處理配方切換時發生錯誤: {ex.Message}");
                return CreateErrorResponse($"處理配方切換時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理設備控制指令 (DEVICE_CONTROL_COMMAND)
        /// </summary>
        public async Task<BaseResponse> ProcessDeviceControlCommandAsync(string requestBody)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<DeviceControlData>>(requestBody);
                
                if (request?.Data == null || request.Data.Count == 0)
                {
                    return CreateErrorResponse("設備控制資料不能為空");
                }                foreach (var controlData in request.Data)
                {
                    _utilityService?.LogInfo($"設備控制: {controlData.Command} (設備: {controlData.TargetDevice})");
                    
                    // 可以在此處加入實際的設備控制邏輯
                    // 例如啟動/停止設備、調整參數等
                }

                return CreateSuccessResponse("設備控制完成", new
                {
                    processedCount = request.Data.Count,
                    processTime = DateTime.Now,
                    requestId = request.RequestID
                });
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"設備控制資料格式錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                _utilityService?.LogError($"處理設備控制時發生錯誤: {ex.Message}");
                return CreateErrorResponse($"處理設備控制時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 鑽針履歷回報處理

        /// <summary>
        /// 處理鑽針履歷回報指令 (TOOL_TRACE_HISTORY_REPORT_COMMAND)
        /// </summary>
        public async Task<BaseResponse> HandleToolTraceHistoryReportAsync(string requestBody)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<ToolTraceHistoryReportData>>(requestBody);
                
                if (request?.Data == null || request.Data.Count == 0)
                {
                    return CreateErrorResponse("鑽針履歷回報資料不能為空");
                }

                // 處理每一筆履歷回報資料
                foreach (var historyData in request.Data)
                {
                    // 這裡可以加入實際的履歷儲存邏輯
                    // 例如儲存到資料庫或記錄到日誌
                    _utilityService?.LogInfo($"收到鑽針履歷回報: 工具ID={historyData.ToolId}, 軸向={historyData.Axis}, 研磨次數={historyData.GrindCount}");
                    
                    // 可以在此處加入資料驗證邏輯
                    if (string.IsNullOrEmpty(historyData.ToolId))
                    {
                        _utilityService?.LogWarning("履歷回報中包含空的工具ID");
                        continue;
                    }

                    // 儲存到資料庫（如果有資料庫服務）
                    if (_databaseService != null)
                    {
                        try
                        {
                            // 這裡可以實作實際的資料庫儲存邏輯
                            // await _databaseService.SaveToolHistoryAsync(historyData);
                        }
                        catch (Exception dbEx)
                        {
                            _utilityService?.LogError($"儲存工具履歷失敗: {dbEx.Message}");
                        }
                    }
                }                return CreateSuccessResponse("鑽針履歷回報處理完成", new
                {
                    processedCount = request.Data.Count,
                    processTime = DateTime.Now,
                    requestId = request.RequestID
                });
            }
            catch (JsonException ex)
            {
                return CreateErrorResponse($"履歷回報資料格式錯誤: {ex.Message}");
            }
            catch (Exception ex)
            {
                _utilityService?.LogError($"處理鑽針履歷回報時發生錯誤: {ex.Message}");
                return CreateErrorResponse($"處理鑽針履歷回報時發生錯誤: {ex.Message}");
            }
        }

        #endregion

        #region 額外的查詢處理方法        /// <summary>
        /// 處理倉庫資源查詢指令 (WAREHOUSE_RESOURCE_QUERY_COMMAND)
        /// </summary>
        public async Task<BaseResponse> ProcessWarehouseResourceQueryCommandAsync(string requestBody)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<WarehouseResourceQueryData>>(requestBody);
                
                if (request?.Data == null || request.Data.Count == 0)
                {
                    return CreateErrorResponse("倉庫資源查詢資料不能為空");
                }

                _utilityService?.LogInfo($"處理倉庫資源查詢，查詢項目數量: {request.Data.Count}");

                // 這裡可以加入實際的倉庫查詢邏輯
                // 目前回傳模擬資料
                return CreateSuccessResponse("倉庫資源查詢完成", new
                {
                    queryCount = request.Data.Count,
                    processTime = DateTime.Now,
                    requestId = request.RequestID
                });
            }
            catch (Exception ex)
            {
                _utilityService?.LogError($"處理倉庫資源查詢時發生錯誤: {ex.Message}");
                return CreateErrorResponse($"處理倉庫資源查詢時發生錯誤: {ex.Message}");
            }
        }

        /// <summary>
        /// 處理工具履歷查詢指令 (TOOL_TRACE_HISTORY_QUERY_COMMAND)
        /// </summary>
        public async Task<BaseResponse> ProcessToolTraceHistoryQueryCommandAsync(string requestBody)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseRequest<ToolTraceHistoryQueryData>>(requestBody);
                
                if (request?.Data == null || request.Data.Count == 0)
                {
                    return CreateErrorResponse("工具履歷查詢資料不能為空");
                }

                _utilityService?.LogInfo($"處理工具履歷查詢，查詢項目數量: {request.Data.Count}");

                // 這裡可以加入實際的工具履歷查詢邏輯
                // 目前回傳模擬資料
                return CreateSuccessResponse("工具履歷查詢完成", new
                {
                    queryCount = request.Data.Count,
                    processTime = DateTime.Now,
                    requestId = request.RequestID
                });
            }
            catch (Exception ex)
            {
                _utilityService?.LogError($"處理工具履歷查詢時發生錯誤: {ex.Message}");
                return CreateErrorResponse($"處理工具履歷查詢時發生錯誤: {ex.Message}");
            }
        }

        #endregion        #endregion

        #region Helper 方法

        /// <summary>
        /// 建立成功回應
        /// </summary>
        /// <param name="message">成功訊息</param>
        /// <param name="data">回應資料</param>
        /// <param name="requestId">請求 ID</param>
        /// <returns>成功回應</returns>
        private BaseResponse CreateSuccessResponse(string message, object data = null, string requestId = null)
        {
            return new BaseResponse
            {
                RequestId = requestId ?? Guid.NewGuid().ToString(),
                Success = true,
                IsSuccess = true,
                Message = message,
                Data = data,
                Timestamp = DateTime.Now,
                StatusCode = 200
            };
        }

        /// <summary>
        /// 建立錯誤回應
        /// </summary>
        /// <param name="errorMessage">錯誤訊息</param>
        /// <param name="requestId">請求 ID</param>
        /// <param name="statusCode">狀態碼</param>
        /// <returns>錯誤回應</returns>
        private BaseResponse CreateErrorResponse(string errorMessage, string requestId = null, int statusCode = 400)
        {
            return new BaseResponse
            {
                RequestId = requestId ?? Guid.NewGuid().ToString(),
                Success = false,
                IsSuccess = false,
                Message = errorMessage,
                Data = null,
                Timestamp = DateTime.Now,
                StatusCode = statusCode
            };        }

        #endregion
    }
}
