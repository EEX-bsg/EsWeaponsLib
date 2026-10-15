# Serves docs/ at http://localhost:4000 the way GitHub Pages builds it (github-pages gem in the official ruby Docker image).
# Needs Docker Desktop running. The first run downloads the image and gems (cached in the Docker volume "ewl-docs-gems").
# Edits under docs/ are picked up automatically (reload the browser). Stop with Ctrl+C, or: docker stop ewl-docs-preview
# Usage: powershell -File tools/preview-docs.ps1 [-Port 4000]
param([int]$Port = 4000)
$root = Split-Path $PSScriptRoot -Parent
docker run --rm --name ewl-docs-preview -p "${Port}:4000" `
  -v "$root\docs:/site" -v "$PSScriptRoot\docs-preview:/preview" -v "ewl-docs-gems:/usr/local/bundle" `
  -e PAGES_REPO_NWO=EEX-bsg/EsWeaponsLib -e JEKYLL_ENV=production -w /preview ruby:3.3 `
  bash -c "bundle install --quiet && bundle exec jekyll serve --source /site --destination /tmp/site --host 0.0.0.0 --force_polling --baseurl ''"
