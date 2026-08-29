@echo off
title Shadow Protocol - Backend API Service
echo Starting Shadow Protocol FastAPI Backend on http://localhost:8000 ...
cd Backend
python -m uvicorn app.main:app --reload --host 127.0.0.1 --port 8000
pause
