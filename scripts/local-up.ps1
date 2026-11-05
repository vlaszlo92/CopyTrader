$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

function Invoke-Step {
    param([string]$Description, [scriptblock]$Command)
    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Command
    if ($LASTEXITCODE -ne 0) { throw "Step failed: $Description" }
}

foreach ($name in 'db-admin', 'app-connection') {
    $target = "k8s/overlays/local/$name.env"
    if (-not (Test-Path $target)) { Copy-Item "$target.example" $target }
}

$clusterExists = (kind get clusters) -contains 'copytrader'
if (-not $clusterExists) {
    Invoke-Step 'Create kind cluster' { kind create cluster --name copytrader --config kind/kind-config.yaml }
}

Invoke-Step 'Install ingress-nginx' { kubectl apply -f https://kind.sigs.k8s.io/examples/ingress/deploy-ingress-nginx.yaml }
Invoke-Step 'Wait for ingress controller' {
    kubectl wait --namespace ingress-nginx --for=condition=ready pod --selector=app.kubernetes.io/component=controller --timeout=180s
}

Invoke-Step 'Build web image' { docker build -f docker/web.Dockerfile -t copytrader-web:dev . }
Invoke-Step 'Build worker image' { docker build -f docker/worker.Dockerfile -t copytrader-worker:dev . }
Invoke-Step 'Pull SQL Server image' { docker pull mcr.microsoft.com/mssql/server:2022-latest }
Invoke-Step 'Load images into kind' {
    kind load docker-image copytrader-web:dev copytrader-worker:dev mcr.microsoft.com/mssql/server:2022-latest --name copytrader
}

Invoke-Step 'Remove old db-init job (jobs are immutable)' { kubectl delete job db-init -n copytrader --ignore-not-found }
Invoke-Step 'Apply manifests' {
    kubectl kustomize --load-restrictor LoadRestrictionsNone k8s/overlays/local | kubectl apply -f -
}

Invoke-Step 'Wait for database initialization' { kubectl wait --for=condition=complete job/db-init -n copytrader --timeout=600s }
Invoke-Step 'Wait for web' { kubectl rollout status deployment/web -n copytrader --timeout=180s }
Invoke-Step 'Wait for worker' { kubectl rollout status deployment/worker -n copytrader --timeout=180s }

Write-Host 'Ready: http://copytrader.localtest.me:8080' -ForegroundColor Green
