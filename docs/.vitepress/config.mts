import { defineConfig } from 'vitepress'
import { cpSync, readdirSync, statSync } from 'node:fs'
import { join, relative } from 'node:path'

// docs/ を GitHub Pages に出すサイトの設定。ページの中身は普通の Markdown のまま(先頭の # 見出しがタイトル)。
// ローカルで確認: npm run docs:dev

// ページを足したらここにも足す(docs/index.md・docs/llms.txt と同じ並び)。英語版は docs/en/ に同じ構成で置く
const sidebarJa = [
  {
    text: 'プレイヤー',
    items: [{ text: 'はじめに', link: '/getting-started' }],
  },
  {
    text: 'ガイド',
    items: [
      { text: '基本の考え方', link: '/guide/concepts' },
      { text: '最初の武装を作る(XML)', link: '/guide/xml-first-weapon' },
      { text: 'XMLの使い方', link: '/guide/xml-recipes' },
      { text: 'ブロックの物理', link: '/guide/xml-block-physics' },
      { text: '誘導ミサイル', link: '/guide/guidance' },
      { text: 'C#で使う', link: '/guide/csharp' },
    ],
  },
  {
    text: 'リファレンス',
    items: [
      { text: 'XMLカスタムモジュール', link: '/reference/xml-modules' },
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

const sidebarEn = [
  {
    text: 'Players',
    items: [{ text: 'Getting started', link: '/en/getting-started' }],
  },
  {
    text: 'Guide',
    items: [
      { text: 'Concepts', link: '/en/guide/concepts' },
      { text: 'First weapon (XML)', link: '/en/guide/xml-first-weapon' },
      { text: 'XML recipes', link: '/en/guide/xml-recipes' },
      { text: 'Block physics', link: '/en/guide/xml-block-physics' },
      { text: 'Guided missiles', link: '/en/guide/guidance' },
      { text: 'Using EWL from C#', link: '/en/guide/csharp' },
    ],
  },
  {
    text: 'Reference',
    items: [
      { text: 'XML custom modules', link: '/en/reference/xml-modules' },
      { text: 'C# API', link: '/en/reference/csharp-api' },
    ],
  },
  {
    text: 'Contributing',
    collapsed: true,
    items: [
      { text: 'Design principles', link: '/en/contributing/design-principles' },
      { text: 'Architecture', link: '/en/contributing/architecture' },
      { text: 'Public surface and compatibility', link: '/en/contributing/compatibility' },
      { text: 'Coding conventions', link: '/en/contributing/conventions' },
      { text: 'Verification and testing', link: '/en/contributing/testing' },
      { text: 'Writing documentation', link: '/en/contributing/documentation' },
    ],
  },
]

// 日本語は空白で区切られないので、検索の索引と検索語を Intl.Segmenter で単語に分ける。
// VitePress はこの関数をソース文字列としてブラウザへ送るので、外の変数を参照せず中だけで完結させる
const tokenize = (text: string): string[] =>
  Array.from(new Intl.Segmenter('ja', { granularity: 'word' }).segment(text))
    .filter((part) => part.isWordLike)
    .map((part) => part.segment.toLowerCase())

// 見出しの ID。VitePress 標準は文字を NFKD に分解するため「ジ」「で」などの ID がリンク(NFC)と一致しない。
// 分解せずに、記号の並びを - にして小文字にする(tools/gen-reference.ps1 の Get-Anchor も同じ規則)
function slugify(text: string): string {
  return text
    .normalize('NFC')
    .replace(/[\u0000-\u001f]/g, '')
    .replace(/[\s~`!@#$%^&*()\-_+=[\]{}|\\;:"'“”‘’<>,.?/]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .replace(/^(\d)/, '_$1')
    .toLowerCase()
}

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
  description: 'Besiegeの武装Mod向け前提ライブラリ(XMLカスタムモジュールとC# API)',
  // GitHub Pages のプロジェクトサイトでは /<リポジトリ名>/ の下に置かれる(ワークフローが DOCS_BASE で渡す)
  base: process.env.DOCS_BASE || '/',
  cleanUrls: true,
  // Web に出さないページ(docs/ からの相対パスの glob。リポジトリ上では読める)
  srcExclude: [],
  lastUpdated: false,
  appearance: 'dark',
  head: [
    ['link', { rel: 'preconnect', href: 'https://fonts.googleapis.com' }],
    ['link', { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' }],
    ['link', { rel: 'stylesheet', href: 'https://fonts.googleapis.com/css2?family=Noto+Sans+JP:wght@400;500;700&family=Noto+Sans+Mono:wght@400;500&display=swap' }],
  ],
  markdown: {
    theme: { light: 'github-light', dark: 'github-dark' },
    anchor: { slugify },
  },
  // 日本語が正本(ルート)。英語は docs/en/ に同じ構成で置く自動翻訳
  locales: {
    root: {
      label: '日本語',
      lang: 'ja',
      themeConfig: {
        nav: [
          { text: 'ガイド', link: '/guide/concepts' },
          { text: 'リファレンス', link: '/reference/xml-modules' },
          { text: '貢献者向け', link: '/contributing/design-principles' },
        ],
        sidebar: sidebarJa,
        outline: { level: [2, 3], label: 'このページの内容' },
        docFooter: { prev: '前のページ', next: '次のページ' },
        darkModeSwitchLabel: '表示モード',
        lightModeSwitchTitle: 'ライトモードにする',
        darkModeSwitchTitle: 'ダークモードにする',
        sidebarMenuLabel: 'メニュー',
        returnToTopLabel: 'ページの先頭へ',
        langMenuLabel: '言語',
        notFound: { title: 'ページが見つかりません', quote: '', linkText: 'トップへ戻る' },
      },
    },
    en: {
      label: 'English',
      lang: 'en',
      link: '/en/',
      description: 'Prerequisite library for Besiege weapon mods (XML custom modules and C# API)',
      themeConfig: {
        nav: [
          { text: 'Guide', link: '/en/guide/concepts' },
          { text: 'Reference', link: '/en/reference/xml-modules' },
          { text: 'Contributing', link: '/en/contributing/design-principles' },
        ],
        sidebar: sidebarEn,
        outline: { level: [2, 3], label: 'On this page' },
        notFound: { title: 'Page not found', quote: '', linkText: 'Back to top' },
      },
    },
  },
  themeConfig: {
    socialLinks: [{ icon: 'github', link: 'https://github.com/EEX-bsg/EsWeaponsLib' }],
    search: {
      provider: 'local',
      options: {
        miniSearch: { options: { tokenize } },
        locales: {
          root: {
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
      },
    },
  },
  buildEnd(siteConfig) {
    copyMarkdown(siteConfig.srcDir, siteConfig.outDir)
  },
})
