# Generates docs/reference/xml-modules.md and docs/reference/csharp-api.md from the built DLL and its XML documentation (/// comments).
# Usage: build Release first, then: powershell -File tools/gen-reference.ps1
#   Commit the output and review its diff. Never edit the output by hand; fix the /// comments in src/ instead.
# Defaults are read from the field initializers in the constructor IL. XML elements without their own /// fall back to
# the same-named field of the matching EWL.Api type ($specOf below).
param(
  [string]$Dll = (Join-Path $PSScriptRoot "..\src\EsWeaponsLib\bin\Release\EsWeaponsLib.dll"),
  [string]$OutDir = (Join-Path $PSScriptRoot "..\docs\reference"),
  [string]$Cecil = (Join-Path $PSScriptRoot "..\..\..\Managed\Mono.Cecil.dll")
)
$ErrorActionPreference = 'Stop'
Add-Type -Path $Cecil
Add-Type -AssemblyName System.Web

$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
$resolver.AddSearchDirectory((Split-Path (Resolve-Path $Dll)))
$readerParameters = New-Object Mono.Cecil.ReaderParameters
$readerParameters.AssemblyResolver = $resolver
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Resolve-Path $Dll).Path, $readerParameters)
$types = @{}
foreach ($type in $assembly.MainModule.Types) { $types[$type.FullName] = $type }

[xml]$docXml = [IO.File]::ReadAllText([IO.Path]::ChangeExtension((Resolve-Path $Dll).Path, '.xml'))
$docs = @{}
foreach ($member in $docXml.doc.members.member) { $docs[$member.name] = $member }

# XML schema class -> EWL.Api type whose same-named fields describe it
$specOf = @{
  'EwlWeaponModule' = 'WeaponSpec'; 'ProjectileXml' = 'ProjectileSpec'; 'ExplosionXml' = 'ExplosionSpec'
  'BoosterXml' = 'BoosterSpec'; 'GuidanceXml' = 'GuidanceSpec'
}
$apiOrder = 'Weapon', 'WeaponSpec', 'ProjectileSpec', 'ExplosionSpec', 'BoosterSpec', 'GuidanceSpec', 'ColliderShape', 'ColliderKind',
  'ProjectileHit', 'HitKind', 'Attacker', 'BundleAssets'
$generatedNote = "> [!NOTE]`n> " + 'このページは生成物です(`tools/gen-reference.ps1`)。手で編集せず、`src/` の `///` コメントを直して作り直してください。'

function Convert-Doc([string]$inner, [bool]$inTable) {
  if (-not $inner) { return '' }
  $text = [regex]::Replace($inner, '<see cref="(\w):([^"]+)"\s*/>', [System.Text.RegularExpressions.MatchEvaluator] {
    param($match)
    $parts = ($match.Groups[2].Value -replace '\(.*$', '' -replace '``\d+', '').Split('.')
    if ($match.Groups[1].Value -eq 'T') { return '`' + $parts[-1] + '`' }
    return '`' + $parts[-2] + '.' + $parts[-1] + '`'
  })
  $text = $text -replace '<c>(.*?)</c>', '`$1`'
  $text = $text -replace '\s*\r?\n\s*', ''
  $breakMark = if ($inTable) { '<br>' } else { "`n`n" }
  $text = $text -replace '\s*<para>\s*', $breakMark -replace '\s*</para>\s*', ''
  $text = [System.Web.HttpUtility]::HtmlDecode($text).Trim()
  # 説明文に Markdown のリンクは無い。"[m/s](…)" のような単位の後の括弧がリンクにならないよう [ をエスケープする
  $text = $text -replace '\[', '\['
  if ($inTable) { $text = $text -replace '\|', '\|' }
  return $text
}

function Get-Doc([string]$key, [bool]$inTable = $true) {
  $member = $docs[$key]
  if ($member -eq $null) { return '' }
  $summary = $member.SelectSingleNode('summary')
  if ($summary -eq $null) { return '' }
  return Convert-Doc $summary.InnerXml $inTable
}

function Get-MethodDoc($type, $method) {
  $pattern = '^M:' + [regex]::Escape($type.FullName + '.' + $method.Name) + '(``\d+)?(\(|$)'
  foreach ($key in $docs.Keys) { if ($key -match $pattern) { return Get-Doc $key } }
  return ''
}

