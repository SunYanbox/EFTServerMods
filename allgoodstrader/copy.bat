@echo off
setlocal enabledelayedexpansion

:: 避免模组文件被占用
taskkill -F -T -IM SPT.Server.exe

:: 设置源路径和目标路径
set "SOURCE_PATH=E:\EFT_Mods\EFT_Mods\allgoodstrader"
set "DEST_PATH=F:\EFT_4_0_0\SPT\user\mods\allgoodstrader-0.4.0"

:: 复制目录
echo copy folder now...
xcopy "%SOURCE_PATH%\data" "%DEST_PATH%\data\" /E /I /H /Y
@REM copy "%SOURCE_PATH%\locals\*" "%DEST_PATH%\locals\"

:: 复制目录
echo copy folder now...
xcopy "%SOURCE_PATH%\res" "%DEST_PATH%\res\" /E /I /H /Y
@REM copy "%SOURCE_PATH%\locals\*" "%DEST_PATH%\locals\"


set "SOURCE_PATH=E:\EFT_Mods\EFT_Mods\allgoodstrader\bin\Release\allgoodstrader-0.4.0\allgoodstrader.dll"
set "DEST_PATH=F:\EFT_4_0_0\SPT\user\mods\allgoodstrader-0.4.0"
copy "%SOURCE_PATH%" "%DEST_PATH%\"

echo copy finash!
pause