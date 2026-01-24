@echo off

set src="%cd%\Source Files"

:: ===============================
:: GET OR MAKE AND GET GAME_PATH
:: ===============================
if not exist %src%\Directory.Build.props (
    if exist setpath.bat (
        call setpath.bat -build
        echo.
    ) else (
        echo ERROR: setpath.bat not found
        pause
        exit /b 1
    )
)

:: Change working directory to \Source Files
cd %src%

for /f "usebackq delims=" %%G in (`powershell -NoProfile ^
  "(Select-Xml -Path Directory.Build.props -XPath '//GamePath').Node.InnerText"`) do (
    set "GAME_PATH=%%G"
)

echo Using GAME_PATH:
echo %GAME_PATH%
echo.

:: ===============================
:: CHECK MSCLoader VERSION
:: ===============================
set MSCL_DLL=%GAME_PATH%\Managed\MSCLoader.dll

if not exist "%MSCL_DLL%" (
    echo ERROR: MSCLoader.dll not found
    pause
    exit /b 1
)

for /f "tokens=*" %%a in ('powershell -command "(Get-Item '%MSCL_DLL%').VersionInfo.FileVersion"') do (
    set "MSCL_VERSION=%%a"
)

for /f "tokens=1,2 delims=." %%a in ("%MSCL_VERSION%") do (
    if %%a LSS 1 goto version_fail
    if %%a EQU 1 if %%b LSS 4 goto version_fail
)

echo MSCLoader version OK: %MSCL_VERSION%
echo.
goto version_ok

:version_fail
echo ERROR: MSCLoader 1.4 or newer is required.
echo Found: %MSCL_VERSION%
pause
exit /b 1

:version_ok

:: ===============================
:: .NET FRAMEWORK 3.5 IS INSTALLED
:: ===============================
for /f "skip=2 tokens=3" %%A in ('
    reg query "HKLM\SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.5" /v Install 2^>nul
') do set NET35_INSTALLED=%%A

if "%NET35_INSTALLED%"=="0x1" (
    echo .NET Framework 3.5 is installed
    echo.
) else (
    echo ERROR: .NET Framework 3.5 is not installed ^(Install it via Windows features^)
    pause
    exit /b 1
)

:: ===============================
:: FIND MSBUILD
:: ===============================
set VSWHERE="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

if not exist %VSWHERE% (
    echo ERROR: vswhere not found ^(Is the Visual Studio Installer installed?^)
    pause
    exit /b 1
)

:: Set Visual Studio version (16-18.99) (2019-2026)
set "VS_Ver=^[16.0^,19.0^)"

for /f "usebackq tokens=*" %%i in (`
    %VSWHERE% ^
    -version %VS_Ver% ^
    -products * ^
    -requires Microsoft.Component.MSBuild ^
    -find MSBuild\**\Bin\MSBuild.exe
`) do set MSBUILD=%%i

if not defined MSBUILD (
    echo ERROR: MSBuild not found Or wrong version
    echo ^(Is MSBuild installed via Visual Studio and is Visual Studio 2019 installed?^)
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

