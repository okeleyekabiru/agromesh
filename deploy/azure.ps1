# Deploys the AgroMesh API to Azure Container Apps (consumption plan, scaled to zero) with Supabase Postgres.
#
# Before running:
#   1. winget install Microsoft.AzureCLI ; az login
#   2. docker login ghcr.io   (a GitHub token with write:packages)
#   3. Put the secrets in the CURRENT PowerShell session, never in this file:
#        $env:AGRO_DB      = "<Supabase SESSION POOLER string, with SSL Mode=Require>"
#        $env:AGRO_JWT     = "<a new long random secret, 48+ characters>"
#        $env:AGRO_ANTHROPIC, $env:AGRO_MONO_SECRET, $env:AGRO_MONO_PUBLIC, $env:AGRO_MONO_WEBHOOK,
#        $env:AGRO_COPERNICUS_ID, $env:AGRO_COPERNICUS_SECRET   (leave a key unset to skip it)
#   4. Run:  ./deploy/azure.ps1 -Image ghcr.io/<your-github-user>/agromesh-api
#
# The database migrations are NOT run here: run `dotnet ef database update` against Supabase once, first.

param(
    [Parameter(Mandatory = $true)][string]$Image,
    [string]$ResourceGroup = "agromesh",
    [string]$Location = "westeurope",
    [string]$AppName = "agromesh-api",
    [string]$WakeTimeZone = "Africa/Lagos",
    [string]$WakeAt = "0 3 * * *",       # the app is woken once a day at 03:00 ...
    [string]$WakeUntil = "15 3 * * *"    # ... and may sleep again from 03:15
)

$ErrorActionPreference = "Stop"
if (-not $env:AGRO_DB -or -not $env:AGRO_JWT) { throw "Set AGRO_DB and AGRO_JWT first (see the top of this file)." }

Write-Host "Building and pushing $Image ..."
docker build -t $Image ./api
docker push $Image

az group create -n $ResourceGroup -l $Location | Out-Null
az containerapp env create -n "$AppName-env" -g $ResourceGroup -l $Location | Out-Null

# Secrets: only the ones that are set. Names are lowercase; env vars refer to them with secretref:.
$secrets = @{ "dbconn" = $env:AGRO_DB; "jwtkey" = $env:AGRO_JWT }
$envVars = @("ConnectionStrings__DefaultConnection=secretref:dbconn", "Jwt__Key=secretref:jwtkey", "ASPNETCORE_ENVIRONMENT=Production")
$optional = @(
    @("anthropic",  $env:AGRO_ANTHROPIC,         "Anthropic__ApiKey"),
    @("monosecret", $env:AGRO_MONO_SECRET,       "Mono__SecretKey"),
    @("monopublic", $env:AGRO_MONO_PUBLIC,       "Mono__PublicKey"),
    @("monohook",   $env:AGRO_MONO_WEBHOOK,      "Mono__WebhookSecret"),
    @("copid",      $env:AGRO_COPERNICUS_ID,     "Copernicus__ClientId"),
    @("copsecret",  $env:AGRO_COPERNICUS_SECRET, "Copernicus__ClientSecret")
)
foreach ($o in $optional) {
    if ($o[1]) { $secrets[$o[0]] = $o[1]; $envVars += "$($o[2])=secretref:$($o[0])" }
}
$secretArgs = $secrets.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }

# Scaled to zero: free while idle. Two things wake it: any HTTP request (the apps, the Mono webhook), and a daily
# cron window so the daily bank sync and satellite refresh get to run (they start a minute or two after the app starts).
az containerapp create -n $AppName -g $ResourceGroup --environment "$AppName-env" `
    --image $Image --target-port 8080 --ingress external `
    --cpu 0.25 --memory 0.5Gi --min-replicas 0 --max-replicas 1 `
    --secrets $secretArgs --env-vars $envVars `
    --scale-rule-name daily-wake --scale-rule-type cron `
    --scale-rule-metadata "timezone=$WakeTimeZone" "start=$WakeAt" "end=$WakeUntil" "desiredReplicas=1"

$fqdn = az containerapp show -n $AppName -g $ResourceGroup --query properties.configuration.ingress.fqdn -o tsv
Write-Host ""
Write-Host "API is at https://$fqdn"
Write-Host "Web:    build with VITE_API_URL=https://$fqdn"
Write-Host "Mobile: flutter run --dart-define=API_BASE_URL=https://$fqdn"
