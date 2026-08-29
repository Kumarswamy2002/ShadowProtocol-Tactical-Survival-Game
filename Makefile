.PHONY: all build test run docker-up clean help

all: build test

build:
	@echo "[BUILD] Compiling C# Solutions and verifying Python dependencies..."
	dotnet build ShadowProtocol.sln --configuration Release

test:
	@echo "[TEST] Running test suites with coverage..."
	pytest tests/ -v --cov=Backend/app --cov-report=term-missing
	dotnet test ShadowProtocol.sln

run:
	@echo "[RUN] Starting Shadow Protocol backend server..."
	python run_game_local.py

docker-up:
	@echo "[DOCKER] Launching containerized microservices..."
	docker compose -f Docker/docker-compose.yml up -d

clean:
	@echo "[CLEAN] Removing build artifacts and cache..."
	dotnet clean ShadowProtocol.sln
	find . -type d -name __pycache__ -exec rm -rf {} +
	find . -type d -name bin -exec rm -rf {} +
	find . -type d -name obj -exec rm -rf {} +

help:
	@echo "Shadow Protocol Build Automation:"
	@echo "  make build      - Build .NET Solution and Python modules"
	@echo "  make test       - Execute automated pytest and xUnit suites"
	@echo "  make run        - Run local standalone server & dashboard"
	@echo "  make docker-up  - Launch full Dockerized microservice stack"
