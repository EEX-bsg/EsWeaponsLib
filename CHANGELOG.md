# Changelog

E'sWeaponsLib(EWL)の変更履歴です。形式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に沿います。

- 書くのは利用者(プレイヤー・Mod開発者)に見える変化だけです。詳しい説明は [docs/](docs/index.md) にあります。
- 公開面 = XMLカスタムモジュールの名前・要素と、C#の公開型・メンバー(一覧は [api-surface.txt](api-surface.txt))。
- 既存のブロックXMLかコンパイル済みの外部DLLが壊れる変更は **[Breaking]** と書き、「旧 → 新」の例を付けます。
- 1.0より前は破壊的変更がありえます。1.0以降、破壊的変更はメジャー版でだけ行います。

## [Unreleased]

## [0.2.1] - 2026-10-19

### 武装

- `Booster/Lift`(`BoosterSpec.Lift`、既定0)を追加。機首に垂直な速度成分を打ち消して飛ぶ向きを機首へ寄せ、推力が切れた後も誘導で曲がれる。
- `Projectile/PenetrationMode`(`ProjectileSpec.PenetrationMode`)を追加。`FirstBlock` で最初に当たったブロックの厚みだけ貫通する。既定の `Total` は従来どおり合計で数える。
- 貫通: 速い弾で接触が装甲の奥で報告されると、命中した装甲が数えられず、止まる位置がずれたり貫通しなかったりしたのを直した。

## [0.2.0] - 2026-10-19

### 武装

- `EwlWeapon` に `<FireMode>` を追加。`SemiAuto` で、キーを押すたびに1回(バースト1回)だけ撃つ。既定の `Auto` は従来どおり。
- `Projectile` に `<Penetration>`(C#は `ProjectileSpec.Penetration`)を追加。装甲を進む向きの厚みで上限まで貫通し、止まった所で命中・起爆する。既定の0は貫通しない。

### 爆発

- `Projectile/Explosion/ForwardOffset`(C#: `ExplosionSpec.ForwardOffset`)を追加。爆発地点を弾の前方へ指定距離 [m] ずらす(HEAT 等)。既定 0 は従来どおり。

### 誘導

- **[Breaking]** 誘導の目標は自動で選ばず、C#の `Weapon.Target` で指定する。旧: 発射の瞬間に、発射方向の前方30度以内で最も近い敵のコアを自動で選んでいた。
  新: `Weapon.Target` に入れた目標を、`TryFire` が発射を受け付けた時点で確定する。XMLだけの武装は目標を持たず、無誘導で飛ぶ。
  例: `_weapon.Target = _weapon.FindTarget(camera.transform.position, camera.transform.forward, 6f);`
- **[Breaking]** 誘導中の弾に掛かっていた隠れた減速(物理フレームごとに速度の約0.9%)を廃止した。誘導する弾が速くなるので、旧と同じ速さにするには `Projectile/Drag` を足す
  (旧と同等は約 `0.9`)か、`Projectile/Booster/MaxSpeed` で上限を決める。
- 追加: `Weapon.FindTarget`(照準の中心に最も近い敵のコアを探す)、`GuidanceSpec.ReleaseAngle` / XML `Projectile/Guidance/ReleaseAngle`
  (機首と目標方向の角がこれを超えたら誘導を解除する。既定0は解除しない)。
- 変更: 目標を失った・解除された弾は、その時点の向きを保って直進する(発射方向へ戻らない。別の目標を追い直さない)。
- 変更: 操舵は、目標が弾の到達までに動く先(迎撃点。重力を受ける弾は落ちる分だけ上)を狙い、機首を速度の遅れの分だけ先へ向ける。
  山なりに撃ったときや動く目標で、前後・横に外れにくくなった(同じ `Gain` でも曲がり方が変わる)。
- 追加: `GuidanceSpec.PredictionIterations` / XML `Projectile/Guidance/PredictionIterations`(迎撃点の予測の反復回数。既定3、0は目標の今の位置を狙う)。
- 文書: 誘導の挙動を [誘導ミサイル](docs/guide/guidance.md) にまとめた。誘導は「仮実装」ではなくなったが、1.0までは挙動・係数を変えることがある。
- 追加: テスト用のコンソールコマンド `ewl_dummy`(そのピアでだけ敵として扱う標的を置く・消す)。

### 推進

- 追加: `BoosterSpec.MaxSpeed` / XML `Projectile/Booster/MaxSpeed`(推力で加速する上限の速さ [m/s]。既定0は上限なし)。

## [0.1.0] - 2026-10-17

最初の公開版(α版)です。この版でできることをまとめます。

### XMLカスタムモジュール

- `EwlWeapon`: ブロックを武装にする。発射キー、連射・切替式、発射間隔・遅延・拡散・反動、バースト、総弾数・弾倉・リロード、
  弾の物理・コライダー・見た目、ダメージ・衝撃力・命中物にくっつく弾、時限信管・起爆遅延、爆発、推進(ロケット)・安定翼(風見トルク)、
  発射・弾・爆発のエフェクトと音、待機弾の表示(ビルド中も)。発射レート・遅延・時限信管・弾倉容量・Power(初速)・ブースターの推力・燃焼時間はスライダーで変えられる。1ブロックに複数書ける。
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

[Unreleased]: https://github.com/EEX-bsg/EsWeaponsLib/compare/0.2.1...HEAD
[0.2.1]: https://github.com/EEX-bsg/EsWeaponsLib/releases/tag/0.2.1
[0.2.0]: https://github.com/EEX-bsg/EsWeaponsLib/releases/tag/0.2.0
[0.1.0]: https://github.com/EEX-bsg/EsWeaponsLib/releases/tag/0.1.0
