# copytrader-sample

Sample project: a Binance USD-M futures copy trader.

- `src/trader-service`: .NET 7 console worker. Listens to the trader accounts' Binance user data streams and mirrors orders on the follower accounts.
- `src/web`: ASP.NET Core 7 MVC admin UI (Identity, EF Core, SQL Server) to manage API keys and trader groups.

This is a cleaned-up import of an older project. The goal of this repository is to practice the DevOps side around it: containers, Terraform, EKS, CI/CD, observability.
Use the Binance **testnet** only. Never use real keys.

## Configuration

All secrets come from environment variables, see `.env.example`.

| Component | Variable |
|-----------|----------|
| web | `ConnectionStrings__DefaultConnection` |
| trader-service | `COPYTRADER_DB_CONNECTION_STRING` |

Both applications fail on startup if the variable is missing.

## Run locally

Requires Docker and the .NET 7 SDK. The local database is a throwaway SQL Server container with fake data.

```
docker compose up -d
```

`db-init` creates the `CopyTrader` database, an app login with minimal rights (`copytrader_app`), the schema from `db/schema.sql` and the mock data from `db/seed.sql` (one trader, two followers, one registration key `LOCAL-DEV-KEY`).

Load the local connection strings (bash: `set -a; source .env.local.example; set +a`, PowerShell: set the two variables by hand) and start the apps:

```
dotnet run --project src/web/CopyTrader.Web.csproj
dotnet run --project src/trader-service/CopyTrader.Worker/CopyTrader.Worker.csproj
```

Register in the web UI with the key `LOCAL-DEV-KEY`, then make that user an admin:

```
docker compose exec db-init /opt/mssql-tools18/bin/sqlcmd -C -S db -U sa -P 'LocalDev_Passw0rd!' -d CopyTrader -v ADMIN_EMAIL=you@example.com -i /db/promote-admin.sql
```

The seeded Binance keys are placeholders, so the trader service starts and reads the database but cannot authenticate against Binance. Switching to a real database means only changing the two connection string variables, no code change.

## Container images

```
docker build -f docker/web.Dockerfile -t copytrader-web:dev .
docker build -f docker/worker.Dockerfile -t copytrader-worker:dev .
```

Build context is the repository root. The web image listens on port 8080 and serves `/healthz` (used by Kubernetes probes). The worker logs to stdout and stops cleanly on SIGTERM; if its update loop crashes (for example a missing connection string) it exits with code 1 so the failure is visible as a restart.

## Local Kubernetes (kind)

Requires `kind` and `kubectl`. Stop the compose database first (`docker compose down`), because the cluster runs its own SQL Server.

```
./scripts/local-up.ps1
```

The script creates the cluster, installs ingress-nginx, builds and loads the images, applies `k8s/overlays/local` and waits until everything is ready. The web is then available at `http://copytrader.localtest.me:8080`. Remove everything with `./scripts/local-down.ps1`.

Local secrets are generated from `*.env.example` files into git-ignored `*.env` files. Kustomize needs `--load-restrictor LoadRestrictionsNone` because the SQL scripts live in `db/`, outside the overlay folder.

## CI

`.github/workflows/ci.yml` runs on every pull request and on pushes to `main`: secret scan (gitleaks), .NET build, Trivy repository scan, Kubernetes manifest validation (kustomize and kubeconform), and a build and Trivy scan of both images. Images are pushed to GHCR tagged with the commit SHA, only from `main` and only if every earlier job passed. Actions are pinned to commit SHAs, Dependabot proposes updates.

## Build

```
dotnet build src/web/CopyTrader.Web.csproj
dotnet build src/trader-service/CopyTrader.Worker/CopyTrader.Worker.csproj
```

## Roadmap

1. CI: build, test, secret scan (gitleaks), dependency scan
2. Containerize both apps (Linux, non-root)
3. Terraform: VPC, EKS, RDS, ECR, remote state
4. Secrets via AWS Secrets Manager, workload identity (no static credentials)
5. Observability and alerting, backup/restore drill

## Known issues (tracked as follow-up PRs)

- EF migrations are outdated: the schema is defined by `db/schema.sql` until migrations are regenerated
- Binance API secrets are stored unencrypted in the database
- Authentication middleware should be replaced by a fallback authorization policy
- Quantity ratio and order cancel matching logic need rework
- Async/WebSocket handling needs rework (awaiting, reconnect, fragmented messages)
- Vendored front-end libraries in `wwwroot/lib` should move to a package manager
