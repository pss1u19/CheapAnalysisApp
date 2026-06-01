# Image for the Python gRPC sidecar. T-006 serves only grpc.health.v1.Health
# (status hardcoded to SERVING); ib_insync + the IBKR Gateway land later, so no
# gateway dependency here yet (T-011 brings up the sidecar standalone).
FROM python:3.12-slim
WORKDIR /app

# Installing the package from pyproject pulls grpcio + grpcio-health-checking.
# server.py is copied first because the build backend declares py-modules=["server"].
COPY pyproject.toml ./
COPY server.py ./
RUN pip install --no-cache-dir .

EXPOSE 50051

# Port/log level are read from the environment by server.py (defaults: 50051/INFO).
CMD ["python", "server.py"]
