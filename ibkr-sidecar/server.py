"""gRPC sidecar entrypoint.

For T-006 this serves only the standard ``grpc.health.v1.Health`` service with
overall status hardcoded to ``SERVING``. The IBKR ``ib_insync`` integration and
the project-specific ``ibkr.proto`` service land in later tasks (T-007 onward).
"""

from __future__ import annotations

import logging
import os
import signal
from concurrent import futures

import grpc
from grpc_health.v1 import health, health_pb2, health_pb2_grpc

DEFAULT_PORT = 50051
DEFAULT_MAX_WORKERS = 4
SHUTDOWN_GRACE_SECONDS = 5.0

logger = logging.getLogger("ibkr_sidecar")


def build_server(port: int, max_workers: int) -> tuple[grpc.Server, health.HealthServicer]:
    """Wire a gRPC server with the standard health service set to SERVING."""
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=max_workers))

    health_servicer = health.HealthServicer()
    health_pb2_grpc.add_HealthServicer_to_server(health_servicer, server)
    # Empty service name "" is the overall server status convention.
    health_servicer.set("", health_pb2.HealthCheckResponse.SERVING)

    server.add_insecure_port(f"0.0.0.0:{port}")
    return server, health_servicer


def main() -> None:
    logging.basicConfig(
        level=os.environ.get("IBKR_SIDECAR_LOG_LEVEL", "INFO"),
        format='{"ts":"%(asctime)s","level":"%(levelname)s","msg":%(message)r}',
    )

    port = int(os.environ.get("IBKR_SIDECAR_PORT", DEFAULT_PORT))
    max_workers = int(os.environ.get("IBKR_SIDECAR_MAX_WORKERS", DEFAULT_MAX_WORKERS))

    server, health_servicer = build_server(port=port, max_workers=max_workers)
    server.start()
    logger.info("ibkr-sidecar listening on 0.0.0.0:%d", port)

    stopping = False

    def handle_signal(signal_number: int, _frame: object) -> None:
        nonlocal stopping
        if stopping:
            return
        stopping = True
        logger.info("received signal %d, shutting down", signal_number)
        # Flip status to NOT_SERVING so load balancers stop sending traffic
        # before the listener actually closes.
        health_servicer.set("", health_pb2.HealthCheckResponse.NOT_SERVING)
        server.stop(SHUTDOWN_GRACE_SECONDS)

    signal.signal(signal.SIGINT, handle_signal)
    signal.signal(signal.SIGTERM, handle_signal)

    server.wait_for_termination()


if __name__ == "__main__":
    main()
