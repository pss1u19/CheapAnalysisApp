"""Smoke test for the ibkr.proto Python codegen (T-007).

Regenerates the stubs from the canonical proto into a temp dir and asserts the
generated client + message types exist and round-trip. Kept self-contained: it
does not depend on committed generated code — the ``*_pb2*.py`` files are
gitignored and produced by ``make proto`` / ``make proto-python``.
"""

from __future__ import annotations

import importlib.util
import subprocess
import sys
from pathlib import Path
from types import ModuleType

REPO_ROOT = Path(__file__).resolve().parents[2]
PROTO_DIR = REPO_ROOT / "backend" / "proto"
PROTO_FILE = PROTO_DIR / "ibkr.proto"


def _load_module(module_name: str, module_path: Path) -> ModuleType:
    spec = importlib.util.spec_from_file_location(module_name, module_path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[module_name] = module
    spec.loader.exec_module(module)
    return module


def test_proto_generates_importable_stubs(tmp_path: Path) -> None:
    subprocess.run(
        [
            sys.executable,
            "-m",
            "grpc_tools.protoc",
            f"-I{PROTO_DIR}",
            f"--python_out={tmp_path}",
            f"--grpc_python_out={tmp_path}",
            str(PROTO_FILE),
        ],
        check=True,
    )

    pb2_path = tmp_path / "ibkr_pb2.py"
    grpc_path = tmp_path / "ibkr_pb2_grpc.py"
    assert pb2_path.exists(), "message stub was not generated"
    assert grpc_path.exists(), "grpc service stub was not generated"

    # ibkr_pb2_grpc imports ibkr_pb2 by bare name, so make the temp dir
    # importable before loading the service stub.
    sys.path.insert(0, str(tmp_path))
    try:
        ibkr_pb2 = _load_module("ibkr_pb2", pb2_path)
        ibkr_pb2_grpc = _load_module("ibkr_pb2_grpc", grpc_path)
    finally:
        sys.path.remove(str(tmp_path))

    # The client stub the .NET backend's counterpart calls into exists.
    assert hasattr(ibkr_pb2_grpc, "IbkrServiceStub")

    # Messages round-trip, and the DEGRADED status from ARCHITECTURE.md §9.4 is
    # part of the contract.
    response = ibkr_pb2.HealthCheckResponse(
        status=ibkr_pb2.HealthCheckResponse.DEGRADED,
        detail="ib_gateway socket closed",
    )
    restored = ibkr_pb2.HealthCheckResponse.FromString(response.SerializeToString())
    assert restored.status == ibkr_pb2.HealthCheckResponse.DEGRADED
    assert restored.detail == "ib_gateway socket closed"
