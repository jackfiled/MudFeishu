@echo off
echo ========================================
echo   FeishuFileServer Frontend Service
echo ========================================
echo.

cd /d "%~dp0frontend"

echo [CHECK] Checking node_modules...
if not exist "node_modules" (
    echo [WARN] node_modules not found, installing dependencies...
    npm install
    if %errorlevel% neq 0 (
        echo [ERROR] Dependency installation failed!
        pause
        exit /b 1
    )
)

echo.
echo [START] Starting frontend dev server...
echo [INFO] Service URL: http://localhost:5173
echo [INFO] Press Ctrl+C to stop
echo.
echo ========================================

npm run dev
