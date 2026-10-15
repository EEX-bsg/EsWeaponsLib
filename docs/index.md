# E'sWeaponsLibドキュメント

E'sWeaponsLib(EWL)は、Besiegeの武装Mod向けの前提ライブラリです。XMLカスタムモジュールとC# APIで、
射撃・弾体・着弾・爆発・弾倉・ブロック物理・マルチプレイ同期を提供します。

## プレイヤー

- [はじめに](getting-started.md) — 導入・マルチプレイ・困ったとき

## Mod開発者

- [基本の考え方](guide/concepts.md) — 使い方の4段階・定義の形・実行権威・ライフサイクル・よくある失敗
- [最初の武装を作る(XML)](guide/xml-first-weapon.md) — XMLだけで撃てるブロックを作る
- [XMLの使い方](guide/xml-recipes.md) — 弾倉・バースト・スライダー・弾・信管と爆発・エフェクトと音・ロケット
- [ブロックの物理(EwlBlockPhysics)](guide/xml-block-physics.md) — 接合強度・物理マテリアル・回転軸・追加コライダー
- [C#で使う](guide/csharp.md) — 武装の観測・命中と攻撃者・自分のモジュールで武装を作る
- [XMLリファレンス](reference/xml-modules.md) — 全要素と既定値(生成)
- [C# APIリファレンス](reference/csharp-api.md) — 公開型とメンバー(生成)
- [変更履歴](../CHANGELOG.md)

## 貢献者

- [CONTRIBUTING.md](../CONTRIBUTING.md) — 環境・ビルド・プルリクエスト
- [設計思想](contributing/design-principles.md) — 目的・原則・責務範囲・利用形態
- [構成](contributing/architecture.md) — フォルダ・依存の向き・武装の流れ・実行権威とMP
- [公開面と互換性](contributing/compatibility.md) — 何を壊してはいけないか、公開面を変える手順
- [コーディング規約](contributing/conventions.md)
- [確認とテスト](contributing/testing.md)
- [ドキュメントの書き方](contributing/documentation.md)
