@echo off
REM Code the Park - measures the frame rate (FPS) of the game on the Quest 3 (written by us).
REM The Quest writes one "VrApi FPS=..." line every second to its log. We save those lines.
title Quest FPS
cd /d "%~dp0"
set ADB=adb
where adb >nul 2>nul
if errorlevel 1 (
  for /d %%U in ("C:\Program Files\Unity\Hub\Editor\*") do if exist "%%U\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" set ADB="%%U\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
)
echo Using: %ADB%
%ADB% devices
%ADB% logcat -G 16M >nul 2>nul
echo.
echo 1) Keep the Quest connected with the cable and put it on.
echo 2) Start Code the Park on the Quest.
echo 3) Come back here and press any key, then play for about 3 minutes
echo    (walk around, look at the rides, run some code, watch a ride turn).
pause
%ADB% logcat -c
echo.
echo Measuring... play now. When you finish (about 3 minutes), press any key.
pause
%ADB% logcat -d -v time -s VrApi > fps_log.txt
echo.
echo Saved: fps_log.txt
pause
