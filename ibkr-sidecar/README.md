# ibkr-sidecar

Python gRPC sidecar that will broker requests from the .NET backend to
Interactive Brokers via [`ib_insync`](https://ib-insync.readthedocs.io/).

This is the **T-006 skeleton**: only the standard
[`grpc.health.v1.Health`](https://github.com/grpc/grpc/blob/master/doc/health-checking.md)
service is wired up, returning a hardcoded `SERVING` status. The project's own
`ibkr.proto` and the actual IBKR integration land in later tasks (T-007+).

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

## Graceful shutdown

`SIGINT` / `SIGTERM` flip the health status to `NOT_SERVING`, then wait up to
5 seconds for in-flight RPCs before closing the listener.
