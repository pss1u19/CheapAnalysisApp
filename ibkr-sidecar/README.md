# ibkr-sidecar

Python gRPC sidecar that will broker requests from the .NET backend to
Interactive Brokers via [`ib_insync`](https://ib-insync.readthedocs.io/).

The running server is still the **T-006 skeleton**: only the standard
[`grpc.health.v1.Health`](https://github.com/grpc/grpc/blob/master/doc/health-checking.md)
service is wired up, returning a hardcoded `SERVING` status.

The project's own contract now exists at [`backend/proto/ibkr.proto`](../backend/proto/ibkr.proto)
(**T-007**): the `cheapanalysis.ibkr.v1.IbkrService` with a single `HealthCheck`
RPC. Serving that service from this sidecar and the real `ib_insync` integration
land in Phase 4 (T-401+).

## Requirements

- Python 3.11 or 3.12 (pinned via `.python-version`)
- `pip` / `venv`

## Run locally

```powershell
# from ibkr-sidecar/
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -e .
python server.py
```

Server listens on `0.0.0.0:50051` by default. Override via env vars:

| Variable                    | Default | Purpose                         |
| --------------------------- | ------- | ------------------------------- |
| `IBKR_SIDECAR_PORT`         | `50051` | TCP port                        |
| `IBKR_SIDECAR_MAX_WORKERS`  | `4`     | ThreadPoolExecutor size         |
| `IBKR_SIDECAR_LOG_LEVEL`    | `INFO`  | Python logging level            |

## Probe the health endpoint

With [`grpcurl`](https://github.com/fullstorydev/grpcurl):

```powershell
grpcurl -plaintext localhost:50051 grpc.health.v1.Health/Check
# {"status": "SERVING"}
```

Or with the Python health-check client:

```powershell
python -c "import grpc; from grpc_health.v1 import health_pb2, health_pb2_grpc; `
ch = grpc.insecure_channel('localhost:50051'); `
print(health_pb2_grpc.HealthStub(ch).Check(health_pb2.HealthCheckRequest()))"
```

## Regenerating gRPC stubs

The Python stubs are generated from [`backend/proto/ibkr.proto`](../backend/proto/ibkr.proto)
and are **not** committed (they are gitignored). Regenerate them from the repo
root with:

```powershell
make proto            # both sides; or just the Python half:
make proto-python
```

That emits `ibkr_pb2.py` and `ibkr_pb2_grpc.py` next to `server.py`. The
underlying command (if you don't have `make`) is:

```powershell
# from the repo root, with ibkr-sidecar[dev] installed
python -m grpc_tools.protoc -I backend/proto --python_out=ibkr-sidecar `
  --grpc_python_out=ibkr-sidecar backend/proto/ibkr.proto
```

`tests/test_proto_codegen.py` regenerates the stubs into a temp dir and asserts
they import and round-trip, so it does not require a prior `make proto`.

## Graceful shutdown

`SIGINT` / `SIGTERM` flip the health status to `NOT_SERVING`, then wait up to
5 seconds for in-flight RPCs before closing the listener.
