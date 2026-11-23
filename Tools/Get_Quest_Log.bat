@echo off
REM Code the Park - saves the Quest 3 log (messages of our game) to quest_log.txt (written by us).
title Quest log
cd /d "%~dp0"
set ADB=adb
where adb >nul 2>nul
if errorlevel 1 (
  for /d %%U in ("C:\Program Files\Unity\Hub\Editor\*") do if exist "%%U\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" set ADB="%%U\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
)
echo Using: %ADB%
%ADB% devices
echo.
echo 1) Keep the Quest connected with the cable and put it on.
echo 2) Start Code the Park on the Quest NOW, wait until Codey talks.
echo 3) Then come back here and press any key.
%ADB% logcat -c
pause
%ADB% logcat -d -v time Unity:V AudioTrack:V AudioFlinger:V AudioPolicyManager:V fmod:V *:S > quest_log.txt
%ADB% logcat -d -v time | findstr /i "audio fmod unity exception error" > quest_log_audio.txt
echo.
echo Saved: quest_log.txt and quest_log_audio.txt
pause
