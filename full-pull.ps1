# .\full-pull.ps1

$ErrorActionPreference = "Stop"

git fetch origin
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git reset --hard origin/main
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git submodule update --init --recursive --force
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

git clean -fd
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }