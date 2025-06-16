# 配針機 通訊規格說明 - 目錄選單

## 📚 文件導覽

### 🏗️ 基礎架構
- [一、通訊架構與方式](#一通訊架構與方式)
  - [1.1 基本架構](#11-基本架構)
  - [1.2 資料格式要求](#12-資料格式要求)
  - [1.3 系統角色](#13-系統角色)
  - [1.4 通訊流程涵蓋範圍](#14-通訊流程涵蓋範圍)
  - [1.5 系統整合](#15-系統整合)

### 🔧 API 規格與測試
- [二、API 測試與範例](#二api-測試與範例)

#### 📥 伺服端角色 API（MES/IoT 系統 → 配針機）
- [1. 伺服端角色 API](#1-伺服端角色-apimesiot-系統--配針機)
  - [1.1 遠程資訊下發指令](#11-遠程資訊下發指令send_message_command)
  - [1.2 派針工單建立指令](#12-派針工單建立指令create_needle_workorder_command)
  - [1.3 設備時間同步指令](#13-設備時間同步指令date_message_command)
  - [1.4 刀具工鑽袋檔發送指令](#14-刀具工鑽袋檔發送指令switch_recipe_command)
  - [1.5 設備啟停控制指令](#15-設備啟停控制指令device_control_command)
  - [1.6 倉庫資源查詢指令](#16-倉庫資源查詢指令warehouse_resource_query_command)
  - [1.9 鑽針履歷查詢指令](#19-鑽針履歷查詢指令tool_trace_history_query_command)

#### 📤 用戶端角色 API（KINSUS → MES/IoT 系統）
- [2. 用戶端角色 API](#2-用戶端角色-apikinsus--mesiot-系統)
  - [2.1 配針回報上傳](#21-配針回報上傳tool_output_report_message)
  - [2.2 錯誤回報上傳](#22-錯誤回報上傳error_report_message)
  - [2.8 機臺狀態上報](#28-機臺狀態上報machine_status_report_message)

#### 🏭 客製化倉庫管理 API
- [3. 客製化倉庫管理 API](#3-客製化倉庫管理-api)
  - [3.1 入庫指令](#31-入庫指令in_material_command)
  - [3.2 出庫指令](#32-出庫指令out_material_command)
  - [3.3 依倉儲查詢位置](#33-依倉儲查詢位置get_location_by_storage_command)
  - [3.4 依PIN碼查詢位置](#34-依pin碼查詢位置get_location_by_pin_command)
  - [3.5 夾具操作指令](#35-夾具操作指令operation_clamp_command)
  - [3.6 變更速度指令](#36-變更速度指令change_speed_command)

#### ⚙️ 系統管理 API
- [4. 系統管理 API](#4-系統管理-api)
  - [4.1 伺服器狀態查詢](#41-伺服器狀態查詢server_status_query)
  - [4.2 伺服器重啟指令](#42-伺服器重啟指令server_restart_command)
  - [4.3 連線測試指令](#43-連線測試指令connection_test_command)

### 🛠️ 系統管理與維護
- [三、錯誤處理與狀態碼](#三錯誤處理與狀態碼)
  - [3.1 HTTP 狀態碼](#31-http-狀態碼)
  - [3.2 業務狀態碼](#32-業務狀態碼)
  - [3.3 常見錯誤碼對照表](#33-常見錯誤碼對照表)

- [四、安全性與認證](#四安全性與認證)
  - [4.1 API 金鑰認證](#41-api-金鑰認證)
  - [4.2 請求簽章驗證](#42-請求簽章驗證)
  - [4.3 IP 白名單](#43-ip-白名單)

- [五、效能與限制](#五效能與限制)
  - [5.1 請求限制](#51-請求限制)
  - [5.2 平行處理能力](#52-平行處理concurrency能力)
  - [5.3 資料保留政策](#53-資料保留政策)

### 🔍 故障排除與參考
- [刀具參考表](#三刀具參考表)

---

## 🚀 快速導覽

### 新手入門建議閱讀順序：
1. **基礎架構** → 了解整體通訊架構
2. **API 規格** → 熟悉具體 API 使用方式
3. **錯誤處理** → 掌握異常處理機制
4. **測試除錯** → 進行功能驗證
5. **部署維護** → 正式環境上線

### 開發者常用章節：
- 📡 [伺服端 API](#1-伺服端角色-api) - 接收外部指令
- 📤 [用戶端 API](#2-用戶端角色-api) - 主動上報資訊
- ❌ [錯誤處理](#三錯誤處理與狀態碼) - 異常狀況處理
- 🔧 [故障排除](#九常見問題與故障排除) - 常見問題解決

### 系統管理員常用章節：
- 🔐 [安全認證](#四安全性與認證) - 系統安全設定
- ⚡ [效能限制](#五效能與限制) - 系統容量規劃
- 🚀 [部署維護](#七部署與維護) - 系統部署指南

# 配針機 通訊規格說明

本文件說明刀具管理系統（配針機）與外部系統（如 MES/IoT）之間的通訊方式與 API 範例，並結合需求規格書（Spec.md）內容，提供完整的通訊流程、平行處理（延展性）與測試參考。

---

## 一、通訊架構與方式

### 1.1 基本架構
- 採用 HTTP/RESTful API 進行資料交換
- 支援高延展性（延展性）與平行處理（平行處理）
- 可同時處理多組配刀/拔刀作業
- 資料格式統一為 JSON

### 1.2 資料格式要求
- 所有資料皆需帶有唯一 requestID
- 必須包含時間戳記、設備代碼、操作人員資訊
- 支援 extendData 擴充欄位

### 1.3 系統角色
#### 伺服端角色
- 配針機作為伺服端，接收 MES/IoT 指令
- 負責入庫、配刀、拔刀、回針、狀態回報等流程
- 預設監聽端口：8085

#### 用戶端角色
- 配針機作為用戶端，主動上報資訊至 MES/IoT
- 包含庫存、壽命、配針、派工、履歷等資訊
- 預設上報端點：/api/

### 1.4 通訊流程涵蓋範圍
- 入料/入庫管理（支援一次性大量入庫、續針判斷）
- 配刀/拔刀指示建立與自動排程（支援本地與遠端模式，並可平行處理多組作業）
- 出料批次暫存、回針入庫、針狀態回報（即時顯示每一針狀態，支援人工標示與自動判斷）
- 刀具壽命判斷、續針履歷追溯、低庫存警報、產速控制等

### 1.5 系統整合
- 所有通訊皆可依需求擴充 extendData 欄位
- 可與 MES/ERP/Local 排程系統自動串接
- 確保資訊即時同步

---

## 二、API 測試與範例

### 1. 伺服端角色 API（MES/IoT 系統 → 配針機）

#### 1.1 遠程資訊下發指令（SEND_MESSAGE_COMMAND）

**請求格式：**
```json
{
  "requestID": "MSG_CMD_001",
  "serviceName": "SEND_MESSAGE_COMMAND",
  "timeStamp": "2025-01-15 10:00:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "message": "系統訊息",
      "level": "info",
      "priority": "normal",
      "actionType": 1,
      "intervalSecondTime": 30,
      "extendData": null
    }
  ],
  "extendData": null
}
```

**回應格式：**
```json
{
  "responseID": "MSG_CMD_001",
  "status": "success",
  "message": "訊息已成功接收",
  "extendData": null
}
```

**訊息等級說明：**
- `info`：一般資訊
- `warning`：警告訊息
- `error`：錯誤訊息

**優先順序說明：**
- `normal`：一般優先順序
- `high`：高優先順序
- `urgent`：緊急優先順序

#### 1.2 派針工單建立指令（CREATE_NEEDLE_WORKORDER_COMMAND）

**請求格式：**
```json
{
  "requestID": "WO_CMD_001",
  "serviceName": "CREATE_NEEDLE_WORKORDER_COMMAND",
  "timeStamp": "2025-01-15 08:00:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "workOrderId": "WO20240101001",
      "partNumber": "PN001",
      "quantity": 100,
      "needleType": "DRILL_1.0",
      "extendData": null
    }
  ],
  "extendData": null
}
  "data": [
    {
      "taskID": "TaskID1",
      "workOrder": "Workorder1",
      "tPackage": "TPackage1",
      "stackCount": 1,
      "totalSheets": 300,
      "startTime": "2025-06-12 08:00:00",
      "endTime": "2025-06-12 12:00:00",
      "priority": "normal",
      "isUrgent": false,
      "extendData": null
    },
    {
      "taskID": "TaskID2",
      "workOrder": "Workorder2",
      "tPackage": "Tpackage2",
      "stackCount": 2,
      "totalSheets": 300,
      "startTime": "2025-06-12 08:00:00",
      "endTime": "2025-06-12 12:00:00",
      "priority": "urgent",
      "isUrgent": true,
      "extendData": null
    }
  ],
  "extendData": null
}
```

**回應格式（資源充足量回報）：**
```json
{
  "responseID": "WO_CREATE_001",
  "serviceName": "CREATE_NEEDLE_WORKORDER_RESPONSE",
  "timeStamp": "2025-06-12 08:01:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "status": "success",
  "message": "工單資源檢查完成",
  "data": [
    {
      "workOrder": "Workorder1",
      "tPackage": "TPackage1",
      "priority": "normal",
      "queuePosition": 2,
      "scheduledTime": "2025-06-12 10:30:00",
      "resourceCheck": [
        {
          "tCode": "T01",
          "size": 0.5,
          "reshape": 1,
          "boxQty": 0
        },
        {
          "tCode": "T02",
          "size": 0.8,
          "reshape": 2,
          "boxQty": 1
        }
      ]
    },
    {
      "workOrder": "Workorder2",
      "tPackage": "Tpackage2",
      "priority": "urgent",
      "queuePosition": 1,
      "scheduledTime": "2025-06-12 08:30:00",
      "resourceCheck": [
        {
          "tCode": "T01",
          "size": 0.5,
          "reshape": 1,
          "boxQty": 0
        },
        {
          "tCode": "T03",
          "size": 1.0,
          "reshape": 3,
          "boxQty": 2
        }
      ]
    }
  ],
  "extendData": null
}
```

**任務優先順序說明：**
- `priority = "normal"`：正常任務，依照原定順序執行
- `priority = "urgent"`：緊急任務，插佇列優先執行
- `isUrgent = true`：標記為插佇列任務
- `queuePosition`：佇列中的位置（1 為最優先）
- `scheduledTime`：預計開始執行時間

**資源狀態說明：**
- `boxQty = 0`：資源充足
- `boxQty > 0`：缺少盒數量（需補充）

#### 1.3 設備時間同步指令（DATE_MESSAGE_COMMAND）

**請求格式：**
```json
{
  "requestID": "TIME_CMD_001",
  "serviceName": "DATE_MESSAGE_COMMAND",
  "timeStamp": "2025-01-15 10:05:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "syncTime": "2025-01-15 10:05:00",
      "timeZone": "GMT+8",
      "extendData": null
    }
  ],
  "extendData": null
}
```

**回應格式（時間同步確認回報）：**
```json
{
  "responseID": "DATE_CMD_001",
  "serviceName": "DATE_MESSAGE_RESPONSE",
  "timeStamp": "2025-04-25 10:05:01",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "status": "success",
  "message": "設備時間同步完成",
  "data": [
    {
      "syncedTime": "2025-04-25 10:05:00",
      "originalTime": "2025-04-25 10:04:58",
      "timeDifference": 2,
      "syncResult": "已同步",
      "extendData": null
    }
  ],
  "extendData": null
}
```

**時間同步狀態說明：**
- `status = "success"`：時間同步成功
- `status = "failed"`：時間同步失敗
- `timeDifference`：時間差異（秒）
- `syncResult`：同步結果描述

#### 1.4 刀具工鑽袋檔發送指令（SWITCH_RECIPE_COMMAND）

**請求格式：**
```json
{
  "requestID": "RECIPE_CMD_001",
  "serviceName": "SWITCH_RECIPE_COMMAND",
  "timeStamp": "2025-04-25 10:10:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "AllPlate": 300,
  "Pressplatens": 2,
  "data": [
    {
      "Tcode": "T01",
      "Size": 0.468,
      "HoleLimit": 500,
      "Reshape": 1,
      "RingColor": "#F76C5E"
    },
    {
      "Tcode": "T02",
      "Size": 0.7,
      "HoleLimit": 500,
      "Reshape": 1,
      "RingColor": "#F76C4E"
    }
  ],
  "extendData": null
}
```

**回應格式（工鑽帶檔確認回報）：**
```json
{
  "responseID": "RECIPE_CMD_001",
  "serviceName": "SWITCH_RECIPE_RESPONSE",
  "timeStamp": "2025-04-25 10:10:05",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "status": "success",
  "message": "工鑽帶檔切換完成",
  "data": [
    {
      "AllPlate": 300,
      "Pressplatens": 2,
      "recipeData": [
        {
          "Tcode": "T01",
          "Size": 0.468,
          "HoleLimit": 500,
          "Reshape": 1,
          "RingColor": "#F76C5E",
          "loadStatus": "已載入",
          "stockCheck": "庫存充足"
        },
        {
          "Tcode": "T02",
          "Size": 0.7,
          "HoleLimit": 500,
          "Reshape": 1,
          "RingColor": "#F76C4E",
          "loadStatus": "已載入",
          "stockCheck": "庫存不足"
        }
      ],
      "switchTime": "2025-04-25 10:10:05",
      "extendData": null
    }
  ],
  "extendData": null
}
```

**工鑽帶檔狀態說明：**
- `status = "success"`：工鑽帶檔切換成功
- `status = "failed"`：工鑽帶檔切換失敗
- `loadStatus`：載入狀態（已載入/載入失敗）
- `stockCheck`：庫存檢查結果（庫存充足/庫存不足/庫存異常）

#### 1.5 設備啟停控制指令（DEVICE_CONTROL_COMMAND）

**請求格式：**
```json
{
  "requestID": "CTRL_CMD_001",
  "serviceName": "DEVICE_CONTROL_COMMAND",
  "timeStamp": "2025-01-15 10:15:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "action": "START",
      "deviceId": "DEV001",
      "extendData": null
    }
  ],
  "extendData": null
}
```

**回應格式：**
```json
{
  "responseID": "CTRL_CMD_001",
  "status": "success",
  "message": "設備控制指令執行完成",
  "data": {
    "action": "START",
    "deviceId": "DEV001",
    "currentStatus": "RUNNING",
    "previousStatus": "IDLE"
  }
}
```

**控制動作說明：**
- `START`：啟動設備
- `STOP`：停止設備
- `PAUSE`：暫停設備
- `RESUME`：恢復設備運行

**設備狀態說明：**
- `RUNNING`：執行中
- `IDLE`：閒置中
- `PAUSED`：暫停中
- `STOPPED`：已停止
- `ERROR`：錯誤狀態
- `commandName`：指令名稱（啟動/暫停）
- `previousStatus`：執行前狀態

#### 1.6 倉庫資源查詢指令（WAREHOUSE_RESOURCE_QUERY_COMMAND）

**請求格式：**
```json
{
  "requestID": "WAREHOUSE_QUERY_001",
  "serviceName": "WAREHOUSE_RESOURCE_QUERY_COMMAND",
  "timeStamp": "2025-04-25 10:40:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "queryConditions": [
        {
          "size": 0.2,
          "reshape": 1,
          "type": "normal"
        },
        {
          "size": 0.1,
          "reshape": 1,
          "type": "special"
        },
        {
          "size": 0.3,
          "reshape": 1,
          "type": "special"
        }
      ],
      "extendData": null
    }
  ],
  "extendData": null
}
```

**回應格式（倉庫資源查詢結果）：**
```json
{
  "responseID": "WAREHOUSE_QUERY_001",
  "serviceName": "WAREHOUSE_RESOURCE_QUERY_RESPONSE",
  "timeStamp": "2025-04-25 10:40:05",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "status": "success",
  "message": "倉庫資源查詢完成",
  "data": [
    {
      "queryResults": [
        {
          "size": 0.2,
          "reshape": 1,
          "locations": [
            {
              "qty": 2,
              "warehouse": "A",
              "track": 10,
              "slide": 3
            },
            {
              "qty": 3,
              "warehouse": "A",
              "track": 12,
              "slide": 5
            },
            {
              "qty": 1,
              "warehouse": "B",
              "track": 8,
              "slide": 2
            }
          ],
          "totalQty": 6
        },
        {
          "size": 0.1,
          "reshape": 1,
          "locations": [
            {
              "qty": 4,
              "warehouse": "A",
              "track": 15,
              "slide": 1
            },
            {
              "qty": 2,
              "warehouse": "C",
              "track": 5,
              "slide": 4
            }
          ],
          "totalQty": 6
        },
        {
          "size": 0.3,
          "reshape": 1,
          "locations": [
            {
              "qty": 1,
              "warehouse": "A",
              "track": 20,
              "slide": 3
            },
            {
              "qty": 5,
              "warehouse": "B",
              "track": 18,
              "slide": 6
            },
            {
              "qty": 2,
              "warehouse": "B",
              "track": 22,
              "slide": 1
            }
          ],
          "totalQty": 8
        }
      ],
      "queryTime": "2025-04-25 10:40:05",
      "extendData": null
    }
  ],
  "extendData": null
}
```

**倉庫資源查詢參數說明：**
- `size`：刀具尺寸
- `reshape`：研次
- `type`：類型（normal/special）

**倉庫資源回應欄位說明：**
- `size`：刀具尺寸
- `reshape`：研次
- `locations`：該條件下所有庫存位置陣列
- `qty`：該位置的數量
- `warehouse`：倉庫代碼
- `track`：軌道編號
- `slide`：滑軌位置
- `totalQty`：該條件下的總數量

**查詢狀態說明：**
- `status = "success"`：查詢成功
- `status = "failed"`：查詢失敗
- `status = "partial"`：部分查詢成功（部分條件無庫存）



#### 1.8 鑽針履歷回報指令（TOOL_TRACE_HISTORY_REPORT_COMMAND）

**範例格式：**
```json
{
  "requestID": "TRACE_001",
  "serviceName": "TOOL_TRACE_HISTORY_REPORT_COMMAND",
  "timeStamp": "2025-04-25 10:00:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "toolId": "T123456",
      "axis": "A",
      "machineId": "MC01",
      "product": "P20250425A",
      "grindCount": 6,
      "trayId": "TRAY01",
      "traySlot": 3,
      "history": [
        {
          "useTime": "2025-04-25 09:00:00",
          "machineId": "MC01",
          "axis": "A",
          "product": "P20250425A",
          "grindCount": 5,
          "trayId": "TRAY01",
          "traySlot": 2
        },
        {
          "useTime": "2025-04-24 15:00:00",
          "machineId": "MC02",
          "axis": "B",
          "product": "P20250424B",
          "grindCount": 4,
          "trayId": "TRAY02",
          "traySlot": 1
        }
      ],
      "extendData": null
    }
  ],
  "extendData": null
}
```

#### 1.9 鑽針履歷查詢指令（TOOL_TRACE_HISTORY_QUERY_COMMAND）

**請求格式：**
```json
{
  "requestID": "TRACE_QUERY_001",
  "serviceName": "TOOL_TRACE_HISTORY_QUERY_COMMAND",
  "timeStamp": "2025-04-25 10:00:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "workorder": "WO20250425001"
    },
    {
      "workorder": "WO20250425002"
    },
    {
      "workorder": "WO20250425003"
    }
  ],
  "extendData": null
}
```

**回應格式（鑽針履歷查詢結果）：**
```json
{
  "responseID": "TRACE_QUERY_001",
  "serviceName": "TOOL_TRACE_HISTORY_QUERY_RESPONSE",
  "timeStamp": "2025-04-25 10:00:05",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "status": "success",
  "message": "鑽針履歷查詢完成",
  "data": [
    {
      "PlateID": "PLATE001",
      "BoxPositionID": "POS_A01",
      "PlateQrcode": "QR_PLATE_20250425_001",
      "BoxQrcode": "QR_BOX_20250425_001",
      "WorkOrder": "WO20250425001",
      "Recipe": "RECIPE_T01_0468",
      "UpdateTime": "2025-04-25 10:00:00",
      "State": "完成",
      "extendData": null
    },
    {
      "PlateID": "PLATE002",
      "BoxPositionID": "POS_A02",
      "PlateQrcode": "QR_PLATE_20250425_002",
      "BoxQrcode": "QR_BOX_20250425_002",
      "WorkOrder": "WO20250425002",
      "Recipe": "RECIPE_T02_0700",
      "UpdateTime": "2025-04-25 10:01:00",
      "State": "執行中",
      "extendData": null
    },
    {
      "PlateID": "PLATE003",
      "BoxPositionID": "POS_A03",
      "PlateQrcode": "QR_PLATE_20250425_003",
      "BoxQrcode": "QR_BOX_20250425_003",
      "WorkOrder": "WO20250425003",
      "Recipe": "RECIPE_T03_1000",
      "UpdateTime": "2025-04-25 10:02:00",
      "State": "待處理",
      "extendData": null
    }
  ],
  "extendData": null
}
```

**履歷查詢欄位說明：**
- **請求參數：**
  - `workorder`：工單號碼（可多筆查詢）
- **回應欄位：**
  - `PlateID`：板子識別碼
  - `BoxPositionID`：盒子位置識別碼
  - `PlateQrcode`：板子 QR 條碼
  - `BoxQrcode`：盒子 QR 條碼
  - `WorkOrder`：工單號碼
  - `Recipe`：配方編號
  - `UpdateTime`：更新時間
  - `State`：狀態（完成/執行中/待處理/異常）

---

### 2. 用戶端角色 API（DDS→ MES/IoT 系統）



#### 2.1 配針回報上傳（TOOL_OUTPUT_REPORT_MESSAGE）

**請求格式（KINSUS → MES/IoT 系統）：**
```json
{
  "requestID": "TOOL_RPT_001",
  "serviceName": "TOOL_OUTPUT_REPORT_MESSAGE",
  "timeStamp": "2025-01-15 10:00:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "reportType": "TOOL_OUTPUT",
      "toolInfo": {
        "toolId": "TOOL001",
        "toolType": "DRILL",
        "location": "A01",
        "status": "COMPLETED"
      },
      "extendData": null
    }
  ],
  "extendData": null
}
```

**回應格式（MES/IoT 系統確認回報）：**
```json
{
  "responseID": "OUTPUT_001",
  "serviceName": "TOOL_OUTPUT_REPORT_RESPONSE",
  "timeStamp": "2025-04-25 10:00:05",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "status": "success",
  "message": "出料回報接收完成",
  "data": [
    {
      "processedRecords": [
        {
          "workorder": "WO20250425001",
          "boxorder": "BOX001",
          "processStatus": "已登記"
        },
        {
          "workorder": "WO20250425002",
          "boxorder": "BOX002",
          "processStatus": "已登記"
        }
      ],
      "totalProcessed": 2,
      "receivedTime": "2025-04-25 10:00:05"
    }
  ],
  "extendData": null
}
```

**出料欄位說明：**
- `workorder`：工單編號
- `recipe`：配方/工鑽帶檔編號
- `boxorder`：盒子訂單編號
- `boxqrcode`：盒子 QR 條碼
- `plateqrcode`：板子 QR 條碼
- `qty`：數量（固定為 50）
- `done`：是否完成（true/false）
- `ringid`：環形識別碼陣列（固定 50 筆資料）
  - `id`：環形識別碼
  - `position`：位置編號（1~50）

#### 2.2 錯誤回報上傳（ERROR_REPORT_MESSAGE）

#### 2.2 錯誤回報上傳（ERROR_REPORT_MESSAGE）

**請求格式（KINSUS → MES/IoT 系統）：**
```json
{
  "requestID": "ERR_RPT_001",
  "serviceName": "ERROR_REPORT_MESSAGE",
  "timeStamp": "2025-01-15 10:40:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "errorCode": "E001",
      "errorMessage": "系統錯誤",
      "severity": "HIGH",
      "errorType": "SYSTEM",
      "extendData": null
    }
  ],
  "extendData": null
}
```

**回應格式：**
```json
{
  "responseID": "ERR_RPT_001",
  "status": "success",
  "message": "錯誤回報已接收",
  "extendData": null
}
```

**錯誤嚴重度說明：**
- `LOW`：低嚴重度
- `MEDIUM`：中等嚴重度
- `HIGH`：高嚴重度
- `CRITICAL`：嚴重錯誤

**錯誤類型說明：**
- `SYSTEM`：系統錯誤
- `HARDWARE`：硬體錯誤
- `SOFTWARE`：軟體錯誤
- `NETWORK`：網路錯誤

#### 2.8 機臺狀態上報（MACHINE_STATUS_REPORT_MESSAGE）

**請求格式（KINSUS → MES/IoT 系統）：**
```json
{
  "requestID": "MACH_RPT_001",
  "serviceName": "MACHINE_STATUS_REPORT_MESSAGE",
  "timeStamp": "2025-01-15 10:35:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "machineId": "MACH001",
      "status": "RUNNING",
      "temperature": 25.5,
      "pressure": 1.2,
      "extendData": null
    }
  ],
  "extendData": null
}
```

**回應格式：**
```json
{
  "responseID": "MACH_RPT_001",
  "status": "success",
  "message": "機臺狀態回報已接收",
  "extendData": null
}
```

**機臺狀態說明：**
- `RUNNING`：執行中
- `IDLE`：閒置中
- `ERROR`：錯誤狀態
- `MAINTENANCE`：維護中

---

### 3. 客製化倉庫管理 API

#### 3.1 入庫指令（IN_MATERIAL_COMMAND）

**請求格式：**
```json
{
  "materialId": "MAT001",
  "materialType": "DRILL",
  "quantity": 50,
  "warehouseLocation": "A-01-01",
  "operator": "OP001",
  "timeStamp": "2025-01-15 10:00:00"
}
```

**回應格式：**
```json
{
  "responseID": "IN_MAT_001",
  "status": "success",
  "message": "入庫作業完成",
  "data": {
    "materialId": "MAT001",
    "actualQuantity": 50,
    "warehouseLocation": "A-01-01",
    "inventoryBalance": 150
  }
}
```

#### 3.2 出庫指令（OUT_MATERIAL_COMMAND）

**請求格式：**
```json
{
  "materialId": "MAT001",
  "quantity": 10,
  "destinationLocation": "PRODUCTION_LINE_A",
  "operator": "OP001",
  "timeStamp": "2025-01-15 11:00:00"
}
```

**回應格式：**
```json
{
  "responseID": "OUT_MAT_001",
  "status": "success",
  "message": "出庫作業完成",
  "data": {
    "materialId": "MAT001",
    "actualQuantity": 10,
    "remainingQuantity": 140,
    "destinationLocation": "PRODUCTION_LINE_A"
  }
}
```

#### 3.3 依倉儲查詢位置（GET_LOCATION_BY_STORAGE_COMMAND）

**請求格式：**
```json
{
  "storageId": "STORAGE001",
  "queryType": "AVAILABLE_LOCATIONS"
}
```

**回應格式：**
```json
{
  "responseID": "LOC_QUERY_001",
  "status": "success",
  "message": "位置查詢完成",
  "data": {
    "storageId": "STORAGE001",
    "availableLocations": [
      {
        "locationId": "A-01-01",
        "capacity": 100,
        "currentStock": 50,
        "status": "AVAILABLE"
      },
      {
        "locationId": "A-01-02",
        "capacity": 100,
        "currentStock": 0,
        "status": "EMPTY"
      }
    ]
  }
}
```

#### 3.4 依PIN碼查詢位置（GET_LOCATION_BY_PIN_COMMAND）

**請求格式：**
```json
{
  "pinCode": "PIN001",
  "queryType": "LOCATION_INFO"
}
```

**回應格式：**
```json
{
  "responseID": "PIN_QUERY_001",
  "status": "success",
  "message": "PIN碼查詢完成",
  "data": {
    "pinCode": "PIN001",
    "locationInfo": {
      "locationId": "A-01-01",
      "storageId": "STORAGE001",
      "materialInfo": {
        "materialId": "MAT001",
        "materialType": "DRILL",
        "quantity": 50
      }
    }
  }
}
```

#### 3.5 夾具操作指令（OPERATION_CLAMP_COMMAND）

**請求格式：**
```json
{
  "clampId": "CLAMP001",
  "operation": "OPEN",
  "force": 100.0,
  "operator": "OP001"
}
```

**回應格式：**
```json
{
  "responseID": "CLAMP_OP_001",
  "status": "success",
  "message": "夾具操作完成",
  "data": {
    "clampId": "CLAMP001",
    "operation": "OPEN",
    "currentStatus": "OPENED",
    "force": 100.0
  }
}
```

**操作類型說明：**
- `OPEN`：開啟夾具
- `CLOSE`：關閉夾具

#### 3.6 變更速度指令（CHANGE_SPEED_COMMAND）

**請求格式：**
```json
{
  "deviceId": "DEV001",
  "newSpeed": 1200,
  "speedUnit": "RPM",
  "operator": "OP001"
}
```

**回應格式：**
```json
{
  "responseID": "SPEED_CHG_001",
  "status": "success",
  "message": "速度變更完成",
  "data": {
    "deviceId": "DEV001",
    "previousSpeed": 1000,
    "newSpeed": 1200,
    "speedUnit": "RPM"
  }
}
```

---

### 4. 系統管理 API

#### 4.1 伺服器狀態查詢（SERVER_STATUS_QUERY）

**請求格式：**
```json
{
  "queryType": "FULL_STATUS",
  "includeStatistics": true
}
```

**回應格式：**
```json
{
  "responseID": "SRV_STATUS_001",
  "status": "success",
  "message": "伺服器狀態查詢完成",
  "data": {
    "serverStatus": "RUNNING",
    "uptime": "72:15:30",
    "activeConnections": 5,
    "totalMemoryUsage": "512MB",
    "cpuUsage": "25%",
    "statistics": {
      "totalRequests": 1250,
      "successfulRequests": 1200,
      "failedRequests": 50,
      "averageResponseTime": "150ms"
    }
  }
}
```

#### 4.2 伺服器重啟指令（SERVER_RESTART_COMMAND）

**請求格式：**
```json
{
  "restartType": "GRACEFUL",
  "reason": "Manual restart request",
  "operator": "ADMIN"
}
```

**回應格式：**
```json
{
  "responseID": "SRV_RESTART_001",
  "status": "success",
  "message": "伺服器重啟指令已接受",
  "data": {
    "restartType": "GRACEFUL",
    "estimatedDowntime": "30 seconds",
    "scheduledTime": "2025-01-15 12:00:00"
  }
}
```

**重啟類型說明：**
- `GRACEFUL`：優雅重啟（等待現有連線完成）
- `FORCE`：強制重啟（立即中斷所有連線）

#### 4.3 連線測試指令（CONNECTION_TEST_COMMAND）

**請求格式：**
```json
{
  "testType": "PING",
  "targetEndpoint": "localhost",
  "timeStamp": "2025-01-15 12:00:00"
}
```

**回應格式：**
```json
{
  "responseID": "CONN_TEST_001",
  "status": "success",
  "message": "連線測試完成",
  "data": {
    "testType": "PING",
    "targetEndpoint": "localhost",
    "responseTime": "5ms",
    "connectionStatus": "CONNECTED",
    "testResult": "PASS"
  }
}
```

**測試結果說明：**
- `PASS`：連線測試通過
- `FAIL`：連線測試失敗
- `TIMEOUT`：連線測試逾時

---

## 三、錯誤處理與狀態碼

### 3.1 HTTP 狀態碼
- `200 OK`：請求成功
- `400 Bad Request`：請求格式錯誤
- `401 Unauthorized`：未授權
- `404 Not Found`：資源不存在
- `500 Internal Server Error`：伺服器內部錯誤
- `503 Service Unavailable`：服務暫時無法使用

### 3.2 業務狀態碼
```json
{
  "status": "success|failed|partial",
  "errorCode": "ERR_001",
  "errorMessage": "詳細錯誤訊息",
  "timestamp": "2025-04-25 10:00:00"
}
```

### 3.3 常見錯誤碼對照表
| 錯誤碼 | 錯誤訊息 | 說明 |
|--------|----------|------|
| ERR_001 | 刀具庫存不足 | 指定規格刀具庫存數量不足 |
| ERR_002 | 工單不存在 | 查詢的工單號碼不存在 |
| ERR_003 | 設備異常 | 配針機硬體異常 |
| ERR_004 | 配方載入失敗 | 工鑽帶檔載入過程發生錯誤 |
| ERR_005 | 通訊逾時 | 請求處理超過預設時間 |

---

## 三、刀具參考表

| Tcode | 孔數 | Size  | 孔限 | 研次 | 環顏色  | 刀型 |
|-------|------|-------|------|------|---------|------|
| T01   | 90   | 0.468 | 500  | 1    | #F76C5E | A    |
| T02   | 1472 | 0.45  | 1200 | 1    | #57C4E5 | A    |
| T03   | 2028 | 0.451 | 1200 | 1    | #A16AE8 | A    |
| T04   | 10   | 0.7   | 1000 | 1    | #4CD964 | A    |
| T05S  | 36   | 0.904 | 1000 | 1    | #FF9F1C | A    |
| T06   | 265  | 1     | 1000 | 1    | #374785 | A    |
| T07   | 192  | 1.075 | 1000 | 1    | #F1C40F | A    |
| T08   | 96   | 1.35  | 1000 | 1    | #F67280 | A    |
| T09   | 4    | 1.6   | 1000 | 1    | #3EC1D3 | A    |
| T10   | 192  | 1.675 | 1000 | 1    | #B388EB | A    |
| T11S  | 12   | 1.754 | 1000 | 1    | #2ECC71 | A    |
| T12   | 96   | 1.775 | 1000 | 1    | #E74C3C | A    |
| T13   | 6    | 2     | 800  | 1    | #3498DB | A    |
| T14   | 24   | 2.4   | 800  | 1    | #FFC75F | A    |
| T15   | 12   | 2.7   | 800  | 1    | #42E6A4 | A    |
| T16S  | 12   | 2.703 | 800  | 1    | #D65DB1 | A    |
| T17   | 12   | 2.8   | 800  | 1    | #ACD8AA | A    |
| T18   | 12   | 3     | 500  | 1    | #FC5185 | A    |
| T19   | 60   | 3.075 | 500  | 1    | #70C1B3 | A    |
| T20   | 9    | 3.2   | 500  | 1    | #B83B5E | A    |
| T21   | 5    | 3.201 | 500  | 1    | #F08A5D | A    |
| T22   | 24   | 4.1   | 500  | 1    | #845EC2 | A    |
| T23S  | 52   | 2     | 500  | 1    | #0081CF | A    |

---

## 四、安全性與認證

### 4.1 API 金鑰認證
```http
POST /api/endpoint
Authorization: Bearer YOUR_API_KEY
Content-Type: application/json
```

### 4.2 請求簽章驗證
- 使用 HMAC-SHA256 演算法
- 簽章內容：requestID + timeStamp + body
- 標頭欄位：`X-Signature`

### 4.3 IP 白名單
- 限制特定 IP 位址存取
- 支援 CIDR 格式設定

## 五、效能與限制

### 5.1 請求限制
- 每分鐘最大請求數：100 次
- 單次請求最大資料量：10MB
- 請求逾時時間：30 秒

### 5.2 平行處理（Concurrency）能力
- 最大平行處理數：10 個連線
- 佇列（Queue）容量：100 個任務
- 支援延展性（Scalability）擴充

### 5.3 資料保留政策
- 交易（Transaction）記錄保留：90 天
- 錯誤日誌保留：30 天
- 效能監控資料：7 天