function Get-Attribute($provider, [string]$name) {
  foreach ($attribute in $provider.CustomAttributes) { if ($attribute.AttributeType.Name -eq $name) { return $attribute } }
  return $null
}

function Format-TypeName($typeReference) {
  if ($typeReference.IsArray) { return (Format-TypeName $typeReference.ElementType) + '[]' }
  if ($typeReference.IsGenericInstance) {
    $arguments = ($typeReference.GenericArguments | ForEach-Object { Format-TypeName $_ }) -join ', '
    return ($typeReference.Name -replace '`\d+$', '') + '<' + $arguments + '>'
  }
  switch ($typeReference.FullName) {
    'System.Single' { return 'float' }  'System.Int32' { return 'int' }  'System.Boolean' { return 'bool' }
    'System.String' { return 'string' } 'System.Void' { return 'void' }  'System.UInt16' { return 'ushort' }
    'System.Object' { return 'object' }
  }
  return $typeReference.Name
}

function Get-EnumNames($typeReference) {
  $definition = $typeReference.Resolve()
  if ($definition -eq $null -or -not $definition.IsEnum) { return $null }
  $names = @{}
  foreach ($field in $definition.Fields) { if ($field.HasConstant) { $names[[string]$field.Constant] = $field.Name } }
  return $names
}

function Format-Number($value) { return $value.ToString('R', [Globalization.CultureInfo]::InvariantCulture) }

# Field initializer values, read from the parameterless constructor up to the base constructor call.
function Get-Initializers($type) {
  $values = @{}
  $constructor = $type.Methods | Where-Object { $_.IsConstructor -and -not $_.IsStatic -and $_.Parameters.Count -eq 0 } | Select-Object -First 1
  if ($constructor -eq $null -or -not $constructor.HasBody) { return $values }
  $stack = New-Object System.Collections.ArrayList
  :instructions foreach ($instruction in $constructor.Body.Instructions) {
    $code = $instruction.OpCode.Code.ToString()
    switch -regex ($code) {
      '^Ldarg_0$' { [void]$stack.Add('#this') }
      '^Ldc_R[48]$' { [void]$stack.Add((Format-Number $instruction.Operand)) }
      '^Ldc_I4_M1$' { [void]$stack.Add('-1') }
      '^Ldc_I4_(\d)$' { [void]$stack.Add($Matches[1]) }
      '^Ldc_I4(_S)?$' { [void]$stack.Add([string]$instruction.Operand) }
      '^Ldnull$' { [void]$stack.Add('#null') }
      '^Ldstr$' { [void]$stack.Add('"' + $instruction.Operand + '"') }
      '^Call$' {
        $method = $instruction.Operand
        if ($method.Name -eq '.ctor') { break instructions }
        if ($method.Name -like 'get_*') { [void]$stack.Add('#' + $method.Name.Substring(4)) }
      }
      '^Newobj$' {
        $method = $instruction.Operand
        $arguments = @()
        for ($i = 0; $i -lt $method.Parameters.Count; $i++) {
          $arguments = @($stack[$stack.Count - 1]) + $arguments
          $stack.RemoveAt($stack.Count - 1)
        }
        $declaring = $method.DeclaringType
        if ($declaring.Name -eq 'Vector3') { [void]$stack.Add('(' + ($arguments -join ', ') + ')') }
        elseif ($declaring.Name -like 'List*') { [void]$stack.Add('#empty') }
        else { [void]$stack.Add('#new') }
      }
      '^Stfld$' {
        $values[$instruction.Operand.Name] = $stack[$stack.Count - 1]
        $stack.RemoveRange($stack.Count - 2, 2)
      }
    }
  }
  return $values
}

function Format-Default($field, $raw) {
  $fieldType = $field.FieldType
  $enumNames = Get-EnumNames $fieldType
  if ($raw -eq $null) {
    if ($enumNames) { return '`' + $enumNames['0'] + '`' }
    switch ($fieldType.FullName) { 'System.Single' { return '`0`' } 'System.Int32' { return '`0`' } 'System.Boolean' { return '`false`' } }
    return 'なし'
  }
  if ($enumNames -and $enumNames.ContainsKey([string]$raw)) { return '`' + $enumNames[[string]$raw] + '`' }
  if ($fieldType.FullName -eq 'System.Boolean') { if ($raw -eq '0') { return '`false`' } else { return '`true`' } }
  switch ($raw) { '#zero' { return '`(0, 0, 0)`' } '#one' { return '`(1, 1, 1)`' } '#empty' { return '空' } '#null' { return 'なし' } '#new' { return '—' } }
  return '`' + $raw + '`'
}

