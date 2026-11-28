@echo off
rem Enciende el servicio en su propia ventana y abre el juego compilado, mucho mas ligero que el editor.
start "NeuralHub servicio (no cerrar)" /min /d "%~dp0servicio" cmd /k ".venv\Scripts\python.exe -m uvicorn neuralhub.api:app --host 127.0.0.1 --port 8000"
timeout /t 6 /nobreak >nul
start "" "%~dp0unity\NeuralHubUnity\Build\NeuralHub.exe" -screen-fullscreen 0 -screen-width 1280 -screen-height 720
