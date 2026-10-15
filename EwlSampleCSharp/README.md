# EWL Sample CSharp

E'sWeaponsLib の `Weapon` を、自分の C# モジュール(`SampleCannon`)から作って駆動するサンプルです。
XML だけのサンプルは `../EwlSample/`、説明は [C# で使う](../docs/guide/csharp.md) にあります。

| ファイル | 内容 |
|---|---|
| `src/SampleMod.cs` | EWL が読み込まれているか確かめてからモジュールを登録する |
| `src/SampleCannonModule.cs` | 独自モジュールの XML 定義(キー・スライダー・間隔・爆発半径) |
| `src/SampleCannonBehaviour.cs` | シミュレーション開始で `Weapon` を作り、キーで撃ち、停止で外す。命中をログに出す |
| `Blocks/SampleCannon.xml` | `SampleCannon` を使うブロック |

## ビルド

EWL を先にビルドしてから(`EsWeaponsLib/EsWeaponsLib.dll` を参照します):

```powershell
& "<Visual Studio のインストール先>\MSBuild\Current\Bin\MSBuild.exe" .\EwlSampleCSharp\src\EwlSampleCSharp.csproj /restore /p:Configuration=Release /nologo /v:minimal
powershell -File .\tools\ilscan.ps1 -Dll .\EwlSampleCSharp\EwlSampleCSharp.dll
```

`EwlSampleCSharp/EwlSampleCSharp.dll` にコピーされ、Besiege がこのフォルダを Mod として読み込みます。
