$ErrorActionPreference = 'Stop'
$assetRootUrl = 'https://michelclauwaertgmailcom.itch.io/farm-ranch-life-free-16x16-top-down-pixel-art-asset-pack'
$assetTemp = Join-Path (Split-Path -Parent $PSScriptRoot) 'Temp/AssetResearch'
[IO.Directory]::CreateDirectory($assetTemp) | Out-Null
$assetSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$page = Invoke-WebRequest -UseBasicParsing -Uri $assetRootUrl -WebSession $assetSession
function Get-AssetCsrf([string]$html) {
    foreach ($pattern in @('"csrf_token"\s*:\s*"([^"]+)"', 'name="csrf_token"\s+value="([^"]+)"', 'I\.csrf_token\s*=\s*"([^"]+)"')) {
        $found = [regex]::Match($html,$pattern)
        if ($found.Success) { return [Net.WebUtility]::HtmlDecode($found.Groups[1].Value) }
    }
    return $null
}
$csrf = Get-AssetCsrf $page.Content
if (-not $csrf) { throw 'Asset page did not provide a CSRF token.' }
$downloadRequest = Invoke-WebRequest -UseBasicParsing -Method Post -Uri ($assetRootUrl + '/download_url') -WebSession $assetSession -Body @{ csrf_token = $csrf }
$downloadData = $downloadRequest.Content | ConvertFrom-Json
if (-not $downloadData.url) { throw 'No free download page was returned.' }
$downloadPage = Invoke-WebRequest -UseBasicParsing -Uri $downloadData.url -WebSession $assetSession
[IO.File]::WriteAllText((Join-Path $assetTemp 'download-page.html'),$downloadPage.Content)
$upload = [regex]::Match($downloadPage.Content,'data-upload_id="(\d+)"')
if (-not $upload.Success) { throw 'No downloadable upload was listed.' }
$csrf = Get-AssetCsrf $downloadPage.Content
$body = @{ csrf_token = $csrf }
$key = [regex]::Match($downloadPage.Content,'"download_key"\s*:\s*"([^"]+)"|"key"\s*:\s*"([^"]+)"')
if ($key.Success) { $body.key = if ($key.Groups[1].Success) { $key.Groups[1].Value } else { $key.Groups[2].Value } }
$fileRequest = Invoke-WebRequest -UseBasicParsing -Method Post -Uri ($assetRootUrl + '/file/' + $upload.Groups[1].Value) -WebSession $assetSession -Body $body
$fileData = $fileRequest.Content | ConvertFrom-Json
if (-not $fileData.url) { throw 'No file URL was returned.' }
$zipPath = Join-Path $assetTemp 'FarmRanchLife.zip'
Invoke-WebRequest -UseBasicParsing -Uri $fileData.url -OutFile $zipPath
Write-Output ('Downloaded free asset pack: ' + $zipPath)
