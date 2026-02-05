@echo off

REM ===============================================================
REM Run Mud.Feishu.Tests project tests batch file
REM Author: Mud Studio
REM Date: 2026-02-05
REM ===============================================================

echo ===============================================================
echo Starting Mud.Feishu.Tests project tests
echo Current directory: %cd%
echo Execution time: %date% %time%
echo ===============================================================

REM Check if we're in the project root directory
if not exist "Tests\Mud.Feishu.Tests" (
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

echo 1. Running Mud.Feishu.Tests project tests...
echo ===============================================================

REM Define test report directory
set TEST_REPORT_DIR=test-reports

REM Create test report directory
if not exist "%TEST_REPORT_DIR%" mkdir "%TEST_REPORT_DIR%"

REM Run the Mud.Feishu.Tests project tests with XML logger
dotnet test Tests\Mud.Feishu.Tests --logger "trx;LogFileName=feishu-tests.trx" --results-directory "%TEST_REPORT_DIR%"

REM Check test results
if %errorlevel% neq 0 (
    echo ===============================================================
    echo Test execution failed! Please check the error messages above
    echo ===============================================================
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\feishu-tests.trx
    pause
    exit /b 1
) else (
    echo ===============================================================
    echo Mud.Feishu.Tests project tests executed successfully!
    echo ===============================================================
    echo Test report generated at: %cd%\%TEST_REPORT_DIR%\feishu-tests.trx
)

REM Check if ReportGenerator is available
where ReportGenerator >nul 2>nul
if %errorlevel% equ 0 (
    echo.
    echo Generating HTML test report...
    echo ===============================================================
    
    REM Generate HTML report
    ReportGenerator "-reports:%TEST_REPORT_DIR%\feishu-tests.trx" "-targetdir:%TEST_REPORT_DIR%\html-feishu" "-reporttypes:Html"
    
    if %errorlevel% neq 0 (
        echo Warning: Failed to generate HTML report
    ) else (
        echo HTML test report generated at: %cd%\%TEST_REPORT_DIR%\html-feishu\index.htm
    )
) else (
    echo.
    echo Info: ReportGenerator not found. To generate HTML reports, install it with: dotnet tool install -g dotnet-reportgenerator-globaltool
)

echo ===============================================================
echo Test execution completed
echo Execution time: %date% %time%
echo ===============================================================

pause