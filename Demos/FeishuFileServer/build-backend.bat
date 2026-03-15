@echo off
echo ========================================
echo   FeishuFileServer Backend Build
echo ========================================
echo.

cd /d "%~dp0backend"

echo [1/2] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] NuGet restore failed!
    pause
    exit /b 1
)

echo.
echo [2/2] Building project...
dotnet build --configuration Release
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Build Success!
echo   Output: bin\Release\net8.0
echo ========================================
pause
