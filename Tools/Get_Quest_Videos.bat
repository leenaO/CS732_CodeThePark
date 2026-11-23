@echo off
REM Code the Park - copies the videos (and photos) recorded on the Quest 3 to the project (written by us).
title Copy Quest videos
cd /d "%~dp0"
set ADB=adb
where adb >nul 2>nul
if errorlevel 1 (
  for /d %%U in ("C:\Program Files\Unity\Hub\Editor\*") do if exist "%%U\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" set ADB="%%U\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
)
%ADB% devices
set OUT=..\..\Videos
if not exist "%OUT%" mkdir "%OUT%"
echo Copying videos from the Quest to FinalProject\Videos ...
%ADB% pull /sdcard/Oculus/VideoShots/. "%OUT%"
%ADB% pull /sdcard/Oculus/Screenshots/. "%OUT%\Screenshots"
echo.
echo Done. The files are in Desktop\FinalProject\Videos
pause
