# AGENTS.md — EsWeaponsLib Project

この Mod の開発規約・作業フローの**正本**です。ルールの追加・変更はこのファイルに行います(編集には人間の承認が要ります。下記)。

## エージェント設定ファイルの編集禁止

- AI エージェントの指示・権限・フックを決めるファイルは、**人間が明示的に依頼・承認したときだけ**編集する。
  対象: `AGENTS.md`、`CLAUDE.md`、各エージェントの設定ディレクトリ(`.claude/`・`.codex/` 等)の規則・設定・エージェント定義・スキル・フック。
- 承認はその依頼の範囲・その1回限り。以後の編集には改めて承認を得る。
- 規則の追加・修正が必要だと判断したら、差分案を示して承認を待つ。規則中の「〜を更新する」という記述も承認を前提とする。

## 基本情報

| 項目 | 値 |
|---|---|
| プロジェクト | `EsWeaponsLib Project` |
| Mod 表示名 / GUID | `E'sWeaponsLib` / `0d219960-b202-4906-b29d-8d90a61a89b8` |
| ルート名前空間 / アセンブリ | `EWL` / `EsWeaponsLib`(AssemblyVersion は `1.0.0.0` 固定) |
| 対象 Besiege | `1.9.1-25490`(Unity 5.4 / Mono 4.4 / C# 6 / .NET 3.5) |
| ビルド | `& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" .\src\EsWeaponsLib.sln /restore /p:Configuration=Release /nologo /v:minimal` |
| 成果物 | `.\EsWeaponsLib\EsWeaponsLib.dll`(ビルド後に自動コピー) |
| IL 検査 | `powershell -File .\tools\ilscan.ps1`(本体 AssemblyScanner 相当。OK なら `IL scan OK`) |
| 公開面一覧 | `powershell -File .\tools\api-surface.ps1` → `api-surface.txt`(差分で破壊的変更を検出) |
| 実機ログ | `Besiege_Data\output_log.txt` |

## このModは何か

E'sWeaponsLib(EWL)は、**外部Modの開発者とその AI エージェントが使う汎用武装ライブラリ**です。
XML カスタムモジュールと C# API を提供します。E'sTankCannonsMod(E砲)は最初の利用者で、リファレンス実装を兼ねます。

- 変えない目的: 外部 Mod は公開 API 経由で使い、内部の仕組みに関与しない / EWL のカスタムモジュールをそのまま使える /
  API で独自に機能を足し、独自カスタムモジュールを作れる。構成や層は手段であり、目的を満たす限り変えてよい。
- そのため EWL 自身の XML モジュールも公開 API だけで書く。新機能は内部 → `Api/` → (必要なら)`Modules/` の順に入れる。
- C# API の形は 1.0.0 まで確定しない(破壊的変更可、CHANGELOG に記録)。

- ACM(AddCustomModule)との互換は**目的外**。ACM 由来のコード・アセット・名称を持ち込まない。
- ワールド拡張(4km 境界突破・原点シフト等)は**廃止済み**。再導入しない。
- 実装範囲は E砲の実需で決め、名前・スキーマ・API の形は汎用武装の語彙で設計する。
## 必須規則

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
- **名前**: ACM 由来名(`Ad*Prop`、`OldCannon` 等)と E砲固有名(`ETCM` 等)を EWL に入れない。
  ACMu のコードを流用する場合は、これらの名前と ACM 行番号の注記を除去する。

## 作業フロー

### 通常の実装

1. 関係する資料を読む。コードと既存差分を確認する。
2. 実装する。
3. ビルド → IL 検査を実行する。
4. `Api/` か `Modules/` に触れたら「公開面の変更」へ。
5. ゲーム外で確認できる項目をセルフレビューする。
6. 実機確認が必要な項目と残課題を報告する。

### 公開面の変更(`Api/`・`Modules/`・XML スキーマ)

1. `tools/api-surface.ps1` を実行し、`api-surface.txt` の差分を確認する。
2. 判定: **「既存の外部ブロック XML か、コンパイル済みの外部 DLL が壊れるか」**
   - 壊れない: `CHANGELOG.md` に追記して進める。
   - 壊れる: α 期間(1.0 前)は `CHANGELOG.md` に「旧→新」の具体例を書いて進める。1.0 以降はメジャー版を上げ、人間に確認する。
3. `api-surface.txt` は変更と一緒に保存する(コミット対象)。

### リリース(1.0 以降の手順は設計書 §4・§7 を参照)

- `Mod.xml` の `Debug` を `false`、`Version` を更新、`CHANGELOG.md` を確定する。
