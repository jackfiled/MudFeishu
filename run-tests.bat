@echo off

REM ===============================================================
REM Auto-run Mud.Feishu project tests batch file
REM Author: Mud Studio
REM Date: 2026-01-25
REM ===============================================================

echo ===============================================================
echo Starting Mud.Feishu project tests
echo Current directory: %cd%
echo Execution time: %date% %time%
echo ===============================================================

REM Check if we're in the project root directory
if not exist "Tests" (
    echo Error: Current directory is not the project root. Please run this script in the MudFeishu directory
    pause
    exit /b 1
)

REM Check if dotnet command is available
where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo Error: dotnet command not found. Please ensure .NET SDK is installed
    pause
    exit /b 1
)

echo 1. Running all tests...
echo ===============================================================

REM Run the entire test suite
dotnet test

REM Check test results
if %errorlevel% neq 0 (
    echo ===============================================================
    echo Test execution failed! Please check the error messages above
    echo ===============================================================
    pause
    exit /b 1
) else (
    echo ===============================================================
    echo All tests executed successfully!
    echo ===============================================================
)

REM Run specific project tests (optional)
echo.
echo 2. Running specific project tests...
echo ===============================================================

echo Running WebSocket tests...
dotnet test Tests\Mud.Feishu.WebSocket.Tests
if %errorlevel% neq 0 (
    echo WebSocket tests failed!
) else (
    echo WebSocket tests passed!
)

echo.
echo Running Webhook tests...
dotnet test Tests\Mud.Feishu.Webhook.Tests
if %errorlevel% neq 0 (
    echo Webhook tests failed!
) else (
    echo Webhook tests passed!
)

echo ===============================================================
echo Test execution completed
echo Execution time: %date% %time%
echo ===============================================================

pause
