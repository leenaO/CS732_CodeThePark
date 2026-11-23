@echo off
REM Code the Park - starts Codey's AI so the Quest 3 can reach it over Wi-Fi (written by us).
title Codey AI backend (Quest / Wi-Fi)
cd /d "%~dp0"
echo.
echo === Put THIS address in Unity: GameSystems ^> Codey AI ^> Backend Url ===
for /f "tokens=2 delims=:" %%a in ('ipconfig ^| findstr /c:"IPv4"') do echo     http:%%a:8000/ask
echo (use the one of your Wi-Fi; remove the space after "http:")
echo If Windows asks about the firewall, press "Allow access".
echo.
call Start_Codey_AI.bat --lan
