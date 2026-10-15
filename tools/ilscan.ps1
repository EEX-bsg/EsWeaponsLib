# Copied from BesiegeDevBridge Project/tools/ilscan.ps1 (default DLL path changed for EsWeaponsLib).
# Mirrors the API check that Besiege 1.9.1-25490 applies when it loads mod assemblies (as observed: a mod DLL using these is rejected).
# Usage: powershell -File tools/ilscan.ps1 [-Dll path] ; exit 1 and a list of violations when rejected.
param([string]$Dll = (Join-Path $PSScriptRoot "..\EsWeaponsLib\EsWeaponsLib.dll"), [string]$Cecil = (Join-Path $PSScriptRoot "..\..\..\Managed\Mono.Cecil.dll"))
Add-Type -Path $Cecil
$ns = "System.IO","System.Net","System.Xml","System.Reflection","System.Runtime.InteropServices","System.Diagnostics","System.Security","Mono.CSharp","Mono.Cecil","System.CodeDom.Compiler","CSharpCompiler","IKVM","Microsoft","Mono.CompilerServices","UnityEngine.WWW","UnityEngine.MasterServer","ModIO","PlayFab","Steamworks","GameGrind","InternalModding","BesiegeDlc"
$methods = "XmlSaver.Save","LevelXMLSaver.Create","UnityEngine.AssetBundle.LoadFromFile","UnityEngine.AssetBundle.LoadFromFileAsync","Application.OpenURL","Application.Quit","Application.RequestUserAuthorization"
$white = "System.IO.Stream","System.IO.TextWriter","System.IO.TextReader","System.IO.BinaryWriter","System.IO.BinaryReader","System.IO.MemoryStream","System.IO.Path","System.IO.SeekOrigin","System.Diagnostics.Stopwatch","System.Security.Cryptography","Mono.CSharp.Tuple``2","Mono.CSharp.Tuple``3"
function Bad-Type($t) {
  if ($t -eq $null) { return $false }
  $full = $t.Namespace + "." + $t.Name
  foreach ($n in $ns) { if ($full.StartsWith($n)) { return -not ($white -contains $full) } }
  return $false
}
function Bad-Method($m) {
  if (Bad-Type $m.DeclaringType) { return $true }
  $full = $m.DeclaringType.Namespace + "." + $m.DeclaringType.Name + "." + $m.Name
  if ($m.DeclaringType.Namespace -eq "") { $full = "." + $m.DeclaringType.Name + "." + $m.Name }
  return ($methods -contains $full)
}
$problems = New-Object System.Collections.Generic.List[string]
function Scan-Type($type) {
  foreach ($f in $type.Fields) { if (Bad-Type $f.FieldType) { $problems.Add("field $($type.FullName)::$($f.Name) : $($f.FieldType.FullName)") } }
  foreach ($m in $type.Methods) {
    if ($m.HasPInvokeInfo) { $problems.Add("pinvoke $($m.FullName)") }
    if (-not $m.HasBody) { continue }
    foreach ($v in $m.Body.Variables) { if (Bad-Type $v.VariableType) { $problems.Add("local in $($m.FullName): $($v.VariableType.FullName)") } }
    foreach ($ins in $m.Body.Instructions) {
      $op = $ins.OpCode.Name
      if ($op -eq "ldfld") { if ((Bad-Type $ins.Operand.DeclaringType) -or (Bad-Type $ins.Operand.FieldType)) { $problems.Add("ldfld in $($m.FullName): $($ins.Operand.FullName)") } }
      elseif ($op -in "call","callvirt","calli","newobj") {
        if (Bad-Method $ins.Operand) { $problems.Add("call in $($m.FullName): $($ins.Operand.FullName)") }
        if ($ins.Operand -is [Mono.Cecil.GenericInstanceMethod]) { foreach ($g in $ins.Operand.GenericArguments) { if (Bad-Type $g) { $problems.Add("generic arg in $($m.FullName): $($g.FullName)") } } }
      }
      elseif ($op -eq "newarr") { if (Bad-Type $ins.Operand) { $problems.Add("newarr in $($m.FullName): $($ins.Operand.FullName)") } }
      elseif ($op -eq "ldtoken") {
        $o = $ins.Operand
        if ($o -is [Mono.Cecil.TypeReference]) { if (Bad-Type $o) { $problems.Add("ldtoken in $($m.FullName): $($o.FullName)") } }
        elseif ($o -is [Mono.Cecil.MemberReference]) { if (Bad-Type $o.DeclaringType) { $problems.Add("ldtoken in $($m.FullName): $($o.FullName)") } }
      }
    }
  }
  foreach ($n in $type.NestedTypes) { Scan-Type $n }
}
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Dll)
foreach ($mod in $asm.Modules) { foreach ($t in $mod.Types) { Scan-Type $t } }
if ($problems.Count -eq 0) { "IL scan OK: $Dll" } else { $problems | Sort-Object -Unique; exit 1 }
