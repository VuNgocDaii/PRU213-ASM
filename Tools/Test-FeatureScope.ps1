param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('economy', 'building', 'enemies', 'towers')]
    [string]$Role,
    [string]$BaseRef = 'origin/main',
    [switch]$IncludeWorkingTree,
    [string[]]$Paths
)

$ErrorActionPreference = 'Stop'
$scopeRoot = Split-Path -Parent $PSScriptRoot
$scopeRules = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'FeatureScopes.json') -Raw | ConvertFrom-Json
if (-not $PSBoundParameters.ContainsKey('Paths')) {
    # Include both sides of renames so moving a protected file cannot evade the scope check.
    $Paths = @(git -C $scopeRoot diff --name-only --no-renames "$BaseRef...HEAD")
    if ($LASTEXITCODE -ne 0) { throw 'Cannot compare the branch with BaseRef.' }
    if ($IncludeWorkingTree) {
        $Paths += @(git -C $scopeRoot diff --name-only --no-renames HEAD)
        if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect working-tree changes.' }
        $Paths += @(git -C $scopeRoot ls-files --others --exclude-standard)
        if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect untracked files.' }
    }
}
$allowedPatterns = $scopeRules.roles.$Role
$violations = @()
foreach ($scopePath in ($Paths | Sort-Object -Unique)) {
    $normalized = $scopePath.Replace('\', '/')
    if ([string]::IsNullOrWhiteSpace($normalized)) { continue }
    $blocked = @($scopeRules.protected | Where-Object { $normalized -match $_ }).Count -gt 0
    $allowed = @($allowedPatterns | Where-Object { $normalized -match $_ }).Count -gt 0
    if ($blocked -or -not $allowed) { $violations += $normalized }
}
if ($violations.Count -gt 0) {
    Write-Output "OUT OF SCOPE ($Role):"
    $violations | ForEach-Object { Write-Output "  $_" }
    Write-Output 'Keep shared changes in a separate base/integration PR.'
    exit 1
}
Write-Output "PASS: all compared paths belong to $Role."
