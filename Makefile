# Repo task runner. Recipes assume a POSIX shell (Linux, macOS, or WSL/Git Bash
# on Windows) — they use `&&`, `[ -f ... ]` and `cp`, which cmd.exe does not have.
#
# `make` with no target prints the self-documenting target list below. The `##`
# trailing comments are what `make help` scrapes, so keep them one line each.
#
# The toolchains themselves (the .NET SDK, Node, Python, Docker) are prerequisites
# documented in the README — `make setup` bootstraps the *project* (env file,
# package restore) but deliberately does not install SDKs, which is platform- and
# version-manager-specific and better left to the developer.

# --- Paths -------------------------------------------------------------------
BACKEND_DIR  := backend
SOLUTION     := CheapAnalysis.sln
FRONTEND_DIR := frontend
SIDECAR_DIR  := ibkr-sidecar
PROTO_DIR    := backend/proto
PROTO_FILE   := $(PROTO_DIR)/ibkr.proto
CONTRACTS    := backend/src/CheapAnalysis.Ibkr.Contracts/CheapAnalysis.Ibkr.Contracts.csproj

# Migrations live in CheapAnalysis.Infrastructure; its design-time factory lets
# `dotnet ef` build AppDbContext without booting the API host, so the same
# project serves as both --project and --startup-project. Paths are relative to
# BACKEND_DIR because the recipes cd there first.
MIGRATIONS_PROJECT := src/CheapAnalysis.Infrastructure

.DEFAULT_GOAL := help

.PHONY: help setup up down migrate seed test format generate-api \
        proto proto-dotnet proto-python

help: ## Show this list of targets
	@grep -E '^[a-zA-Z0-9_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| sort \
		| awk 'BEGIN {FS = ":.*?## "} {printf "  \033[36m%-14s\033[0m %s\n", $$1, $$2}'

setup: ## Bootstrap the project: create .env, restore .NET + tools, install deps
	@[ -f .env ] || (cp .env.example .env && echo "created .env from .env.example")
	cd $(BACKEND_DIR) && dotnet tool restore && dotnet restore $(SOLUTION)
	cd $(FRONTEND_DIR) && npm install
	python -m pip install -e "$(SIDECAR_DIR)[dev]"

up: ## Start the local dev stack in the background (docker compose up -d)
	docker compose up -d

down: ## Stop the dev stack and remove its containers (docker compose down)
	docker compose down

migrate: ## Apply EF Core migrations to the database (dotnet ef database update)
	cd $(BACKEND_DIR) && dotnet ef database update \
		--project $(MIGRATIONS_PROJECT) --startup-project $(MIGRATIONS_PROJECT)

seed: ## Insert demo/seed data (no seed data exists yet — see T-301)
	@echo "No seed data yet. Category seeding lands with T-301; demo user TBD."

test: ## Run backend (dotnet) and frontend (jest) test suites
	cd $(BACKEND_DIR) && dotnet test $(SOLUTION)
	cd $(FRONTEND_DIR) && npm test

format: ## Format backend (dotnet format) and frontend (prettier) sources
	cd $(BACKEND_DIR) && dotnet format $(SOLUTION)
	cd $(FRONTEND_DIR) && npm run format

generate-api: ## Regenerate the TypeScript API client from the OpenAPI schema (NSwag)
	cd $(FRONTEND_DIR) && npm run generate-api

# --- gRPC stubs (T-007) ------------------------------------------------------
# `make proto` regenerates the gRPC stubs on BOTH sides of the IBKR contract
# from the single source of truth at backend/proto/ibkr.proto:
#   - .NET:   Grpc.Tools regenerates the client stubs into obj/ as part of
#             building CheapAnalysis.Ibkr.Contracts (output is gitignored).
#   - Python: grpc_tools.protoc emits ibkr_pb2.py / ibkr_pb2_grpc.py next to the
#             sidecar entrypoint (also gitignored). Needs the sidecar's [dev]
#             extra installed: `pip install -e ibkr-sidecar[dev]`.

proto: proto-python proto-dotnet ## Regenerate gRPC stubs for both .NET and Python

proto-dotnet:
	dotnet build $(CONTRACTS)

proto-python:
	python -m grpc_tools.protoc -I $(PROTO_DIR) --python_out=$(SIDECAR_DIR) --grpc_python_out=$(SIDECAR_DIR) $(PROTO_FILE)
