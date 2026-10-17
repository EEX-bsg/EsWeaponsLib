# E'sWeaponsLib

Besiegeの武装Mod向けの前提ライブラリです。XMLカスタムモジュールとC# APIで、射撃・弾体・着弾・爆発・弾倉・ブロック物理・マルチプレイ同期を提供します。

> [!IMPORTANT]
> **現在はα版(開発中)です。** 公開面(モジュール名・XML要素・C# API)は1.0まで予告付きで変わることがあります。変更点は [CHANGELOG.md](CHANGELOG.md) を参照してください。

- 対象: Besiege 1.9.1-25490
- 最初の利用Mod: E'sTankCannonsMod
- ACM(AddCustomModule)とは独立した実装で、互換性はありません。

## 読む場所

| あなたは | 読むもの |
|---|---|
| プレイヤー(EWLを使うModを遊びたい) | [はじめに](docs/getting-started.md) |
| XMLでブロックを作るMod開発者 | [最初の武装を作る](docs/guide/xml-first-weapon.md) → [XMLの使い方](docs/guide/xml-recipes.md) → [XMLリファレンス](docs/reference/xml-modules.md) |
| C#でEWLを使うMod開発者 | [C#で使う](docs/guide/csharp.md) → [C# APIリファレンス](docs/reference/csharp-api.md) |
| EWLに貢献したい | [CONTRIBUTING.md](CONTRIBUTING.md) |
| AIエージェント | [docs/llms.txt](docs/llms.txt)(全ページの目次)、貢献作業では [AGENTS.md](AGENTS.md) |

全ページの一覧は [docs/index.md](docs/index.md) にあります。同じ内容をWebサイト(日本語・English)でも読めます: https://eex-bsg.github.io/EsWeaponsLib/

## ライセンス

[Slime Fair Play License 1.0](LICENSE)(SFPL)で提供します。作品固有の情報(著作権者・保護名称・連絡先)は [NOTICE](NOTICE) にあります。

**オープンソースライセンスではありません。** 要点は次のとおりです(正式な条件はLICENSEの日本語版です)。

- 自由: プレイ・配信・大会での使用、EWLに依存するModの作成と配布(有償も可)、コード片の流用、改変、フォーク。
- 改変版・フォークを配布するとき: ソースを公開し、本ライセンスのまま配布し、公式版と取り違えない名前にする(`Unofficial` などを付ける)。
- 許可が要るもの: 無改変のまま再配布すること、有償で配布すること、EWLを別のModに同梱すること。
- プルリクエストは歓迎します。取り込んだ貢献はLICENSE第12条の条件で受け取ります。詳しくは [CONTRIBUTING.md](CONTRIBUTING.md)。

---
<div align="center">
<sub>開発: EEX-slime</sub>
<br>
<sub>BESIEGE © Spiderling Studios</sub>
</div>
