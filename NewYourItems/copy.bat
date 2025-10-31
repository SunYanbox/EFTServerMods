@echo off
setlocal enabledelayedexpansion

:: 避免模组文件被占用
taskkill -F -T -IM SPT.Server.exe

:: 设置源路径和目标路径
set "SOURCE_PATH=E:\EFT_Mods\EFT_Mods\NewYourItems"
set "DEST_PATH=F:\EFT_4_0_0\SPT\user\mods\NewYourItems"

:: 复制目录
echo copy folder now...
xcopy "%SOURCE_PATH%\data" "%DEST_PATH%\data\" /E /I /H /Y
@REM copy "%SOURCE_PATH%\locals\*" "%DEST_PATH%\locals\"

echo copy folder now...
xcopy "%SOURCE_PATH%\bundles" "%DEST_PATH%\bundles\" /E /I /H /Y
@REM copy "%SOURCE_PATH%\locals\*" "%DEST_PATH%\locals\"

set "SOURCE_PATH=E:\EFT_Mods\EFT_Mods\NewYourItems\bin\Release\NewYourItems\NewYourItems.dll"
set "DEST_PATH=F:\EFT_4_0_0\SPT\user\mods\NewYourItems"
copy "%SOURCE_PATH%" "%DEST_PATH%\"

echo copy file now：
echo source: %SOURCE_PATH%
echo target: %DEST_PATH%\
echo.

set "SOURCE_PATH=E:\EFT_Mods\EFT_Mods\NewYourItems\bundles.json"
copy "%SOURCE_PATH%" "%DEST_PATH%\"
echo copy file now：
echo source: %SOURCE_PATH%
echo target: %DEST_PATH%\
echo.

echo copy finash!
pause