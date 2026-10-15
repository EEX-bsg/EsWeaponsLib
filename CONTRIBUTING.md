# EWL への貢献

E'sWeaponsLib(EWL)への貢献を歓迎します。バグ報告・提案は Issue へ、修正はプルリクエストへお願いします。

## 最初に読むもの

| 内容 | ページ |
|---|---|
| 何を守り、なぜそうしているか | [docs/contributing/design-principles.md](docs/contributing/design-principles.md) |
| コードの構成と流れ | [docs/contributing/architecture.md](docs/contributing/architecture.md) |
| 公開面と互換性の決まり | [docs/contributing/compatibility.md](docs/contributing/compatibility.md) |
| コーディング規約 | [docs/contributing/conventions.md](docs/contributing/conventions.md) |
| 確認とテスト | [docs/contributing/testing.md](docs/contributing/testing.md) |
| ドキュメントの書き方 | [docs/contributing/documentation.md](docs/contributing/documentation.md) |

AI エージェントで作業するときは、リポジトリ直下の [AGENTS.md](AGENTS.md) を読ませてください。

## 開発環境

- Besiege 1.9.1(Steam 版)
- Visual Studio 2022 または Build Tools for Visual Studio 2022(MSBuild。.NET Framework 3.5 の参照アセンブリは NuGet で取得されます)
- Windows PowerShell 5.1(`tools/` のスクリプト用)

リポジトリは **`Besiege/Besiege_Data/Mods/` の下に clone してください**。
ビルドとツールが、Besiege 本体の DLL(`Besiege_Data/Managed/`)を相対パスで探します。

```powershell
cd "<Steam>\steamapps\common\Besiege\Besiege_Data\Mods"
git clone <このリポジトリの URL> "EsWeaponsLib Project"
```

## ビルドと確認

```powershell
& "<Visual Studio のインストール先>\MSBuild\Current\Bin\MSBuild.exe" .\src\EsWeaponsLib.sln /restore /p:Configuration=Release /nologo /v:minimal
powershell -File .\tools\ilscan.ps1          # 禁止 API の検査。IL scan OK なら通過
powershell -File .\tools\api-surface.ps1     # 公開面の一覧 api-surface.txt を更新
powershell -File .\tools\gen-reference.ps1   # docs/reference/ を更新
powershell -File .\tools\check-public.ps1    # 公開してはいけない情報が混ざっていないか
```

ビルドすると `EsWeaponsLib/EsWeaponsLib.dll` にコピーされ、Besiege がその Mod として読み込みます。
ゲームの中での確認は [docs/contributing/testing.md](docs/contributing/testing.md) を参照してください。

## プルリクエスト

1. 大きな変更や、責務範囲([design-principles.md](docs/contributing/design-principles.md#責務範囲))の外に見える機能は、先に Issue で相談してください。
2. 公開面(`Api/`・`Modules/`・XML スキーマ)を変えたら、`api-surface.txt` と `docs/reference/` を更新し、`CHANGELOG.md` に追記してください
   ([compatibility.md](docs/contributing/compatibility.md))。
3. PR の説明に、何をなぜ変えたかと、確認したこと(ビルド・検査・ゲーム内の確認、MP を確認したかどうか)を書いてください。
4. PR テンプレートの「貢献の条件」に同意してください。取り込んだ貢献は [LICENSE](LICENSE) 第12条の条件で受け取ります。

## ライセンス

EWL は [Slime Fair Play License 1.0](LICENSE) で提供されています。オープンソースライセンスではありません。要点は [README.md](README.md#ライセンス) を参照してください。