function Get-Anchor([string]$heading) { return ($heading.ToLowerInvariant() -replace '[^\w\- ]', '' -replace ' ', '-') }

function Test-Element($type) {
  $current = $type
  while ($current -ne $null) {
    if ($current.FullName -eq 'Modding.Serialization.Element') { return $true }
    if ($current.BaseType -eq $null) { return $false }
    $current = $current.BaseType.Resolve()
  }
  return $false
}

# ---------------------------------------------------------------- XML modules

$xmlLines = New-Object System.Collections.Generic.List[string]

function Get-XmlShape($field) {
  $typeName = $field.FieldType.FullName
  $arrayAttribute = Get-Attribute $field 'XmlArrayAttribute'
  if ($arrayAttribute -ne $null) {
    $items = @($field.CustomAttributes | Where-Object { $_.AttributeType.Name -eq 'XmlArrayItemAttribute' } | ForEach-Object { $_.ConstructorArguments[0].Value })
    if ($typeName -match 'ResourceReference') { return '`<' + $items[0] + ' />` の並び' }
    return (($items | ForEach-Object { '`<' + $_ + '>`' }) -join ' / ') + ' の並び'
  }
  $definition = $field.FieldType.Resolve()
  if ($definition -ne $null -and $definition.IsEnum) {
    return (($definition.Fields | Where-Object { $_.HasConstant } | ForEach-Object { '`' + $_.Name + '`' }) -join ' / ')
  }
  switch ($typeName) {
    'System.Single' { return '数値' }
    'System.Int32' { return '整数' }
    'System.Boolean' { return '`true` / `false`' }
    'System.String' { return '文字列' }
    'Modding.Serialization.Vector3' { return 'ベクトル' }
    'Modding.Serialization.MKeyReference' { return 'キー参照' }
    'Modding.Serialization.MToggleReference' { return 'トグル参照' }
    'Modding.Serialization.MSliderReference' { return 'スライダー参照' }
    'Modding.Serialization.ResourceReference' { return 'リソース名' }
    'Modding.Serialization.MeshReference' { return 'メッシュ' }
  }
  return Format-TypeName $field.FieldType
}

function Write-XmlSection($type, [string]$path, [bool]$parentReloadable) {
  $xmlLines.Add('')
  $xmlLines.Add("## $path")
  $xmlLines.Add('')
  $summary = Get-Doc ('T:' + $type.FullName) $false
  if (-not $summary -and $specOf.ContainsKey($type.Name)) { $summary = Get-Doc ('T:EWL.Api.' + $specOf[$type.Name]) $false }
  if ($specOf.ContainsKey($type.Name)) { $summary = ($summary + "`n`nC# では ``" + $specOf[$type.Name] + '` に当たります。').Trim() }
  if ($summary) { $xmlLines.Add($summary); $xmlLines.Add('') }
  $xmlLines.Add('| 要素 | 書き方 | 既定値 | 再読込 | 説明 |')
  $xmlLines.Add('|---|---|---|---|---|')
  $initializers = Get-Initializers $type
  $children = @()
  foreach ($field in $type.Fields) {
    if (-not $field.IsPublic -or $field.IsStatic) { continue }
    $elementAttribute = Get-Attribute $field 'XmlElementAttribute'
    $arrayAttribute = Get-Attribute $field 'XmlArrayAttribute'
    if ($elementAttribute -eq $null -and $arrayAttribute -eq $null) { continue }
    $name = $field.Name
    if ($arrayAttribute -ne $null -and $arrayAttribute.ConstructorArguments.Count -gt 0) { $name = $arrayAttribute.ConstructorArguments[0].Value }
    elseif ($elementAttribute -ne $null -and $elementAttribute.ConstructorArguments.Count -gt 0) { $name = $elementAttribute.ConstructorArguments[0].Value }
    $required = (Get-Attribute $field 'RequireToValidateAttribute') -ne $null
    $reloadable = $parentReloadable -or ((Get-Attribute $field 'ReloadableAttribute') -ne $null)
    $description = Get-Doc ('F:' + $type.FullName + '.' + $field.Name)
    if (-not $description -and $specOf.ContainsKey($type.Name)) { $description = Get-Doc ('F:EWL.Api.' + $specOf[$type.Name] + '.' + $field.Name) }
    $fieldType = $field.FieldType.Resolve()
    if ($arrayAttribute -eq $null -and $fieldType -ne $null -and $types.ContainsKey($fieldType.FullName) -and (Test-Element $fieldType)) {
      $childPath = "$path/$name"
      $shape = '子要素([' + $childPath + '](#' + (Get-Anchor $childPath) + '))'
      $children += , @($fieldType, $childPath, $reloadable)
      $default = '—'
    } else {
      $shape = Get-XmlShape $field
      $default = Format-Default $field $initializers[$field.Name]
    }
    $nameCell = '`' + $name + '`'
    if ($required) { $nameCell += ' **必須**'; $default = '—' }
    $reloadCell = if ($reloadable) { '○' } else { '' }
    $xmlLines.Add("| $nameCell | $shape | $default | $reloadCell | $description |")
  }
  foreach ($child in $children) { Write-XmlSection $child[0] $child[1] $child[2] }
}

