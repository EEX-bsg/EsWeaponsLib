# ドキュメントの書き方

## 構成

| 場所 | 読者 | 書き方 |
|---|---|---|
| `README.md` | 全員 | 入口。読者ごとに行き先を案内する |
| `docs/getting-started.md` | プレイヤー | 導入と困ったとき |
| `docs/guide/` | Mod開発者 | 手書き。手順と考え方、例 |
| `docs/reference/` | Mod開発者 | **生成物**。手で編集しない |
| `docs/contributing/` | 貢献者 | 手書き。設計・構成・規約 |
| `docs/llms.txt` | AIエージェント | 全ページの目次と1行説明 |
| `CHANGELOG.md` | 全員 | 利用者に見える変化だけ |

## 言語

- 正本は日本語です。英語版は日本語版から自動で作ります。英語版だけを直さないでください。

## リファレンスの生成

`docs/reference/` は、ビルドで出力されるXMLドキュメント(`///` コメント)とDLLから `tools/gen-reference.ps1` で作ります。

```powershell
powershell -File tools/gen-reference.ps1
```

- 説明を直したいときは `src/` の `///` コメントを直して作り直す。
- XML要素の説明が無いときは、対応するC#のフィールド(`EwlWeapon` 直下 → `WeaponSpec`、`Projectile` → `ProjectileSpec` など)の説明が使われる。
  モジュールにしかない要素(キー・スライダー・リソース名で書く項目)には、XML定義クラスに `///` を書く。

## Webサイト

`docs/` はVitePressでサイトにし、`main` へのpushでGitHub Pagesに公開されます(`.github/workflows/docs.yml`。`docs/` に変更が無いpushではビルドしません)。
ページの中身は普通のMarkdownのままで、先頭の `#` 見出しがページのタイトルになります。

手元で見るにはNode.js(LTS)を入れて、リポジトリ直下で:

```powershell
npm install          # 初回だけ
npm run docs:dev     # http://localhost:5173 。保存すると自動で反映される
npm run docs:build   # 公開と同じビルド(リンク切れがあると失敗する)
```

- ページを足したら `docs/.vitepress/config.mts` の `sidebar` にも足す(`docs/index.md` と `docs/llms.txt` も)。
- 注記はGitHubと同じ書き方(`> [!NOTE]`・`> [!WARNING]` など)で書くと、サイトでも枠付きで表示される。
- `docs/` の外(リポジトリ直下の `CHANGELOG.md` など)へは相対リンクでは辿れません。GitHub上のURLで書きます。
- Webに出したくないページは `docs/.vitepress/config.mts` の `srcExclude` に書きます(リポジトリ上では読めます)。
- サイトには各ページの生のMarkdownも置かれます(例: `/guide/csharp.md`)。AIは `/llms.txt` から辿れます。

## 書くときの注意

- 公開してはいけない情報を書かない: Besiege本体のデコンパイル結果やそこから分かる内部実装名、他Modのソースから分かる情報。
  Besiegeの挙動は、観測できる挙動として自分の言葉で書く。`tools/check-public.ps1` で機械的に確かめられる範囲は確かめる。
- 用語: XMLでブロックに書くモジュールは「XMLカスタムモジュール」と呼ぶ。
- 単位は `[m/s]`・`[s]`・`[度]` のように角括弧で書く。
- 例はそのまま動くものにする。XMLの例は省略した要素が既定値になることを前提に、必要な要素だけを書く。
- AIが読みやすいように、1ページ1話題にし、見出しと表で構造を見せる。新しいページを足したら `docs/llms.txt` と `docs/index.md` に追加する。
