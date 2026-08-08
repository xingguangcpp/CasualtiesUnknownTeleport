@echo off
chcp 65001 >nul
setlocal EnableExtensions

set "GAME_DIR=F:\exe\a\steamapps\common\Casualties Unknown Demo"
set "PROJECT=KrokoshaTeleport.csproj"

echo.
echo ========================================
echo       Krokosha Teleport Build
echo ========================================
echo.

if not exist "%PROJECT%" (
    echo [错误] 找不到项目文件：
    echo %CD%\%PROJECT%
    pause
    exit /b 1
)

if not exist "%GAME_DIR%\BepInEx\core\BepInEx.dll" (
    echo [错误] 找不到 BepInEx.dll
    echo %GAME_DIR%\BepInEx\core\BepInEx.dll
    pause
    exit /b 1
)

if not exist "%GAME_DIR%\BepInEx\plugins\KrokMP\KrokoshaCasualtiesMP.dll" (
    echo [错误] 找不到 KrokoshaCasualtiesMP.dll
    echo %GAME_DIR%\BepInEx\plugins\KrokMP\KrokoshaCasualtiesMP.dll
    pause
    exit /b 1
)

if not exist "%GAME_DIR%\CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll" (
    echo [错误] 找不到 Assembly-CSharp.dll
    echo %GAME_DIR%\CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll
    pause
    exit /b 1
)

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [错误] 找不到 dotnet。
    echo 请安装 .NET SDK，或者打开 Developer Command Prompt。
    pause
    exit /b 1
)

echo [1/2] 开始编译...
dotnet build "%PROJECT%" ^
    -c Release ^
    -p:GameDir="%GAME_DIR%"

if errorlevel 1 (
    echo.
    echo ========================================
    echo             编译失败
    echo ========================================
    pause
    exit /b 1
)

if not exist "%GAME_DIR%\BepInEx\plugins\KrokoshaTeleport.dll" (
    echo.
    echo [警告] DLL 编译成功，但没有找到复制后的插件文件。
    pause
    exit /b 1
)

echo.
echo ========================================
echo             编译成功
echo ========================================
echo.
echo 插件位置：
echo %GAME_DIR%\BepInEx\plugins\KrokoshaTeleport.dll
echo.
pause