# .\update-submodules.ps1 "e"
param(
    [string]$Message = "Update submodule"
)

$env:GIT_TERMINAL_PROMPT = "0"

$root = (git rev-parse --show-toplevel).Trim()
$parentName = (git -C $root config user.name)
$parentEmail = (git -C $root config user.email)

$paths = @(git submodule foreach --recursive --quiet 'echo $displaypath')
[array]::Reverse($paths)

foreach ($rel in $paths) {
    $dir = Join-Path $root $rel
    Write-Host "== $rel ==" -ForegroundColor Cyan
    Push-Location $dir

    $name = git config user.name
    if (-not $name) { $name = $parentName }
    $email = git config user.email
    if (-not $email) { $email = $parentEmail }

    if (-not $name -or -not $email) {
        Write-Host "No git user.name/user.email found, skipping $rel" -ForegroundColor Red
        Pop-Location
        continue
    }

    $branch = (git symbolic-ref --short -q HEAD)
    if (-not $branch) {
        $head = (git rev-parse --abbrev-ref origin/HEAD 2>$null)
        $branch = if ($head) { $head -replace '^origin/', '' } else { "main" }
        git checkout $branch
    }

    git add -A
    git diff --cached --quiet
    if ($LASTEXITCODE -ne 0) {
        git -c user.name="$name" -c user.email="$email" commit -m $Message
    }

    git pull --rebase origin $branch
    if ($LASTEXITCODE -eq 0) {
        git push origin $branch
    } else {
        Write-Host "Pull failed in $rel, skipping push" -ForegroundColor Red
    }

    Pop-Location
}

Set-Location $root

$topLevel = @(git submodule --quiet foreach 'echo $sm_path')
foreach ($rel in $topLevel) { git add $rel }

git diff --cached --quiet
if ($LASTEXITCODE -ne 0) {
    git -c user.name="$parentName" -c user.email="$parentEmail" commit -m $Message
}
git push