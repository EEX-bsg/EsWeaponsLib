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
  `HoldToFireToggle`、`Muzzle`、`Fire`(Velocity/Interval/Hold/Spread/Delay/DelayRandomExtra/Recoil/Ammo)、
  `Projectile`(Mass/Drag/AngularDrag/IgnoreGravity/CollisionMode/Lifetime/TimeFuse/TimeFuseRandomExtra/Friction/Bounciness/FrictionCombine/BounceCombine/
  Colliders/Mesh/Texture)、`Impact`(BlockDamage/EntityDamage/ImpactForce/Attach/ExplosionRadius/ExplosionPower/ExplosionUpPower/
  DetonationDelay/DetonationDelayRandomExtra/DespawnOnHit)、`Effects`(AssetBundle/ShotFlash+Position/Rotation/Scale/Trail/Bullet/Explosion/
  ExplosionFacesNormal/FireSounds/ExplosionSounds)。
- バースト・弾倉・リロード。`FireSpec.BurstCount`(既定 1)/`BurstInterval`(既定 0.1 秒)/`MagazineCapacity`(既定 0 = 弾倉なし)/
  `ReloadTime`(既定 1 秒)/`AutoReload`(既定 true)、`EffectSpec.ReloadSounds`、`Weapon.ReserveAmmo` / `IsReloading` / `TryReload()` /
  `ReloadStarted`。弾倉を使う武装では `Weapon.Ammo` は弾倉内の弾、`FireSpec.Ammo` は弾倉を含む総弾数。`Fired` はバーストの1発ごと。
  無限弾薬では弾倉は減り(リロードは要る)、予備弾は減らない。
  `EwlWeapon` に `ReloadKey` / `AutoReloadToggle` / `FireDelaySlider`(値を Fire/Delay に加算)/ `TimeFuseSlider`(値を Projectile/TimeFuse に使う。どちらも発射ごとに読み、時限信管は発射時の値で固定)、
  `Fire` に BurstCount/BurstInterval/MagazineCapacity/ReloadTime/AutoReload、`Effects` に ReloadSounds を追加。
- XML モジュール `EwlBlockPhysics`(仮称)。全要素省略可で、省略した項目は本体の値のまま。
  `JointStrength`(接合の破断力・破断トルク。0 = 変えない)、`Material`(Friction/Bounciness/FrictionCombine/BounceCombine。
  書けばブロックの全コライダーに適用)、`Rotation`(AxisX/AxisY/AxisZ = Locked/Limited/Free、Spring/Damper/Limit[度])。
- `EwlBlockPhysics/AdditionalColliders`(省略可)。ブロックにコライダーを足す。書式はブロックの `Colliders` と同じ
  (SphereCollider/CapsuleCollider/BoxCollider、`layer`・`trigger` 属性)で、BoxCollider は `Center`(Unity の BoxCollider.center)も書ける。
  `layer="12"` で他ブロックを置けて接続される面になる(BoxCollider は6面のどこでも置け、
  クリックした点を面の中心から1マス単位に丸めた位置に置かれる。Sphere/Capsule はコライダーの Position + 法線 × 0.5)。`Material` を書けばこれらにも適用。
- 弾とエフェクトのプール。`ProjectileSpec.PoolSize`(既定 50。同時に飛べる弾の上限で、武装の生成時にこの数まで事前生成し、
  上限では最古の弾を回収して撃つ。エフェクトもプレハブごとにこの数まで)、`ProjectileSpec.PoolKey`(弾のオブジェクトを共有するキー。
  null なら武装ごと)。XML は `EwlWeapon/Projectile/PoolSize`。`EwlWeapon` モジュールは同じブロック種別で弾を共有する。
- `BundleAssets.Load<T>(ModAssetBundle, string)`。AssetBundle からアセットを取り出し、バンドル・名前・型ごとに覚える
  (見つからなかった名前も覚える)。未読込・読込エラー・見つからないときは null。未読込時の待ち方は型の説明を参照。
- `ProjectileSpec.ShowWhenLoaded`(既定 false)/ XML `EwlWeapon/Projectile/ShowWhenLoaded`。撃てる状態の間、次の弾の見た目を
  発射位置に表示する(発射の瞬間に消え、発射間隔の後に戻る。弾切れ・リロード中は表示しない)。
- 弾の推進(ロケット)。`WeaponSpec.Booster`(`BoosterSpec`: `Thrust` [N]、`BurnTime` [s、0 = 消えるまで]、`LateralDrag`)/
  XML `EwlWeapon/Booster/Thrust|BurnTime|LateralDrag`。推力は弾の前方へ毎物理フレーム、命中で止まる。
  `FireSpec.EjectVelocity` / XML `Fire/EjectVelocity`(弾ローカル軸で足す初速。発射筒から押し出す等)。
- MP 同期。MP ホストのグローバルシミュで撃った弾を、クライアントでは見た目だけの弾(当たり判定なし)として表示する。
  発射エフェクト・音、爆発エフェクト・音、`Weapon.Ammo` / `ReserveAmmo` / `IsReloading`、`Fired` / `ReloadStarted` もクライアントに届く。
  利用者は同期コードを書かない。条件: `Weapon` を**全ピアで、ブロックごとに同じ順で**生成する(シミュ開始時に作れば満たす)。
  ローカルシミュの弾は他のプレイヤーに配信しない。
