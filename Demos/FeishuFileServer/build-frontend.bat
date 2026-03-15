@echo off
echo ========================================
echo   FeishuFileServer Frontend Build
echo ========================================
echo.

cd /d "%~dp0frontend"

echo [1/2] Checking node_modules...
if not exist "node_modules" (
    echo [INFO] node_modules not found, installing dependencies...
    npm install
    if %errorlevel% neq 0 (
        echo.
        echo [ERROR] npm install failed!
        pause
        exit /b 1
    )
)

echo.
echo [2/2] Building frontend project...
npm run build
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Build Success!
echo   Output: dist
echo ========================================
pause
