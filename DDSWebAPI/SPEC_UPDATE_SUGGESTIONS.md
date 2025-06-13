# KINSUS通訊_整理版.md 文件同步更新建議

## 📋 文件同步需求

根據專案 API 覆蓋度分析，發現專案實作了一些規範文件中未提及的客製化功能。為了保持文件與實作的一致性，建議同步更新 `KINSUS通訊_整理版.md` 文件。

---

## 🆕 需要新增到規格文件的 API

### 客製化倉庫管理 API

專案中實作了以下客製化 API，但規範文件中未記載：

#### 3.1 入料請求 (IN_MATERIAL_REQUEST)

**端點**: `POST /api/in-material`

**請求格式**:
```json
{
  "requestID": "IN_MAT_001",
  "serviceName": "IN_MATERIAL_REQUEST", 
  "timeStamp": "2025-04-25 10:00:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "materialId": "MAT001",
      "quantity": 100,
      "location": {
        "warehouse": "A",
        "track": 10,
        "slide": 3
      },
      "batchNumber": "BATCH20250425001",
      "extendData": null
    }
  ],
  "extendData": null
}
```

**回應格式**:
```json
{
  "responseID": "IN_MAT_001",
  "serviceName": "IN_MATERIAL_RESPONSE",
  "timeStamp": "2025-04-25 10:00:05",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "status": "success",
  "message": "入料操作完成",
  "data": [
    {
      "materialId": "MAT001",
      "assignedLocation": {
        "warehouse": "A",
        "track": 10,
        "slide": 3
      },
      "processResult": "success",
      "processTime": "2025-04-25 10:00:05"
    }
  ],
  "extendData": null
}
```

#### 3.2 出料請求 (OUT_MATERIAL_REQUEST)

**端點**: `POST /api/out-material`

**請求格式**:
```json
{
  "requestID": "OUT_MAT_001",
  "serviceName": "OUT_MATERIAL_REQUEST",
  "timeStamp": "2025-04-25 10:05:00", 
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "materialId": "MAT001",
      "quantity": 50,
      "fromLocation": {
        "warehouse": "A",
        "track": 10,
        "slide": 3
      },
      "extendData": null
    }
  ],
  "extendData": null
}
```

#### 3.3 根據儲存位置查詢 (GET_LOCATION_BY_STORAGE)

**端點**: `POST /api/getlocationbystorage`

**請求格式**:
```json
{
  "requestID": "QUERY_STORAGE_001",
  "serviceName": "GET_LOCATION_BY_STORAGE",
  "timeStamp": "2025-04-25 10:10:00",
  "devCode": "KINSUS001",
  "operator": "OP001", 
  "data": [
    {
      "warehouse": "A",
      "track": 10,
      "slide": 3
    }
  ],
  "extendData": null
}
```

#### 3.4 根據針腳查詢位置 (GET_LOCATION_BY_PIN)

**端點**: `POST /api/getlocationbypin`

#### 3.5 操作夾具 (OPERATION_CLAMP)

**端點**: `POST /api/operationclamp`

**請求格式**:
```json
{
  "requestID": "CLAMP_001",
  "serviceName": "OPERATION_CLAMP",
  "timeStamp": "2025-04-25 10:15:00",
  "devCode": "KINSUS001", 
  "operator": "OP001",
  "data": [
    {
      "operation": "open", // open, close
      "clampId": "CLAMP_01",
      "position": {
        "x": 100.5,
        "y": 200.3,
        "z": 50.0
      },
      "extendData": null
    }
  ],
  "extendData": null
}
```

#### 3.6 改變速度 (CHANGE_SPEED)

**端點**: `POST /api/changespeed`

**請求格式**:
```json
{
  "requestID": "SPEED_001", 
  "serviceName": "CHANGE_SPEED",
  "timeStamp": "2025-04-25 10:20:00",
  "devCode": "KINSUS001",
  "operator": "OP001",
  "data": [
    {
      "speedType": "drilling", // drilling, positioning, rotation
      "speedValue": 1500,
      "unit": "rpm",
      "extendData": null
    }
  ],
  "extendData": null
}
```

---

## 🔧 需要新增的系統管理 API

### 系統監控與管理端點

#### 4.1 系統狀態查詢 (SERVER_STATUS)

**端點**: `GET /api/server/status`

**回應格式**:
```json
{
  "status": "success",
  "data": {
    "serverStatus": "running",
    "uptime": "120.5 hours",
    "totalRequests": 15420,
    "averageResponseTime": "25ms",
    "memoryUsage": "45%",
    "cpuUsage": "12%",
    "diskUsage": "67%",
    "timestamp": "2025-04-25 10:00:00"
  }
}
```