- 命中イベント `Weapon.Hit`(その武装の弾)と静的 `Weapon.AnyHit`(全武装)。引数 `ProjectileHit`(撃った武装・ブロック・撃った人 `Shooter`、
  当たったブロック・コライダー・位置、爆風か)。直撃は1回、爆風は剛体ごとに1回。実行権威側(SP・MP ホスト・ローカルシミュ)でだけ発火。
- ゴッドツール「爆発する砲弾」対応。`ImpactSpec.ExplodesOnlyWithGodTool` / XML `Impact/ExplodesOnlyWithGodTool`(既定 false)。
  true の弾は通常は爆発せず、発射者のマシンでこのゴッドツールが有効なときだけ `ExplosionRadius` 等で爆発する(判定は発射時)。
  爆発したとき・無限弾薬で撃ったときは本体と同じく「ゴッドツール使用済み」を記録する。
- 攻撃者のタグ `Attacker`(`PlayerId` / `Team` / `Player`)。発射・起爆の時点の値なので、撃った人がリスポーン・観戦・退出しても読める。
  `Player` は接続ごとに別の参照で、退出した人の番号が再利用されても別人と区別できる(SP は null)。
- `Attacker.GetDetonator(BlockBehaviour)`: 爆発済みのバニラのボムを起爆させた人(不明なら null。実行権威側だけ)。誘爆の連鎖は根本までたどる。
  EWL の弾の直撃・爆風(その中で起きた誘爆を含む)はその撃ち手、バニラの矢・砲弾の直撃と発射済みロケットの衝突はその持ち主、落下・体当たり等のブロック衝突はボムの持ち主。
  位置では推定しないので、グレネード・ロケット・燃料樽の爆風、着火、他 Mod の弾で起爆したボムは不明。キル・デスの判定は利用側の責務。
- コンソールコマンド `ewl_detonations`: このシミュで爆発したボムと起爆させた人・判定の経路を出す(確認用)。
- `Weapon.PoolKey`: この武装が使う弾のプールのキー(`PoolKey` 未指定なら武装専用のキー)。置き場は
  `EsWeaponsLib (Scene)/ProjectilePool/(キー)`。弾に直接触るのは非推奨で、置き場の構成は 1.0 まで変わりうる。
- XML の再読込(ブロック定義 Mod の `Mod.xml` が `<Debug>true</Debug>` のとき、ゲーム中に XML を保存すると反映)。
  次のシミュ開始から効く。対象は `EwlWeapon` の `Muzzle` / `Fire` / `Projectile` / `Impact` / `Effects` / `Booster`
  (弾のコライダー・`PoolSize` も、弾のプールを作り直して反映)と、`EwlBlockPhysics` の `JointStrength` / `Material` / `Rotation`。
  キー・トグル・スライダーの参照と `EwlBlockPhysics/AdditionalColliders` は対象外(Besiege の再起動が必要)。
  `EwlBlockPhysics` の `Material` / `Rotation` を XML から消したときは、置いてあるブロックには再起動まで前の設定が残る。

### Changed
- [Breaking] 遅延・信管の乱数幅の名前を整理し、時限信管の乱数幅を独立させた(C# と XML 共通。乱数はどれも 0〜値の上乗せ)。
  旧 `Fire.RandomDelay` → 新 `Fire.DelayRandomExtra`、旧 `Impact.FuseDelay` → 新 `Impact.DetonationDelay`、
  旧 `Impact.FuseRandom` → 新 `Impact.DetonationDelayRandomExtra`、旧 `EwlWeapon/DelaySlider` → 新 `EwlWeapon/FireDelaySlider`。
  追加 `Projectile.TimeFuseRandomExtra`(既定 0.05 秒。発射時に時限信管へ上乗せ。従来は起爆時に固定 0〜0.05 秒待っていた)。
- [Breaking] `ProjectileHit.ShooterPlayerId` / `ShooterTeam` を `ProjectileHit.Shooter`(`Attacker`)にまとめた。
  旧 `hit.ShooterPlayerId` → 新 `hit.Shooter.PlayerId`、旧 `hit.ShooterTeam` → 新 `hit.Shooter.Team`。
- EWL のオブジェクトをシーン直下に並べず、寿命で2つにまとめた。常駐する仕組みは `EsWeaponsLib`(DontDestroyOnLoad)、
  弾・エフェクトのプールは `EsWeaponsLib (Scene)` の下(飛行中の弾・ワールドのエフェクトを含む)。プールはシーンが変わると全部捨てて作り直す。
- 拡散(`Spread`)で曲げた方向に弾の向きも合わせるようにした(従来は向きは発射位置の向きのまま)。
- `EwlWeapon` の押下判定(長押し OFF の連射切替・リロードキー)を `IsHeld` の変化で取るようにした(MP ホストでクライアントの
  `IsPressed` を取りこぼさないため)。
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

### Fixed
- 撃った武装が消えた後(MP で撃った人だけがビルドに戻った等)に、飛行中の弾の見た目がマテリアルエラーになり、摩擦・反発も既定値に
  戻っていた。弾のマテリアル・物理マテリアルを武装の所有から外し、同じテクスチャ・同じ値どうしで共有するようにした。
  弾は武装が消えても最後まで飛ぶ(従来の方針どおり)。
