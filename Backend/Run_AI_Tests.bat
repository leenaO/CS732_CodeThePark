@echo off
REM Code the Park - runs the 5 AI tests for the report (written by us). Start_Codey_AI.bat must be running.
title Codey AI tests
cd /d "%~dp0"
python --version >nul 2>nul
if errorlevel 1 (py test_codey_ai.py) else (python test_codey_ai.py)
echo.
echo The results are saved in ai_test_results.md
pause