#### 4.2 系統重新啟動 (SERVER_RESTART)

**端點**: `POST /api/server/restart`

#### 4.3 系統統計資訊 (SERVER_STATISTICS)

**端點**: `GET /api/server/statistics`

#### 4.4 健康檢查 (HEALTH_CHECK)

**端點**: `GET /api/health`

**回應格式**:
```json
{
  "status": "healthy",
  "timestamp": "2025-04-25 10:00:00",
  "uptime": "120.5 hours",
  "version": "1.0.0",
  "dependencies": {
    "database": "connected",
    "warehouse_service": "connected", 
    "workflow_service": "connected"
  }
}
```

#### 4.5 出針資料查詢 (OUT_GETPINS)

**端點**: `GET /api/out-getpins`

---

## 📖 建議新增的文件章節

### 建議在 KINSUS通訊_整理版.md 中新增以下章節：

```markdown
## 三、客製化 API 規格

### 3.1 倉庫管理 API
- [3.1.1 入料請求](#311-入料請求in_material_request)
- [3.1.2 出料請求](#312-出料請求out_material_request)  
- [3.1.3 根據儲存位置查詢](#313-根據儲存位置查詢get_location_by_storage)
- [3.1.4 根據針腳查詢位置](#314-根據針腳查詢位置get_location_by_pin)

### 3.2 設備操作 API
- [3.2.1 操作夾具](#321-操作夾具operation_clamp)
- [3.2.2 改變速度](#322-改變速度change_speed)

## 四、系統管理 API 規格

### 4.1 系統監控 API
- [4.1.1 系統狀態查詢](#411-系統狀態查詢server_status)
- [4.1.2 系統重新啟動](#412-系統重新啟動server_restart)
- [4.1.3 系統統計資訊](#413-系統統計資訊server_statistics)
- [4.1.4 健康檢查](#414-健康檢查health_check)
- [4.1.5 出針資料查詢](#415-出針資料查詢out_getpins)
```

---

## 🔄 WebSocket 通訊規格

### 專案中實作了 WebSocket 功能，建議新增章節：

```markdown
## 五、WebSocket 即時通訊規格

### 5.1 連接建立
- **端點**: `ws://localhost:8085/ws`
- **協定**: WebSocket 1.3

### 5.2 訊息格式
所有 WebSocket 訊息均採用 JSON 格式：

```json
{
  "messageId": "MSG_001",
  "messageType": "status_update", // status_update, command, notification
  "timestamp": "2025-04-25 10:00:00",
  "data": {
    // 具體資料內容
  }
}
```

### 5.3 支援的訊息類型
- `status_update`: 狀態更新訊息
- `command`: 指令訊息  
- `notification`: 通知訊息
- `heartbeat`: 心跳檢測

### 5.4 連接管理
- 自動重連機制
- 心跳檢測（每 30 秒）
- 連接狀態事件
```

---

## 📊 建議更新的章節

### 更新第一章「通訊架構與方式」

在 1.4 通訊流程涵蓋範圍中新增：

```markdown
- WebSocket 即時通訊（雙向通訊、狀態同步、事件通知）
- 系統監控與管理（健康檢查、效能統計、重新啟動控制）
- 客製化倉庫操作（精確位置控制、批次處理、庫存最佳化）
- 設備精密控制（夾具操作、速度調整、位置定位）
```

### 更新第五章「效能與限制」

新增實際實作的限制：

```markdown
### 5.4 實際實作限制
- WebSocket 最大連線數：100 個
- 靜態檔案服務：支援多種 MIME 類型
- 多執行緒請求處理：無限制（依系統資源）
- 相依性注入：完整支援，可自訂實作
```

---

## 🎯 同步更新優先順序

### 高優先權（必須更新）
1. **新增客製化 API 章節** - 文件化已實作的功能
2. **新增 WebSocket 通訊規格** - 重要的即時通訊功能
3. **更新通訊流程涵蓋範圍** - 反映實際功能範圍

### 中優先權（建議更新）  
4. **新增系統管理 API 章節** - 系統監控與管理功能
5. **更新效能與限制章節** - 反映實際實作能力

### 低優先權（可選更新）
6. **新增實作架構說明** - 相依性注入、分層架構等
7. **新增開發者指南** - 如何擴充和客製化

---

## 📝 更新建議格式

建議按照以下格式更新文件：

```markdown
## 📌 文件版本資訊
- **文件版本**: v2.0
- **更新日期**: 2025-04-25  
- **更新內容**: 新增客製化 API 與 WebSocket 通訊規格
- **專案版本**: DDSWebAPI v1.0.0
```

完成這些更新後，KINSUS通訊_整理版.md 將完整反映專案的實際功能，成為真正的「實作一致性規範文件」。
