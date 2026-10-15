# AGENTS.md — E'sWeaponsLib

AI エージェント向けの入口です。作業の前に読んでください。規約の詳しい正本は `docs/contributing/` にあり、
このファイルは要点・コマンド・手順を持ちます。人向けの入口は `CONTRIBUTING.md` です。

## エージェント設定ファイルの編集禁止

- AI エージェントの指示・権限・フックを決めるファイルは、**人間が明示的に依頼・承認したときだけ**編集する。
  対象: `AGENTS.md`、`CLAUDE.md`、各エージェントの設定ディレクトリ(`.claude/`・`.codex/` 等)の規則・設定・エージェント定義・スキル・フック。
- 承認はその依頼の範囲・その1回限り。以後の編集には改めて承認を得る。
- 規則の追加・修正が必要だと判断したら、差分案を示して承認を待つ。規則中の「〜を更新する」という記述も承認を前提とする。

## 基本情報

| 項目 | 値 |
|---|---|
| Mod 表示名 / GUID | `E'sWeaponsLib` / `0d219960-b202-4906-b29d-8d90a61a89b8` |
| ルート名前空間 / アセンブリ | `EWL` / `EsWeaponsLib`(AssemblyVersion は `1.0.0.0` 固定) |
| 対象 Besiege | `1.9.1-25490`(Unity 5.4 / Mono / C# 6 / .NET 3.5) |
| 置き場所 | リポジトリは `Besiege_Data/Mods/` の下(参照 DLL を相対パスで探す) |
| ビルド | `MSBuild.exe .\src\EsWeaponsLib.sln /restore /p:Configuration=Release /nologo /v:minimal`(Visual Studio 2022 の MSBuild) |
| 成果物 | `.\EsWeaponsLib\EsWeaponsLib.dll`(ビルド後に自動コピー)と `src\EsWeaponsLib\bin\Release\EsWeaponsLib.xml`(XML ドキュメント) |
| IL 検査 | `powershell -File .\tools\ilscan.ps1`(OK なら `IL scan OK`) |
| 公開面一覧 | `powershell -File .\tools\api-surface.ps1` → `api-surface.txt` |
| リファレンス生成 | `powershell -File .\tools\gen-reference.ps1` → `docs/reference/` |
| 公開前チェック | `powershell -File .\tools\check-public.ps1`(OK なら `check-public OK`) |
| 実機ログ | `Besiege_Data\output_log.txt` |

## このModは何か

E'sWeaponsLib(EWL)は、**外部 Mod の開発者とその AI エージェントが使う汎用武装ライブラリ**です。
XML カスタムモジュールと C# API を提供します。E'sTankCannonsMod が最初の利用者です。

- 変えない目的: 外部 Mod は公開 API 経由で使い、内部の仕組みに関与しない / EWL のカスタムモジュールをそのまま使える /
  API で独自に機能を足し、独自カスタムモジュールを作れる。構成や層は手段であり、目的を満たす限り変えてよい。
- そのため EWL 自身の XML モジュールも公開 API だけで書く。新機能は内部 → `Api/` → (必要なら)`Modules/` の順に入れる。
- C# API の形は 1.0.0 まで確定しない(破壊的変更可、CHANGELOG に記録)。
- ACM(AddCustomModule)との互換は**目的外**。ACM 由来のコード・アセット・名称を持ち込まない。
- ワールド拡張(4km 境界突破・原点シフト等)は**廃止済み**。再導入しない。
- 実装範囲は実在の利用者の必要で決め、名前・スキーマ・API の形は汎用武装の語彙で設計する。

## 読むもの

| 作業 | 資料 |
|---|---|
| どの作業でも | `docs/contributing/design-principles.md`、`docs/contributing/conventions.md` |
| コードの構成を知る | `docs/contributing/architecture.md` |
| 公開面(`Api/`・`Modules/`・XML スキーマ)を変える | `docs/contributing/compatibility.md` |
| 確認・テスト | `docs/contributing/testing.md` |
| ドキュメントを書く | `docs/contributing/documentation.md` |
| 利用者から見た使い方 | `docs/guide/`、`docs/reference/` |

## 必須規則(要点。詳細は docs/contributing/conventions.md)

### 共通(Besiege Mod)

- C# 6、.NET 3.5 サブセット、Unity 5.4 を前提にする。`Task`・`async`/`await`・C# 7 以降の構文を使わない。
- `System.Reflection`、禁止された `System.IO` / `System.Net` / `System.Xml` を使わない。`InternalModding.*` を参照しない。ファイル I/O は `Modding.ModIO`。
- Unity API はメインスレッドだけで呼ぶ。`Update` / `FixedUpdate` での不要な割り当てと毎フレームログを避ける。
- `.cs` と `.xml` のインデントはスペース4つ。タブを使わない(`.editorconfig`)。
- イベント購読と解除、生成と破棄、開始と停止を対にする。
- SP、MP Host、MP Client、MP ローカルシミュレーションを区別する。武装・物理の実行権威は `IsSimulating` 系で判定し、`IsHost` で代用しない。
- 例外を `catch` したら必ずログを出す。握りつぶさない。EWL のログは `[EWL]` で始める。
- 他 Mod のコードや素材は利用条件を確認せず流用しない。デコンパイル資料をリポジトリに含めない。

### EWL 固有

- **構成**: csproj 1つ・DLL 1つ。フォルダと namespace を必ず一致させる(`Weapons/` → `EWL.Weapons`)。
  - `Api/` 公開 C# API / `Modules/` XML モジュール / `Weapons/` / `Effects/` / `Net/` / `Besiege/`
- **依存方向**: `Modules/` は EWL の namespace のうち `EWL.Api` だけを使う(内部が見えても使わない。足りなければ `Api/` に足す)。
  内部(`Weapons/` 等)は `Modules/` を参照せず、XML スキーマ型を持ち込まない。データは `Api/` の型で渡す。
- **拡張方式**: 合成(利用側が部品を持って駆動)とイベントだけ。継承用基底クラス・フック連鎖・レジストリを作らない。
- **公開範囲**: `public` は `Api/` と `Modules/` の文書化対象だけ。それ以外は `internal`。
  `Modules/` の XML 定義クラスは XmlSerializer の都合で public だが、それは「XML スキーマ」であって C# API ではない。
- **本体依存**: 公開 API に Besiege 本体の内部型(`Machine`、`KillingHandler`、`NetworkCompression` 等)を出さない
  (`Modding.*` と UnityEngine 型は可。`BlockBehaviour` のように Modder が慣例的に使う型を出すかは公開面の変更として個別に判断)。落とし穴がある、または複数箇所で使う本体操作は `Besiege/` のヘルパーに集約する。それ以外の直接参照は可。
- **抽象化**: インターフェースは実装が2つ以上あるときだけ作る。DI・フック網・Null 実装を作らない。
- **XML スキーマ**: 新要素は既定値付き・非必須。改名時は旧名を別名として残す(初回リリース後。未リリースの間は利用側と同時に直せば別名不要)。必須要素の追加は外部ブロックを全モジュール喪失させる。
- **命名**: 一般的な C# の慣例に従い、読んで役割が分かる名前にする(型・メソッドは PascalCase の完全な英単語)。
  コード量を減らすことはあっても名前は削らない。`Fx` `Sim` `Hits` のような略称や、仕様・部品を指す1文字の別名(`var f = spec.Fire`)は使わない。
  ループ添字や `rb`(Rigidbody)・`go`(GameObject)のような定番の略は可。冗長に長くする必要はない。
- **名前**: ACM 由来名(`Ad*Prop`、`OldCannon` 等)と、特定の利用 Mod の固有名を EWL に入れない。
- **ドキュメント**: 公開する型・メンバーには `///` で説明を書く(単位・既定値の意味を含める。`docs/reference/` はここから生成される)。

## 作業フロー

### 通常の実装

1. 関係する資料を読む。コードと既存差分を確認する。
2. 実装する。
3. ビルド → IL 検査 → 公開前チェックを実行する。
4. `Api/` か `Modules/` に触れたら「公開面の変更」へ。
5. ゲーム外で確認できる項目をセルフレビューする(`docs/contributing/testing.md`)。
6. 実機確認が必要な項目と残課題を報告する。

### 公開面の変更(`Api/`・`Modules/`・XML スキーマ)

1. `tools/api-surface.ps1` を実行し、`api-surface.txt` の差分を確認する。
2. 判定: **「既存の外部ブロック XML か、コンパイル済みの外部 DLL が壊れるか」**
   - 壊れない: `CHANGELOG.md` の `[Unreleased]` に1行で追記する。
   - 壊れる: `[Breaking]` と書き「旧→新」の具体例を付ける。1.0 以降はメジャー版を上げ、人間に確認する。
3. `tools/gen-reference.ps1` で `docs/reference/` を作り直し、関係する `docs/guide/` も直す。
4. `api-surface.txt` と `docs/reference/` は変更と一緒にコミットする。

### ドキュメント

- 正本は日本語。英語版は日本語版から自動で作るので、英語版だけを直さない。
- `docs/reference/` は生成物。手で編集せず `///` を直して作り直す。
- 公開してはいけない情報(デコンパイル由来の内部実装名、他 Mod のソース由来の情報)を書かない。Besiege の挙動は観測できる挙動として書く。
- 用語: ブロック XML に書くモジュールは「XML カスタムモジュール」。

### リリース

- `Mod.xml` の `Debug` を `false`、`Version` を更新、`CHANGELOG.md` の `[Unreleased]` を版番号に確定する。

## 確認が必要な場合

- 公開済みの ID・保存形式・ネットワーク形式・公開面(1.0 以降)を壊す。
- `docs/contributing/design-principles.md` の目的・原則・責務範囲から外れる(範囲外に見える機能は実装前に確認する)。
- 選択によって機能・操作感・MP の実行権威が大きく変わる。
- 禁止 API や本体の private 実装への依存が必要になる。第三者のコード・素材の利用条件が不明。
