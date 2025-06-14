@echo off
REM DDSWebAPI 建構腳本
REM 此腳本會建構 DDSWebAPI 函式庫和主 WPF 應用程式

echo ========================================
echo DDSWebAPI 建構腳本
echo ========================================
echo.

REM 設定變數
set SOLUTION_FILE=KINSUS.sln
set LIBRARY_PROJECT=DDSWebAPI\DDSWebAPI.csproj
set MAIN_PROJECT=KINSUS.csproj
set BUILD_CONFIG=Release

echo 正在檢查檔案是否存在...

REM 檢查解決方案檔案
if not exist "%SOLUTION_FILE%" (
    echo 錯誤: 找不到解決方案檔案 %SOLUTION_FILE%
    pause
    exit /b 1
)

REM 檢查 DDSWebAPI 專案檔案
if not exist "%LIBRARY_PROJECT%" (
    echo 錯誤: 找不到 DDSWebAPI 專案檔案 %LIBRARY_PROJECT%
    pause
    exit /b 1
)

REM 檢查主專案檔案
if not exist "%MAIN_PROJECT%" (
    echo 錯誤: 找不到主專案檔案 %MAIN_PROJECT%
    pause
    exit /b 1
)

echo 所有必要檔案都存在
echo.

REM 清理先前的建構
echo 正在清理先前的建構...
if exist "DDSWebAPI\bin" rmdir /s /q "DDSWebAPI\bin"
if exist "DDSWebAPI\obj" rmdir /s /q "DDSWebAPI\obj"
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
echo 清理完成
echo.

REM 還原 NuGet 套件
echo 正在還原 NuGet 套件...
nuget restore %SOLUTION_FILE%
if errorlevel 1 (
    echo 警告: NuGet 套件還原失敗，但繼續建構...
)
echo.

REM 建構 DDSWebAPI 函式庫
echo 正在建構 DDSWebAPI 函式庫...
msbuild "%LIBRARY_PROJECT%" /p:Configuration=%BUILD_CONFIG% /p:Platform="Any CPU" /v:minimal
if errorlevel 1 (
    echo 錯誤: DDSWebAPI 函式庫建構失敗
    pause
    exit /b 1
)
echo DDSWebAPI 函式庫建構成功
echo.

REM 建構主 WPF 應用程式
echo 正在建構主 WPF 應用程式...
msbuild "%MAIN_PROJECT%" /p:Configuration=%BUILD_CONFIG% /p:Platform="Any CPU" /v:minimal
if errorlevel 1 (
    echo 錯誤: 主應用程式建構失敗
    pause
    exit /b 1
)
echo 主應用程式建構成功
echo.

REM 檢查建構結果
echo 正在檢查建構結果...
set DDL_FILE=DDSWebAPI\bin\%BUILD_CONFIG%\DDSWebAPI.dll
set EXE_FILE=bin\%BUILD_CONFIG%\OthinCloud.exe

if not exist "%DDL_FILE%" (
    echo 錯誤: 找不到 DDSWebAPI.dll
    pause
    exit /b 1
)

if not exist "%EXE_FILE%" (
    echo 錯誤: 找不到 OthinCloud.exe
    pause
    exit /b 1
)

echo.
echo ========================================
echo 建構完成！
echo ========================================
echo.
echo 建構產物位置:
echo   DDSWebAPI 函式庫: %DDL_FILE%
echo   主應用程式: %EXE_FILE%
echo.
echo 您現在可以:
echo   1. 執行主應用程式測試整合功能
echo   2. 將 DDSWebAPI.dll 用於其他專案
echo   3. 部署到目標環境
echo.

REM 詢問是否要執行應用程式
set /p RUN_APP=是否要執行主應用程式進行測試? (y/n): 
if /i "%RUN_APP%"=="y" (
    echo.
    echo 正在啟動應用程式...
    start "" "%EXE_FILE%"
)

echo.
echo 建構腳本執行完成
pause
