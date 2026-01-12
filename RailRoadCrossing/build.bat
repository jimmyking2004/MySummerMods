@echo off
setlocal

:: ===============================
:: USER CONFIGURATION
:: ===============================

:: Change this to your game data folder can either be Winter or Summer
set GAME_PATH=D:\Programs\Steam\steamapps\common\My Winter Car\mywintercar_Data

:: ===============================
:: FIND MSBUILD
:: ===============================
set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if not exist %VSWHERE% (
    echo ERROR: vswhere not found.
    echo Please install Visual Studio or Visual Studio Build Tools.
    pause
    exit /b 1
)

for /f "usebackq tokens=*" %%i in (`
    %VSWHERE% -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe
`) do set MSBUILD=%%i

if not defined MSBUILD (
    echo ERROR: MSBuild not found.
    pause
    exit /b 1
)

echo Using MSBuild:
echo %MSBUILD%
echo.

:: ===============================
:: BUILD
:: ===============================
"%MSBUILD%" RailRoadCrossing.csproj ^
 /p:GamePath="%GAME_PATH%" ^
 /p:Configuration=Release ^
 /nologo

if errorlevel 1 (
    echo.
    echo BUILD FAILED
    pause
    exit /b 1
)

echo.
echo BUILD SUCCESS
pause