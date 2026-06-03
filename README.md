# CheapAnalysisApp

Bulgaria-first personal finance and portfolio dashboard. PSD2 bank sync, IBKR portfolio data, AI summaries.

![Backend](https://img.shields.io/badge/backend-.NET%2010-512BD4?style=flat-square&logo=dotnet)
![Frontend](https://img.shields.io/badge/frontend-Angular-DD0031?style=flat-square&logo=angular&logoColor=white)
![Database](https://img.shields.io/badge/database-PostgreSQL-4169E1?style=flat-square&logo=postgresql&logoColor=white)
![License](https://img.shields.io/badge/license-PolyForm%20Noncommercial-orange?style=flat-square)

## Stack

```text
Frontend      Angular
Backend       .NET 10 + FastEndpoints
Database      PostgreSQL
Jobs          Hangfire + Redis
Brokerage     Python sidecar + gRPC + ib_insync
AI            Anthropic Claude
Infra         Docker Compose + GitHub Actions
```

## Repo layout

```text
backend/         ASP.NET Core API
frontend/        Angular app (planned)
ibkr-sidecar/    Python gRPC service for IBKR (planned)
proto/           Shared protobuf contracts (planned)
```

## Contributing

See [CONTRIBUTORS.md](CONTRIBUTORS.md) for local development, branch naming, and the CLA.

## License

Source-available under **PolyForm Noncommercial 1.0.0**. Commercial use requires a separate license — contact the maintainer.

See [LICENSE](LICENSE) and [CONTRIBUTORS.md](CONTRIBUTORS.md).
