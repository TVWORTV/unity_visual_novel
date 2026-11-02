# .\update-git.ps1

param(
    [string]$Message = "Update full module"
)

$env:GIT_TERMINAL_PROMPT = "0"

$root = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Set-Location $root

$parentName = git config user.name
$parentEmail = git config user.email
if (-not $parentName -or -not $parentEmail) {
    Write-Host "No git user.name/user.email configured." -ForegroundColor Red
    exit 1
}

& "$PSScriptRoot\update-submodules.ps1" $Message
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git add -A
git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    git -c user.name="$parentName" -c user.email="$parentEmail" commit -m $Message
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

git push
exit $LASTEXITCODE