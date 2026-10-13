# Writes the public API surface of EsWeaponsLib.dll as sorted text, so breaking changes show up as a diff.
# Usage: powershell -File tools/api-surface.ps1 [-Dll path] [-Out path]
#   default output: api-surface.txt at the repository root (commit it; review its diff on every change to Api/ or Modules/).
param(
  [string]$Dll = (Join-Path $PSScriptRoot "..\EsWeaponsLib\EsWeaponsLib.dll"),
  [string]$Out = (Join-Path $PSScriptRoot "..\api-surface.txt"),
  [string]$Cecil = (Join-Path $PSScriptRoot "..\..\..\Managed\Mono.Cecil.dll")
)
Add-Type -Path $Cecil

function Is-Visible($t) {
  while ($t -ne $null) {
    if (-not ($t.IsPublic -or $t.IsNestedPublic -or $t.IsNestedFamily -or $t.IsNestedFamilyOrAssembly)) { return $false }
    $t = $t.DeclaringType
  }
  return $true
}
function Is-VisibleMember($m) { return $m.IsPublic -or $m.IsFamily -or $m.IsFamilyOrAssembly }

$lines = New-Object System.Collections.Generic.List[string]
function Add-Type-Surface($t) {
  if (-not (Is-Visible $t)) { return }
  $kind = if ($t.IsInterface) { "interface" } elseif ($t.IsEnum) { "enum" } elseif ($t.IsValueType) { "struct" } else { "class" }
  $base = if ($t.BaseType -ne $null) { " : " + $t.BaseType.FullName } else { "" }
  $lines.Add("$kind $($t.FullName)$base")
  foreach ($a in $t.CustomAttributes) {
    if ($a.AttributeType.Name -in "XmlRootAttribute","ObsoleteAttribute") {
      $args = ($a.ConstructorArguments | ForEach-Object { $_.Value }) -join ","
      $lines.Add("$($t.FullName) [$($a.AttributeType.Name)($args)]")
    }
  }
  foreach ($f in $t.Fields) {
    if ((Is-VisibleMember $f) -and -not $f.IsSpecialName) {
      $xml = ($f.CustomAttributes | Where-Object { $_.AttributeType.Name -in "XmlElementAttribute","XmlArrayAttribute","XmlAttributeAttribute","DefaultValueAttribute","RequireToValidateAttribute","ReloadableAttribute" } | ForEach-Object { "$($_.AttributeType.Name -replace 'Attribute$','')(" + (($_.ConstructorArguments | ForEach-Object { $_.Value }) -join ",") + ")" }) -join " "
      $lines.Add("$($t.FullName)::$($f.Name) : $($f.FieldType.FullName) $xml".TrimEnd())
    }
  }
  foreach ($m in $t.Methods) {
    if ((Is-VisibleMember $m) -and -not ($m.IsGetter -or $m.IsSetter -or $m.IsAddOn -or $m.IsRemoveOn)) {
      $ps = ($m.Parameters | ForEach-Object { $_.ParameterType.FullName }) -join ", "
      $mod = if ($m.IsStatic) { "static " } elseif ($m.IsAbstract) { "abstract " } elseif ($m.IsVirtual) { "virtual " } else { "" }
      $lines.Add("$($t.FullName)::$mod$($m.ReturnType.FullName) $($m.Name)($ps)")
    }
  }
  foreach ($pr in $t.Properties) {
    $g = $pr.GetMethod; $s = $pr.SetMethod
    $acc = @(); if ($g -and (Is-VisibleMember $g)) { $acc += "get" }; if ($s -and (Is-VisibleMember $s)) { $acc += "set" }
    if ($acc.Count -gt 0) { $lines.Add("$($t.FullName)::$($pr.Name) : $($pr.PropertyType.FullName) {$($acc -join ';')}") }
  }
  foreach ($e in $t.Events) {
    if ($e.AddMethod -and (Is-VisibleMember $e.AddMethod)) { $lines.Add("$($t.FullName)::event $($e.EventType.FullName) $($e.Name)") }
  }
  foreach ($n in $t.NestedTypes) { Add-Type-Surface $n }
}

$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Dll)
$lines.Add("assembly $($asm.Name.Name) $($asm.Name.Version)")
foreach ($mod in $asm.Modules) { foreach ($t in $mod.Types) { Add-Type-Surface $t } }
$sorted = $lines | Sort-Object -Unique
[IO.File]::WriteAllLines($Out, [string[]]$sorted, (New-Object Text.UTF8Encoding $false))
"API surface written: $Out ($($sorted.Count) lines)"
