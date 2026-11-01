# Run in a normal PowerShell terminal with GitHub network access.
# Uses Git Credential Manager; never accepts or stores a token in the remote URL.
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$expectedRemote = 'https://github.com/VuNgocDaii/PRU213-ASM.git'

function Invoke-CheckedGit {
    param([string[]]$GitArguments)
    & git @GitArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git failed: $($GitArguments -join ' '). Stop without overwriting history."
    }
}

Push-Location -LiteralPath $projectRoot
try {
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
        if (-not (Test-Path -LiteralPath $folder -PathType Container)) {
            throw "Missing Unity folder: $folder"
        }
    }
    $branch = & git symbolic-ref --quiet --short HEAD
    if ($LASTEXITCODE -ne 0 -or $branch -ne 'main') {
        throw 'Expected main. Inspect the current branch role before publishing; no branch was renamed.'
    }
    foreach ($remoteArgs in @(@('remote', 'get-url', 'origin'), @('remote', 'get-url', '--push', 'origin'))) {
        $remoteUrl = & git @remoteArgs
        if ($LASTEXITCODE -ne 0 -or $remoteUrl -ne $expectedRemote) {
            throw 'origin differs from the expected repository. Stop and review; remote was not changed.'
        }
    }
    Invoke-CheckedGit -GitArguments @('status', '--short', '--branch')
    # Check network/auth/history BEFORE staging or creating a root commit.
    $remoteRefs = @(& git ls-remote origin)
    if ($LASTEXITCODE -ne 0) {
        throw 'Cannot read GitHub. Check network and sign in through Git Credential Manager browser prompt, then rerun. Never paste tokens into chat or URLs.'
    }
    & git rev-parse --verify --quiet HEAD 2>$null | Out-Null
    $hasHead = $LASTEXITCODE -eq 0
    if ($remoteRefs.Count -gt 0) {
        if (-not $hasHead) {
            throw 'Remote has history but local main has no commits. Stop: inspect/import remote history without overwriting local files before proceeding.'
        }
        $remoteMain = @($remoteRefs | Where-Object { $_ -match '\srefs/heads/main$' })
        if ($remoteMain.Count -ne 1) {
            throw 'Remote has history without a unique main branch. Stop and inspect its default branch.'
        }
        Invoke-CheckedGit -GitArguments @('fetch', 'origin', 'main')
        & git merge-base HEAD origin/main 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'Unrelated histories. Stop; no force push or merge of unrelated histories.' }
        & git merge-base --is-ancestor origin/main HEAD
        if ($LASTEXITCODE -ne 0) {
            throw 'Local main is behind or diverged. Preserve local work and synchronize/review before rerunning; no automatic merge was attempted.'
        }
    }

    $ignoredTracked = @(& git ls-files -ci --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect tracked generated files.' }
    if ($ignoredTracked.Count -gt 0) {
        Invoke-CheckedGit -GitArguments (@('rm', '--cached', '--') + $ignoredTracked)
    }
    $paths = @('.gitignore', 'README.md', 'Assets', 'Packages', 'ProjectSettings', 'Docs', 'Tools', '.github', '.vscode', '.vsconfig', 'ignore.conf', 'output/imagegen')
    $existingPaths = @($paths | Where-Object { Test-Path -LiteralPath $_ })
    Invoke-CheckedGit -GitArguments (@('add', '--') + $existingPaths)
    $indexEntries = @(& git -c core.quotepath=false ls-files --stage)
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect staged files.' }
    $indexedObjects = @{}
    foreach ($entry in $indexEntries) {
        if ($entry -notmatch '^\d+ ([a-f0-9]+) 0\t(.+)$') {
            throw 'Unmerged or unsupported index entry. Review index before publishing.'
        }
        $indexedObjects[$Matches[1]] = $Matches[2]
    }
    $objectSizes = @($indexedObjects.Keys | & git cat-file '--batch-check=%(objectname) %(objectsize)')
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect indexed object sizes.' }
    foreach ($objectSize in $objectSizes) {
        $parts = $objectSize -split ' '
        if ($parts.Count -ne 2 -or $parts[1] -notmatch '^\d+$') { throw 'Unexpected object size response.' }
        if ([long]$parts[1] -ge 50MB) {
            throw "Indexed file >= 50 MiB: $($indexedObjects[$parts[0]]). Review Git LFS before committing. Files remain on disk."
        }
    }
    # Report filenames only, never secret values. Scan the INDEX that will be committed.
    $secretPattern = '(gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,}|AKIA[0-9A-Z]{16}|-----BEGIN (RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----|sk-[A-Za-z0-9_-]{30,}|https://[^/[:space:]:@]+:[^/[:space:]@]+@)'
    $secretFiles = @(& git grep --cached -I -l -E -e $secretPattern -- .)
    if ($LASTEXITCODE -gt 1) { throw 'Secret scan could not complete.' }
    if ($secretFiles.Count -gt 0) {
        Write-Output $secretFiles
        throw 'Potential credentials in indexed files. Review/remove before committing. No secret values printed.'
    }
    Invoke-CheckedGit -GitArguments @('diff', '--cached', '--shortstat')
    & git diff --cached --quiet
    $diffExit = $LASTEXITCODE
    if ($diffExit -gt 1) { throw 'Cannot inspect staged changes.' }
    if ($diffExit -eq 1) {
        Invoke-CheckedGit -GitArguments @('commit', '-m', 'chore: publish PolishCow Unity base')
    }
    # Normal push rejects a remote change that happened after the initial check.
    Invoke-CheckedGit -GitArguments @('push', '-u', 'origin', 'main')
    $localHead = & git rev-parse HEAD
    $published = @(& git ls-remote --heads origin main)
    if ($LASTEXITCODE -ne 0 -or $published.Count -ne 1 -or ($published[0] -split '\s+')[0] -ne $localHead) {
        throw 'Push returned but remote verification failed. Inspect main before retrying.'
    }
    Write-Output "Published main at $localHead"
    Write-Output $expectedRemote
}
finally {
    Pop-Location
}
