```powershell
# .\update-submodules.ps1

param(
    [string]$Message = "Update submodule"
)

$env:GIT_TERMINAL_PROMPT = "0"

$root = (git rev-parse --show-toplevel).Trim()
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$parentName = git -C $root config user.name
$parentEmail = git -C $root config user.email

if (-not $parentName -or -not $parentEmail) {
    Write-Host "No git user.name/user.email configured in parent repository." -ForegroundColor Red
    exit 1
}

$paths = @(git -C $root submodule foreach --recursive --quiet 'echo $displaypath')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

[array]::Reverse($paths)

foreach ($rel in $paths) {
    $dir = Join-Path $root $rel

    if (-not (Test-Path (Join-Path $dir ".git"))) {
        Write-Host "Skipping uninitialized submodule: $rel" -ForegroundColor Yellow
        continue
    }

    Write-Host "== $rel ==" -ForegroundColor Cyan
    Push-Location $dir

    try {
        $name = git config user.name
        if (-not $name) { $name = $parentName }

        $email = git config user.email
        if (-not $email) { $email = $parentEmail }

        $remote = git remote get-url origin
        if ($LASTEXITCODE -ne 0 -or -not $remote) {
            Write-Host "No origin remote found in $rel" -ForegroundColor Red
            continue
        }

        # Detect the current branch.
        $branch = git symbolic-ref --short -q HEAD

        # Recover from detached HEAD by identifying the remote default branch.
        if (-not $branch) {
            $remoteHead = git symbolic-ref --quiet --short refs/remotes/origin/HEAD

            if ($remoteHead) {
                $branch = $remoteHead -replace '^origin/', ''
            } else {
                $remoteHead = git ls-remote --symref origin HEAD |
                    Where-Object { $_ -match '^ref: refs/heads/([^ ]+) HEAD$' }

                if ($remoteHead -match '^ref: refs/heads/([^ ]+) HEAD$') {
                    $branch = $Matches[1]
                }
            }

            if (-not $branch) {
                Write-Host "Could not determine default branch for $rel" -ForegroundColor Red
                continue
            }

            # Fetch the selected branch before switching.
            git fetch origin $branch
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Failed to fetch origin/$branch in $rel" -ForegroundColor Red
                continue
            }

            # Switch to the existing local branch, or create a tracking branch.
            git switch $branch 2>$null

            if ($LASTEXITCODE -ne 0) {
                git switch --track -c $branch "origin/$branch"
            }

            if ($LASTEXITCODE -ne 0) {
                Write-Host "Could not switch to branch $branch in $rel" -ForegroundColor Red
                continue
            }
        }

        # Stage and commit only after selecting a branch.
        git add -A
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to stage changes in $rel" -ForegroundColor Red
            continue
        }

        git diff --cached --quiet
        $diffExit = $LASTEXITCODE

        if ($diffExit -gt 1) {
            Write-Host "Failed to check staged changes in $rel" -ForegroundColor Red
            continue
        }

        if ($diffExit -eq 1) {
            git -c user.name="$name" -c user.email="$email" commit -m $Message

            if ($LASTEXITCODE -ne 0) {
                Write-Host "Commit failed in $rel" -ForegroundColor Red
                continue
            }
        }

        # Update from the remote before pushing.
        git pull --rebase origin $branch

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Pull/rebase failed in $rel. Resolve conflicts before continuing." -ForegroundColor Red
            continue
        }

        # Push explicitly to the selected branch.
        git push origin "HEAD:refs/heads/$branch"

        if ($LASTEXITCODE -ne 0) {
            Write-Host "Push failed in $rel" -ForegroundColor Red
            continue
        }

        Write-Host "Updated successfully: $rel ($branch)" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}

Set-Location $root

# Update top-level submodule references.
$topLevel = @(git submodule --quiet foreach 'echo $sm_path')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

foreach ($rel in $topLevel) {
    git add -- $rel
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to stage submodule reference: $rel" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

git diff --cached --quiet
$diffExit = $LASTEXITCODE

if ($diffExit -gt 1) { exit $diffExit }

if ($diffExit -eq 1) {
    git -c user.name="$parentName" -c user.email="$parentEmail" commit -m $Message
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

git push
exit $LASTEXITCODE
```