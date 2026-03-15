@echo off
echo ========================================
echo   FeishuFileServer Backend Service
echo ========================================
echo.

cd /d "%~dp0backend"

echo [CHECK] Checking build output...
if not exist "bin\Release\net8.0\FeishuFileServer.dll" (
    echo [WARN] Build output not found, building...
    dotnet build --configuration Release
    if %errorlevel% neq 0 (
        echo [ERROR] Build failed!
        pause
        exit /b 1
    )
)

echo.
echo [START] Starting backend service...
echo [INFO] Service URL: http://localhost:5000
echo [INFO] Swagger URL: http://localhost:5000/swagger
echo [INFO] Press Ctrl+C to stop
echo.
echo ========================================

dotnet run --configuration Release --urls="http://localhost:5000"
