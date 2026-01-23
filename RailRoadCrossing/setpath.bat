@echo off

:: Change working directory to \source
cd source

set "props=Directory.Build.props"

if exist %props% (
    echo Already existing Directory.Build.props. Delete?
    pause
    echo.
    echo Deleting %props%
    del /s /q %props% > nul
    echo.
)

:: ===============================
:: FIND STEAM INSTALL PATH
:: ===============================
for /f "tokens=2,*" %%A in ('
  reg query "HKCU\Software\Valve\Steam" /v SteamPath 2^>nul
') do set STEAM_PATH=%%B

if not defined STEAM_PATH (
    echo ERROR: Steam not found in registry ^(Is Steam installed?^)
    pause
    exit 1
)

:: Normalize slashes
set STEAM_PATH=%STEAM_PATH:/=\%

:: Add common
set STEAM_PATH=%STEAM_PATH%\steamapps\common

:: ===============================
:: DETECT GAME
:: ===============================
set MSC=%STEAM_PATH%\My Summer Car\mysummercar_Data
set MWC=%STEAM_PATH%\My Winter Car\mywintercar_Data

if exist "%MSC%" (
    set "GAME_PATH=%MSC%"
) else if exist "%MWC%" (
    set "GAME_PATH=%MWC%"
) else (
    echo ERROR: My Summer Car / My Winter Car not found ^(Is one of them installed?^)
    pause
    exit 1
)

echo Setting GAME_PATH:
echo %GAME_PATH%
echo.

:: ===============================
:: WRITE Directory.Build.props
:: ===============================
echo ^<Project^>^<PropertyGroup^>^<GamePath^>%GAME_PATH%^</GamePath^>^</PropertyGroup^>^</Project^> > %props%

echo Created %props%

if "%1"=="-build" exit /b 0

:: ===============================
:: PAUSE IF ONLY NO FLAG
:: ===============================
pause
exit 0