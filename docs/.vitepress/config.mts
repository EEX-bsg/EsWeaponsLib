import { defineConfig } from 'vitepress'
import { cpSync, readdirSync, statSync } from 'node:fs'
import { join, relative } from 'node:path'

// docs/ を GitHub Pages に出すサイトの設定。ページの中身は普通の Markdown のまま(先頭の # 見出しがタイトル)。
// ローカルで確認: npm run docs:dev

// ページを足したらここにも足す(docs/index.md・docs/llms.txt と同じ並び)
const sidebar = [
  {
    text: 'プレイヤー',
    items: [{ text: 'はじめに', link: '/getting-started' }],
  },
  {
    text: 'ガイド',
    items: [
      { text: '基本の考え方', link: '/guide/concepts' },
      { text: '最初の武装を作る(XML)', link: '/guide/xml-first-weapon' },
      { text: 'XML の使い方', link: '/guide/xml-recipes' },
      { text: 'ブロックの物理', link: '/guide/xml-block-physics' },
      { text: 'C# で使う', link: '/guide/csharp' },
    ],
  },
  {
    text: 'リファレンス',
    items: [
      { text: 'XML カスタムモジュール', link: '/reference/xml-modules' },
      { text: 'C# API', link: '/reference/csharp-api' },
    ],
  },
  {
    text: '貢献者向け',
    collapsed: true,
    items: [
      { text: '設計思想', link: '/contributing/design-principles' },
      { text: '構成', link: '/contributing/architecture' },
      { text: '公開面と互換性', link: '/contributing/compatibility' },
      { text: 'コーディング規約', link: '/contributing/conventions' },
      { text: '確認とテスト', link: '/contributing/testing' },
      { text: 'ドキュメントの書き方', link: '/contributing/documentation' },
    ],
  },
]

// 日本語は空白で区切られないので、検索の索引と検索語を Intl.Segmenter で単語に分ける。
// VitePress はこの関数をソース文字列としてブラウザへ送るので、外の変数を参照せず中だけで完結させる
const tokenize = (text: string): string[] =>
  Array.from(new Intl.Segmenter('ja', { granularity: 'word' }).segment(text))
    .filter((part) => part.isWordLike)
    .map((part) => part.segment.toLowerCase())

// AI が生の Markdown を読めるよう、docs/ の .md と llms.txt を出力先にもそのまま置く(/guide/csharp.md など)
function copyMarkdown(srcDir: string, outDir: string) {
  const walk = (dir: string) => {
    for (const name of readdirSync(dir)) {
      if (name.startsWith('.')) continue
      const path = join(dir, name)
      if (statSync(path).isDirectory()) walk(path)
      else if (name.endsWith('.md') || name === 'llms.txt') cpSync(path, join(outDir, relative(srcDir, path)))
    }
  }
  walk(srcDir)
}

export default defineConfig({
  lang: 'ja',
  title: "E'sWeaponsLib",
  description: 'Besiege の武装 Mod 向け前提ライブラリ(XML カスタムモジュールと C# API)',
  // GitHub Pages のプロジェクトサイトでは /<リポジトリ名>/ の下に置かれる(ワークフローが DOCS_BASE で渡す)
  base: process.env.DOCS_BASE || '/',
  cleanUrls: true,
  // Web に出さないページ(docs/ からの相対パスの glob。リポジトリ上では読める)
  srcExclude: [],
  // docs/ の外(リポジトリ直下)へのリンク。リポジトリ名が決まったら GitHub の URL に直してこの行を消す
  ignoreDeadLinks: [/CHANGELOG$/, /CONTRIBUTING$/],
  lastUpdated: false,
  appearance: 'dark',
  head: [
    ['link', { rel: 'preconnect', href: 'https://fonts.googleapis.com' }],
    ['link', { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' }],
    ['link', { rel: 'stylesheet', href: 'https://fonts.googleapis.com/css2?family=Noto+Sans+JP:wght@400;500;700&family=Noto+Sans+Mono:wght@400;500&display=swap' }],
  ],
  markdown: {
    theme: { light: 'github-light', dark: 'github-dark' },
  },
  themeConfig: {
    nav: [
      { text: 'ガイド', link: '/guide/concepts' },
      { text: 'リファレンス', link: '/reference/xml-modules' },
      { text: '貢献者向け', link: '/contributing/design-principles' },
    ],
    sidebar,
    outline: { level: [2, 3], label: 'このページの内容' },
    search: {
      provider: 'local',
      options: {
        miniSearch: { options: { tokenize } },
        translations: {
          button: { buttonText: '検索', buttonAriaLabel: '検索' },
          modal: {
            displayDetails: '詳細を表示',
            resetButtonTitle: 'リセット',
            backButtonTitle: '閉じる',
            noResultsText: '見つかりませんでした',
            footer: { selectText: '選択', navigateText: '移動', closeText: '閉じる' },
          },
        },
      },
    },
    docFooter: { prev: '前のページ', next: '次のページ' },
    darkModeSwitchLabel: '表示モード',
    lightModeSwitchTitle: 'ライトモードにする',
    darkModeSwitchTitle: 'ダークモードにする',
    sidebarMenuLabel: 'メニュー',
    returnToTopLabel: 'ページの先頭へ',
    langMenuLabel: '言語',
    notFound: { title: 'ページが見つかりません', quote: '', linkText: 'トップへ戻る' },
  },
  buildEnd(siteConfig) {
    copyMarkdown(siteConfig.srcDir, siteConfig.outDir)
  },
})