$xmlLines.Add('# XML カスタムモジュール リファレンス')
$xmlLines.Add('')
$xmlLines.Add($generatedNote)
$xmlLines.Add('')
$xmlLines.Add('EWL の XML カスタムモジュールの全要素です。使い方は [XML の使い方](../guide/xml-recipes.md) を参照してください。')
$xmlLines.Add('')
$xmlLines.Add('- ブロック XML の `<Modules>` に、`modid="0d219960-b202-4906-b29d-8d90a61a89b8"`(EWL の Mod ID)を付けて書きます。')
$xmlLines.Add('- **必須** と書いた要素以外は省略でき、省略した要素は既定値になります。単位は C# 側と同じです(`[m/s]`・`[s]`・`[度]`)。')
$xmlLines.Add('- 位置・回転はブロックローカル、回転はオイラー角 [度] です。')
$xmlLines.Add('- 「書き方」の見方: ベクトル = `<要素名 x="0" y="0" z="1" />`、キー参照・トグル参照・スライダー参照 = `<要素名 key="…" />`(`key` はブロックの `ModuleMapperTypes` に定義した名前)、リソース名 = `<要素名 name="…" />`(ブロックを定義した Mod の `Resources` の名前)、メッシュ = リソース名に `Position` / `Rotation` / `Scale` を足したもの、`<AudioClip />` の並び = `<AudioClip name="…" />` を並べる、コライダーの並び = ブロックの `Colliders` と同じ書式。')
$xmlLines.Add('- 再読込 ○: ブロックを定義した Mod の `Mod.xml` が `<Debug>true</Debug>` のとき、ゲーム中に XML を保存すると次のシミュレーション開始から反映されます。空欄の要素は Besiege の再起動が必要です。')
$moduleTypes = $types.Values | Where-Object { $_.Namespace -eq 'EWL.Modules' -and (Get-Attribute $_ 'XmlRootAttribute') -ne $null } | Sort-Object { if ($_.Name -eq 'EwlWeaponModule') { 0 } else { 1 } }
foreach ($moduleType in $moduleTypes) {
  Write-XmlSection $moduleType (Get-Attribute $moduleType 'XmlRootAttribute').ConstructorArguments[0].Value $false
}

# ---------------------------------------------------------------- C# API

$apiLines = New-Object System.Collections.Generic.List[string]
$apiTypes = @($types.Values | Where-Object { $_.Namespace -eq 'EWL.Api' -and $_.IsPublic } |
  Sort-Object @{ Expression = { $index = [array]::IndexOf($apiOrder, $_.Name); if ($index -lt 0) { 999 } else { $index } } }, Name)

$apiLines.Add('# C# API リファレンス')
$apiLines.Add('')
$apiLines.Add($generatedNote)
$apiLines.Add('')
$apiLines.Add('名前空間 `EWL.Api` の公開型です。使い方は [C# で使う](../guide/csharp.md) を参照してください。')
$apiLines.Add('')
foreach ($type in $apiTypes) {
  $line = '- [' + $type.Name + '](#' + (Get-Anchor $type.Name) + ')'
  $summary = Get-Doc ('T:' + $type.FullName)
  if ($summary) { $line += ' — ' + ($summary -split '。')[0] + '。' }
  $apiLines.Add($line)
}

