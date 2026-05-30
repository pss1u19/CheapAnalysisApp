# Repo task runner. T-007 introduces the `proto` target; the remaining targets
# (setup, up, down, migrate, seed, test, format, generate-api) land with T-012.
#
# `make proto` regenerates the gRPC stubs on BOTH sides of the IBKR contract
# from the single source of truth at backend/proto/ibkr.proto:
#   - .NET:   Grpc.Tools regenerates the client stubs into obj/ as part of
#             building CheapAnalysis.Ibkr.Contracts (output is gitignored).
#   - Python: grpc_tools.protoc emits ibkr_pb2.py / ibkr_pb2_grpc.py next to the
#             sidecar entrypoint (also gitignored). Needs the sidecar's [dev]
#             extra installed: `pip install -e ibkr-sidecar[dev]`.

PROTO_DIR   := backend/proto
PROTO_FILE  := $(PROTO_DIR)/ibkr.proto
SIDECAR_DIR := ibkr-sidecar
CONTRACTS   := backend/src/CheapAnalysis.Ibkr.Contracts/CheapAnalysis.Ibkr.Contracts.csproj

.PHONY: proto proto-dotnet proto-python

proto: proto-python proto-dotnet

proto-dotnet:
	dotnet build $(CONTRACTS)

proto-python:
	python -m grpc_tools.protoc -I $(PROTO_DIR) --python_out=$(SIDECAR_DIR) --grpc_python_out=$(SIDECAR_DIR) $(PROTO_FILE)
