# Replaces the bundled Public Suffix List and its upstream test fixture with a reviewed revision.
# Runs on Windows PowerShell 5.1 and PowerShell 7.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{40}$')][string]$Revision,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{64}$')][string]$ListSha256,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-fA-F]{64}$')][string]$FixtureSha256
)
$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1 may default to TLS 1.0/1.1, which GitHub refuses.
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$Revision = $Revision.ToLowerInvariant()

# raw.githubusercontent.com serves any commit in the fork network, including unmerged pull
# requests, so the revision must be proven to be on upstream main before anything is used.
$compare = Invoke-RestMethod -UseBasicParsing -Uri "https://api.github.com/repos/publicsuffix/list/compare/main...$Revision" `
    -Headers @{ Accept = 'application/vnd.github+json'; 'User-Agent' = 'Broiler.Net-psl-updater' }
if ($compare.status -notin 'behind', 'identical') {
    throw "Revision $Revision is not on publicsuffix/list main (compare status '$($compare.status)')."
}

$listTemp = [IO.Path]::GetTempFileName()
$fixtureTemp = [IO.Path]::GetTempFileName()
try {
    $baseUrl = "https://raw.githubusercontent.com/publicsuffix/list/$Revision"
    Invoke-WebRequest -UseBasicParsing "$baseUrl/public_suffix_list.dat" -OutFile $listTemp
    Invoke-WebRequest -UseBasicParsing "$baseUrl/tests/test_psl.txt" -OutFile $fixtureTemp
    if ((Get-FileHash -LiteralPath $listTemp -Algorithm SHA256).Hash -ine $ListSha256) { throw 'PSL SHA-256 mismatch.' }
    if ((Get-FileHash -LiteralPath $fixtureTemp -Algorithm SHA256).Hash -ine $FixtureSha256) { throw 'Fixture SHA-256 mismatch.' }
    if (!(Select-String -LiteralPath $listTemp -Pattern '^// ===BEGIN PRIVATE DOMAINS===$' -Quiet)) { throw 'PSL private section missing.' }
    # The test suite compares its parsed case count with this value to detect fixture format drift.
    $caseCount = @(Select-String -LiteralPath $fixtureTemp -Pattern '^checkPublicSuffix\(').Count
    if ($caseCount -eq 0) { throw 'PSL fixture has no cases.' }
    Copy-Item -LiteralPath $listTemp -Destination (Join-Path $repositoryRoot 'src/Broiler.Net/Data/public_suffix_list.dat')
    Copy-Item -LiteralPath $fixtureTemp -Destination (Join-Path $repositoryRoot 'tests/Broiler.Net.Tests/Fixtures/test_psl.txt')
    $json = [ordered]@{
        repository = 'https://github.com/publicsuffix/list'
        revision = $Revision
        sha256 = $ListSha256.ToLowerInvariant()
        fixtureSha256 = $FixtureSha256.ToLowerInvariant()
        fixtureCaseCount = $caseCount
        license = 'MPL-2.0 (list); CC0-1.0 (upstream test fixture)'
        includesPrivateDomains = $true
    } | ConvertTo-Json
    # UTF-8 without a BOM and with LF line endings on every PowerShell edition.
    [IO.File]::WriteAllText((Join-Path $repositoryRoot 'src/Broiler.Net/Data/public-suffix-list.json'),
        ($json -replace "`r`n", "`n") + "`n", (New-Object Text.UTF8Encoding $false))
    Write-Output "PSL and fixture updated to $Revision ($caseCount fixture cases). Run the full test suite and review all three files before release."
}
finally {
    Remove-Item -LiteralPath $listTemp, $fixtureTemp -ErrorAction SilentlyContinue
}
