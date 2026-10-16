# Changelog

E'sWeaponsLib(EWL)の変更履歴です。形式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に沿います。

- 書くのは利用者(プレイヤー・Mod開発者)に見える変化だけです。詳しい説明は [docs/](docs/index.md) にあります。
- 公開面 = XMLカスタムモジュールの名前・要素と、C#の公開型・メンバー(一覧は [api-surface.txt](api-surface.txt))。
- 既存のブロックXMLかコンパイル済みの外部DLLが壊れる変更は **[Breaking]** と書き、「旧 → 新」の例を付けます。
- 1.0より前は破壊的変更がありえます。1.0以降、破壊的変更はメジャー版でだけ行います。

## [Unreleased]

最初の公開版に向けた内容です。この版でできることをまとめます。

### XMLカスタムモジュール

- `EwlWeapon`: ブロックを武装にする。発射キー、連射・切替式、発射間隔・遅延・拡散・反動、バースト、総弾数・弾倉・リロード、
  弾の物理・コライダー・見た目、ダメージ・衝撃力・命中物にくっつく弾、時限信管・起爆遅延、爆発、推進(ロケット)、
  発射・弾・爆発のエフェクトと音、待機弾の表示(ビルド中も)。発射レート・遅延・時限信管・弾倉容量・Power(初速)・ブースターの推力はスライダーで変えられる。1ブロックに複数書ける。
- `EwlBlockPhysics`: ブロックの接合強度・物理マテリアル・接合の回転軸を設定し、コライダー(ブロックを置ける面を含む)を足す。

### C# API(`EWL.Api`)

- `WeaponSpec` / `ProjectileSpec` / `ExplosionSpec` / `BoosterSpec`: 武装・弾・爆発・推進の定義(純データ)。
- `Weapon`: 自分のモジュールやブロックスクリプトから武装を作って駆動する部品(`Create` / `TryFire` / `TryReload` / `Destroy`)。
  状態(`Ammo` / `ReserveAmmo` / `IsReloading`)とイベント(`Fired` / `ReloadStarted` / `Hit`)を持ち、`GetAll` でブロック上の武装を列挙できる。
  `CreateLoadedProjectilePreview` でビルド中のブロックに待機弾の見た目を置ける。
- `Weapon.AnyHit` / `ProjectileHit` / `Attacker`: 誰の弾がどこに当たったか、バニラのボムを誰が起爆させたか。
  `AnyHit` はBesiege標準の大砲(「爆発する砲弾」の爆風を含む)・クロスボウ・FuelCannonの弾も通知し、`ProjectileHit.Source`(`HitSource`)で武器の種類が分かる。
- `BundleAssets`: AssetBundleからアセットを取り出して覚える。

### マルチプレイ

- EWLのモジュール・`Weapon` で作った武装は、利用者が何もしなくても同期される(弾はホストが処理し、クライアントには見た目を出す)。

### 読み込み

- EWLはタイトル画面では読み込まれない(無効にしても再起動は不要)。EWLを使うModは `Mod.xml` の `LoadOrder` を1以上にする。

### 既知の制限

- 誘導(`ProjectileSpec.Guidance` / XML `Projectile/Guidance`)は仮実装で、今後まとめて作り直す予定です(破壊的変更になります)。
- 途中参加したプレイヤーには、参加前に撃たれた弾は見えません(次の弾から見えます)。
