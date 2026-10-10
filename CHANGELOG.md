# Changelog

E'sWeaponsLib の利用者(Mod 開発者・AI エージェント)向け変更履歴です。

- 公開面 = XML モジュール名・XML 要素・C# の公開型/メンバー(一覧はリポジトリの `api-surface.txt`)。
- 既存の外部ブロック XML かコンパイル済み外部 DLL が壊れる変更は **[Breaking]** と書き、「旧 → 新」の具体例を付けます。
- 1.0 より前(α/β)は破壊的変更がありえます。1.0 以降、破壊的変更はメジャー版でのみ行います。

## [Unreleased]

### Added
- プロジェクト初期化(空のスケルトン)。
- C# API `EWL.Api`(仮称): `WeaponSpec`(`Fire` / `Projectile` / `Impact` / `Effects` の純データ)と武装部品 `Weapon`
  (`Create(BlockBehaviour, WeaponSpec)` / `TryFire()` / `Destroy()` / `Spec` / `Ammo` / `Fired` / `GetAll(GameObject)`)。
  `Spec` は発射のたびに読まれる。SP のみ(MP 同期なし)。
- XML モジュール `EwlWeapon`(仮称)。`EWL.Api` だけで実装した標準モジュール。`FireKey` 以外は省略可。
  `HoldToFireToggle`、`Muzzle`、`Fire`(Velocity/Interval/Hold/Spread/Delay/RandomDelay/Recoil/Ammo)、
  `Projectile`(Mass/Drag/AngularDrag/IgnoreGravity/CollisionMode/Lifetime/TimeFuse/Friction/Bounciness/FrictionCombine/BounceCombine/
  Colliders/Mesh/Texture)、`Impact`(BlockDamage/EntityDamage/ImpactForce/Attach/ExplosionRadius/ExplosionPower/ExplosionUpPower/
  FuseDelay/FuseRandom/DespawnOnHit)、`Effects`(AssetBundle/ShotFlash+Position/Rotation/Scale/Trail/Bullet/Explosion/
  ExplosionFacesNormal/FireSounds/ExplosionSounds)。
- XML モジュール `EwlBlockPhysics`(仮称)。全要素省略可で、省略した項目は本体の値のまま。
  `JointStrength`(接合の破断力・破断トルク。0 = 変えない)、`Material`(Friction/Bounciness/FrictionCombine/BounceCombine。
  書けばブロックの全コライダーに適用)、`Rotation`(AxisX/AxisY/AxisZ = Locked/Limited/Free、Spring/Damper/Limit[度])。
- 弾とエフェクトのプール。`ProjectileSpec.PoolSize`(既定 50。同時に飛べる弾の上限で、武装の生成時にこの数まで事前生成し、
  上限では最古の弾を回収して撃つ。エフェクトもプレハブごとにこの数まで)、`ProjectileSpec.PoolKey`(弾のオブジェクトを共有するキー。
  null なら武装ごと)。XML は `EwlWeapon/Projectile/PoolSize`。`EwlWeapon` モジュールは同じブロック種別で弾を共有する。
- `BundleAssets.Load<T>(ModAssetBundle, string)`。AssetBundle からアセットを取り出し、バンドル・名前・型ごとに覚える
  (見つからなかった名前も覚える)。未読込・読込エラー・見つからないときは null。未読込時の待ち方は型の説明を参照。

### Changed
- 発射音・爆発音を本体の作法で鳴らすようにした。本体のミキサー(発射音 Blocks・爆発音 Explosions)を通るので音量スライダー・水中のこもり・
  スロー時のピッチが効く。3D の聞こえ方(距離減衰)はバニラの大砲(発射音)・爆弾(爆発音)に揃え、ピッチは毎回 ±5% 揺らす。
  音量はクリップのまま(従来の `PlayClipAtPoint` と同じ 1.0)。
- `EwlWeapon` のエフェクト用プレハブを AssetBundle から一度だけ取り出して使い回すようにした(シミュ開始時の負荷を軽減)。
- `EwlWeapon` はシミュ開始時に AssetBundle が読込中なら、読込完了の時点でエフェクトを差し込む(従来はそのシミュ中エフェクトなし)。
- [Breaking] `EwlWeapon` のたたき台からの変更(α 内):
  - `Impact/ExplodeRadius` → `Impact/ExplosionRadius`(`ExplodePower`/`ExplodeUpPower` も同様に `Explosion*`)。
    旧 `<ExplodeRadius>2</ExplodeRadius>` → 新 `<ExplosionRadius>2</ExplosionRadius>`。
  - `Projectile/Radius` を削除。球コライダーは `<Colliders><SphereCollider><Radius>0.1</Radius></SphereCollider></Colliders>`。
  - `Fire/Hold` の既定を `true` に変更。`false` は「押すたびに連射の開始・停止を切替」(旧: 押すたび1発)。