foreach ($type in $apiTypes) {
  $kind = if ($type.IsEnum) { 'enum' } elseif ($type.IsAbstract -and $type.IsSealed) { 'static class' } elseif ($type.IsSealed) { 'sealed class' } else { 'class' }
  $apiLines.Add('')
  $apiLines.Add('## ' + $type.Name)
  $apiLines.Add('')
  $apiLines.Add('`public ' + $kind + ' ' + $type.FullName + '`')
  $apiLines.Add('')
  $summary = Get-Doc ('T:' + $type.FullName) $false
  if ($summary) { $apiLines.Add($summary); $apiLines.Add('') }

  if ($type.IsEnum) {
    $apiLines.Add('| 値 | 説明 |')
    $apiLines.Add('|---|---|')
    foreach ($field in $type.Fields) {
      if ($field.HasConstant) { $apiLines.Add('| `' + $field.Name + '` | ' + (Get-Doc ('F:' + $type.FullName + '.' + $field.Name)) + ' |') }
    }
    continue
  }

  $fields = @($type.Fields | Where-Object { $_.IsPublic })
  if ($fields.Count -gt 0) {
    $initializers = Get-Initializers $type
    $apiLines.Add('| フィールド | 型 | 既定値 | 説明 |')
    $apiLines.Add('|---|---|---|---|')
    foreach ($field in $fields) {
      $apiLines.Add('| `' + $field.Name + '` | `' + (Format-TypeName $field.FieldType) + '` | ' + (Format-Default $field $initializers[$field.Name]) + ' | ' + (Get-Doc ('F:' + $type.FullName + '.' + $field.Name)) + ' |')
    }
    $apiLines.Add('')
  }

  $members = New-Object System.Collections.Generic.List[string]
  foreach ($method in $type.Methods) {
    if (-not $method.IsPublic -or $method.IsConstructor -or $method.IsGetter -or $method.IsSetter -or $method.IsAddOn -or $method.IsRemoveOn) { continue }
    $generic = if ($method.HasGenericParameters) { '<' + (($method.GenericParameters | ForEach-Object { $_.Name }) -join ', ') + '>' } else { '' }
    $parameters = ($method.Parameters | ForEach-Object { (Format-TypeName $_.ParameterType) + ' ' + $_.Name }) -join ', '
    $static = if ($method.IsStatic) { 'static ' } else { '' }
    $members.Add('| `' + $static + (Format-TypeName $method.ReturnType) + ' ' + $method.Name + $generic + '(' + $parameters + ')` | ' + (Get-MethodDoc $type $method) + ' |')
  }
  foreach ($property in $type.Properties) {
    $accessors = @()
    if ($property.GetMethod -ne $null -and $property.GetMethod.IsPublic) { $accessors += 'get;' }
    if ($property.SetMethod -ne $null -and $property.SetMethod.IsPublic) { $accessors += 'set;' }
    if ($accessors.Count -eq 0) { continue }
    $static = if (($property.GetMethod -ne $null -and $property.GetMethod.IsStatic)) { 'static ' } else { '' }
    $members.Add('| `' + $static + (Format-TypeName $property.PropertyType) + ' ' + $property.Name + ' { ' + ($accessors -join ' ') + ' }` | ' + (Get-Doc ('P:' + $type.FullName + '.' + $property.Name)) + ' |')
  }
  foreach ($event in $type.Events) {
    if ($event.AddMethod -eq $null -or -not $event.AddMethod.IsPublic) { continue }
    $static = if ($event.AddMethod.IsStatic) { 'static ' } else { '' }
    $members.Add('| `' + $static + 'event ' + (Format-TypeName $event.EventType) + ' ' + $event.Name + '` | ' + (Get-Doc ('E:' + $type.FullName + '.' + $event.Name)) + ' |')
  }
  if ($members.Count -gt 0) {
    $apiLines.Add('| メンバー | 説明 |')
    $apiLines.Add('|---|---|')
    foreach ($member in $members) { $apiLines.Add($member) }
  }
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
$encoding = New-Object Text.UTF8Encoding $false
function Write-Page([string]$name, $lines) {
  $text = ($lines -join "`n") -replace "`n{3,}", "`n`n"
  [IO.File]::WriteAllText((Join-Path $OutDir $name), $text.TrimEnd() + "`n", $encoding)
}
Write-Page 'xml-modules.md' $xmlLines
Write-Page 'csharp-api.md' $apiLines
"Reference written: $OutDir (xml-modules.md, csharp-api.md)"
