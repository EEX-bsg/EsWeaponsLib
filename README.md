# E'sWeaponsLib

Besiege の武装 Mod 向けの前提ライブラリです。XML カスタムモジュールと C# API で、射撃・弾体・着弾・爆発・弾倉・ブロック物理・マルチプレイ同期を提供します。

> [!IMPORTANT]
> **現在はα版(開発中)です。** 公開面(モジュール名・XML 要素・C# API)は 1.0 まで予告付きで変わることがあります。変更点は [CHANGELOG.md](CHANGELOG.md) を参照してください。

- 対象: Besiege 1.9.1-25490
- 最初の利用 Mod: E'sTankCannonsMod
- ACM(AddCustomModule)とは独立した実装で、互換性はありません。

## 読む場所

| あなたは | 読むもの |
|---|---|
| プレイヤー(EWL を使う Mod を遊びたい) | [はじめに](docs/getting-started.md) |
| XML でブロックを作る Mod 開発者 | [最初の武装を作る](docs/guide/xml-first-weapon.md) → [XML の使い方](docs/guide/xml-recipes.md) → [XML リファレンス](docs/reference/xml-modules.md) |
| C# で EWL を使う Mod 開発者 | [C# で使う](docs/guide/csharp.md) → [C# API リファレンス](docs/reference/csharp-api.md) |
| EWL に貢献したい | [CONTRIBUTING.md](CONTRIBUTING.md) |
| AI エージェント | [docs/llms.txt](docs/llms.txt)(全ページの目次)、貢献作業では [AGENTS.md](AGENTS.md) |

全ページの一覧は [docs/index.md](docs/index.md) にあります。

## ライセンス

[Slime Fair Play License 1.0](LICENSE)(SFPL)で提供します。作品固有の情報(著作権者・保護名称・連絡先)は [NOTICE](NOTICE) にあります。

**オープンソースライセンスではありません。** 要点は次のとおりです(正式な条件は LICENSE の日本語版です)。

- 自由: プレイ・配信・大会での使用、EWL に依存する Mod の作成と配布(有償も可)、コード片の流用、改変、フォーク。
- 改変版・フォークを配布するとき: ソースを公開し、本ライセンスのまま配布し、公式版と取り違えない名前にする(`Unofficial` などを付ける)。
- 許可が要るもの: 無改変のまま再配布すること、有償で配布すること、EWL を別の Mod に同梱すること。
- プルリクエストは歓迎します。取り込んだ貢献は LICENSE 第12条の条件で受け取ります。詳しくは [CONTRIBUTING.md](CONTRIBUTING.md)。

---
<div align="center">
<sub>開発: EEX-slime</sub>
<br>
<sub>BESIEGE © Spiderling Studios</sub>
</div>
