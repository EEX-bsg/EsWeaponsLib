# Fails if files that would be published (tracked + untracked, not ignored) contain things that must not be public:
# references to private material, decompiled sources, or other mods' source code. Extend $patterns when a new kind of leak is found.
# Usage: powershell -File tools/check-public.ps1 ; prints "check-public OK" or the offending lines and exits 1.
$root = Split-Path $PSScriptRoot -Parent
$patterns = @(
  '\.internal-docs',             # private documents
  'acmu-archive',                # private copies of research notes
  'rules[\\/]owner',             # private agent rules
  'decompiled[\\/]',             # decompiled game sources
  'skpCustomModule',             # another mod's source tree
  'InternalModding\.\w+\.\w+',   # internal game class paths
  '\bBCM\b'                      # private jargon; write "XML カスタムモジュール"
)
$skip = @('.gitignore', 'tools/check-public.ps1')
$files = @(git -C $root ls-files) + @(git -C $root ls-files --others --exclude-standard) | Where-Object { $skip -notcontains $_ } | Sort-Object -Unique
$found = 0
foreach ($file in $files) {
  $path = Join-Path $root $file
  if (-not (Test-Path $path -PathType Leaf) -or $file -match '\.(png|jpg|obj|dll|pdb)$') { continue }
  $lines = [IO.File]::ReadAllLines($path)
  for ($i = 0; $i -lt $lines.Length; $i++) {
    foreach ($pattern in $patterns) {
      if ($lines[$i] -match $pattern) { "${file}:$($i + 1): [$pattern] $($lines[$i].Trim())"; $found++ }
    }
  }
}
if ($found -gt 0) { "check-public: $found finding(s)"; exit 1 }
"check-public OK"
