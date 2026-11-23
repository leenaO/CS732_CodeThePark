@echo off
REM Code the Park - starts Codey's AI (written by us). Double-click this file.
title Codey AI backend
cd /d "%~dp0"
echo.
echo === Code the Park - Codey AI ===
echo.
where ollama >nul 2>nul
if errorlevel 1 (
  echo Ollama is not installed. Install it from https://ollama.com and run this file again.
  pause
  exit /b
)
echo Starting Ollama (if it is not running yet)...
start "" /min ollama serve
timeout /t 3 >nul
echo Making sure the model llama3.2:1b is downloaded (only the first time takes long)...
ollama pull llama3.2:1b
echo.
python --version >nul 2>nul
if errorlevel 1 (
  py codey_backend.py %*
) else (
  python codey_backend.py %*
)
pause
