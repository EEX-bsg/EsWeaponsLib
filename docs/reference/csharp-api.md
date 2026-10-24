# C# APIリファレンス

> [!NOTE]
> このページは生成物です(`tools/gen-reference.ps1`)。手で編集せず、`src/` の `///` コメントを直して作り直してください。

名前空間 `EWL.Api` の公開型です。使い方は [C#で使う](../guide/csharp.md) を参照してください。

- [Weapon](#weapon) — 武装部品。
- [WeaponSpec](#weaponspec) — 武装1基の定義(純データ)。
- [ProjectileSpec](#projectilespec) — 弾1種類の定義(純データ)。
- [ExplosionSpec](#explosionspec) — 爆発1回の定義(純データ)。
- [BoosterSpec](#boosterspec) — 弾の推進(ロケット)と安定翼。
- [GuidanceSpec](#guidancespec) — 誘導。
- [ColliderShape](#collidershape) — 弾のコライダー1個。
- [ColliderKind](#colliderkind) — 弾のコライダーの形。
- [ProjectileHit](#projectilehit) — 弾の命中1回分(`Weapon.Hit` / `Weapon.AnyHit` の引数)。
- [HitKind](#hitkind) — 命中の種類。
- [Attacker](#attacker) — 攻撃者のタグ(誰の弾か・誰が起爆させたか)。
- [BundleAssets](#bundleassets) — `ModAssetBundle` からアセットを取り出す。
- [HitSource](#hitsource) — どの武器の弾か。
- [PenetrationMode](#penetrationmode) — 貫通(`ProjectileSpec.Penetration`)で厚みをどう数えるか。

## Weapon

`public sealed class EWL.Api.Weapon`

武装部品。利用側(BlockModuleBehaviour 等)がシミュ開始で `Weapon.Create` し、シミュ停止で `Weapon.Destroy` する。いつ撃つかは利用側が決めて `Weapon.TryFire` を呼ぶ。弾体・着弾・爆発・エフェクトは EWL が行う。

| メンバー | 説明 |
|---|---|
| `static Weapon Create(BlockBehaviour block, WeaponSpec spec)` | ブロックに武装を付ける。1ブロックに複数付けてよい。 |
| `static Weapon[] GetAll(GameObject block)` | ブロックに付いている EWL の武装(他 Mod が付けたものを含む)。 |
| `static GameObject CreateLoadedProjectilePreview(BlockBehaviour block, WeaponSpec spec)` | ビルド中のブロックに、装填中の弾の見た目(`WeaponSpec.ShowLoadedProjectile` と同じもの)を置く。見た目だけで当たり判定はない。表示するかは呼び出し側が決める(Spec の ShowLoadedProjectile は見ない)。位置・弾の形はこの時の Spec で決まり、後から Spec を書き換えても追従しない。シミュ中は表示されない(シミュ側で見せるのは `WeaponSpec.ShowLoadedProjectile`)。ブロックと一緒に消え、途中で消すときは戻り値を Destroy する。 |
| `Transform FindTarget(Vector3 aimOrigin, Vector3 aimDirection, float maxAngle)` | 照準(位置 `aimOrigin` と向き `aimDirection`)から `maxAngle` \[度] 以内にいる敵マシンのコアブロック(の Transform)のうち、照準の向きとのなす角が最も小さいものを返す(距離では選ばない)。無ければ null。敵 = この武装のブロックのマシン以外で、観戦中でなく、グローバルシミュ中のプレイヤーのマシン。ブロックにチームがあれば同じチームは除く(チーム無しは全員が敵)。SP・ローカルシミュには他人がいないので null(コンソールコマンド ewl_dummy で置いたテスト用のダミー標的は、このピアでだけ敵として選ばれる)。どのピアでも呼べ、毎フレーム呼んでよい。目標の指定は `Weapon.Target` で行う。 |
| `bool TryFire()` | 発射を要求する。実行権威側(SP・MP Host・MP Client ローカルシミュ)で、間隔・弾数・リロード中でないことが許すときだけ受け付けて true。BurstCount が 2 以上なら、この1回でバーストを撃つ。 |
| `bool TryReload()` | リロードを要求する。実行権威側で、弾倉を使い、弾倉に空きがあり、予備弾があり、リロード中でないときだけ受け付けて true。 |
| `void Destroy()` | 武装を外す。飛んでいる弾はシミュ終了まで残る。 |
| `WeaponSpec Spec { get; set; }` | 定義。発射のたびに読まれるので、書き換えると次の弾から反映される。ただし総弾数(TotalAmmo)と弾倉の初期の詰め方は `Weapon.Create` の時だけ使う。 |
| `int Ammo { get; }` | すぐ撃てる残弾(弾倉を使う武装は弾倉内の弾)。負値は無制限。 |
| `int ReserveAmmo { get; }` | 弾倉の外の予備弾。負値は無制限。弾倉を使わない武装は 0。 |
| `bool IsReloading { get; }` | リロード中なら true(撃てない)。 |
| `float FireCooldownRemaining { get; }` | 発射間隔・StartDelay で次の発射を受け付けるまでの残り \[s]。0 なら待ちなし(弾切れ・リロードは含まない)。`WeaponSpec.KeepFireIntervalOnRespawn` で引き継いだ待ちを含む。MP で発射の権威が無いピアでは、届いた発射から数えた目安。 |
| `string PoolKey { get; }` | この武装が使う弾のプールのキー。`ProjectileSpec.PoolKey` が空ならこの武装専用のキー。破棄済みなら null。プールの置き場はシーン直下の "EsWeaponsLib (Scene)/ProjectilePool/(キー)" で、待機中・飛行中の弾はその子(命中物にくっついた弾はその命中物の子)。弾に直接触るのは推奨しない(足りない操作は API への追加を提案してほしい)。置き場の構成は 1.0 まで変わりうる。 |
| `Transform Target { get; set; }` | 誘導の目標(敵マシンのコアブロックの Transform。`Weapon.FindTarget` で探す)。`Weapon.TryFire` が発射を受け付けた時点の値で、その発射で出る弾(遅延・バーストで後から出る弾を含む)の目標が確定する。確定した後に変えても、発射待ち・飛行中の弾には効かない。null、または発射の受付時に有効な敵のコアでない(味方・壊れた・リスポーンした・任意の物体等)なら、その発射の弾は無誘導で飛ぶ。`GuidanceSpec.Gain` が0の武装では使われない。MP: 撃つブロックの持ち主のピアで設定する。持ち主がクライアント(グローバルシミュ)なら実行権威のホストへ送られ、ホストは持ち主以外からの指定を受け付けず、敵のコアでない指定は null として扱う。ホスト側でクライアントのブロックの目標を決めないこと(ホストのカメラはそのクライアントの照準ではない)。読むと、このピアで最後に設定・受信した値。 |
| `bool IsAlive { get; }` | 破棄済みなら false。 |
| `event Action Fired` | 1発撃つ(弾数を消費する)たび。バーストでは弾ごと。弾の生成は遅延の後。 |
| `event Action ReloadStarted` | リロードを始めたとき。弾倉はリロード時間の後に満たされる。 |
| `event Action<ProjectileHit> Hit` | この武装の弾が何かに当たった(直撃・爆風)。実行権威側(SP・MP ホスト・ローカルシミュ)でだけ発火する。 |
| `static event Action<ProjectileHit> AnyHit` | 弾が当たるたびに、撃った武装を問わず発火する。被弾した側が撃った相手を知るために使う。対象は EWL の弾(EWL を使う他 Mod の武装を含む)と、一部のバニラ武装の弾(大砲・クロスボウ・FuelCannon。ゴッドツール「爆発する砲弾」の爆風を含む)。どれかは `ProjectileHit.Source` で分かる。それ以外のバニラ武装(散弾砲・火炎放射器等)は今後対応することがある。他の仕組みの Mod の弾は対象外。実行権威側でだけ発火する。静的イベントなので、購読したら必ず解除する。 |

## WeaponSpec

`public sealed class EWL.Api.WeaponSpec`

武装1基の定義(純データ)。発射機そのものの値と、撃つ弾の定義 `WeaponSpec.Projectile` を持つ。`Weapon` は発射のたびにこの値を読むので、発射前に書き換えればその弾から反映される(スライダー連動など)。全項目に既定値あり。位置・回転はブロックローカルで、回転はオイラー角 \[度]。中の定義(Projectile・Explosion・Booster・Guidance)にnullを代入しない(使わない機能は既定値のまま = 無効)。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `MuzzlePosition` | `Vector3` | `(0, 0, 0)` | 発射位置。 |
| `MuzzleRotation` | `Vector3` | `(0, 0, 0)` | 発射向き(オイラー角 \[度])。弾はこの回転の +Zへ飛ぶ。 |
| `MuzzleVelocity` | `float` | `100` | 初速 \[m/s]。ブロックの速度に加算される。 |
| `EjectVelocity` | `Vector3` | `(0, 0, 0)` | 発射時に弾のローカル軸で足す速度 \[m/s](ロケットを発射筒から押し出す等)。反動には含めない。 |
| `FireInterval` | `float` | `0.5` | 最短発射間隔 \[s]。 |
| `FireDelay` | `float` | `0` | 発射要求から弾が出るまでの固定遅延 \[s]。 |
| `FireDelayRandomExtra` | `float` | `0` | 遅延に加える乱数幅 \[s](0〜この値)。 |
| `Spread` | `float` | `0` | 拡散 \[度]。前方にtan(Spread) 半径の球内の乱数ベクトルを足して方向を決める。弾の向きもこの方向に合わせる。 |
| `RecoilMultiplier` | `float` | `1` | 反動の倍率。弾の運動量 × この値の撃力をブロックへ逆向きに与える(1で物理的に正しい反動)。 |
| `BurstCount` | `int` | `1` | 1回の発射要求で撃つ弾数。1発ごとに弾数を消費し、弾切れで止まる。 |
| `BurstInterval` | `float` | `0.1` | バースト内の弾の間隔 \[s]。次の発射要求は最後の弾からFireInterval後に受け付ける。 |
| `StartDelay` | `float` | `0` | シミュ開始・リスポーンのたびに撃てない時間 \[s]。0で待たない。FireIntervalとは独立で、発射間隔の残り(`WeaponSpec.KeepFireIntervalOnRespawn` で引き継いだもの)とは足さずに長い方を待つ。武装の生成時だけ読む。 |
| `KeepFireIntervalOnRespawn` | `bool` | `false` | trueなら、レベルロジックのリスポーン(リスポーンボックス・スポーンボックス等)で、直前の発射から数えた発射間隔の残りを引き継ぐ(リスポーンでリロードを踏み倒せない)。通常のシミュ停止・開始では引き継がない。武装の生成時だけ読む。 |
| `TotalAmmo` | `int` | `-1` | 総弾数(弾倉内を含む)。負値で無制限。マシンの無限弾薬設定時は消費しない(弾倉を使う武装は弾倉の弾だけ減り、予備弾は減らない)。武装の生成時だけ読む。 |
| `MagazineCapacity` | `int` | `0` | 弾倉の容量。0以下なら弾倉なし。武装の生成時にTotalAmmoから弾倉を満たし、残りを予備弾にする。 |
| `ReloadTime` | `float` | `1` | リロード時間 \[s]。リロード中は撃てない。 |
| `AutoReload` | `bool` | `true` | 弾倉が空になったら自動でリロードする。 |
| `ShowLoadedProjectile` | `bool` | `false` | 撃てる状態の間、次の弾の見た目を発射位置に表示する(迫撃砲・爆弾など)。発射の瞬間に消え、発射間隔の後に戻る。弾切れ・リロード中は表示しない。見た目だけでコライダーは持たない。EwlWeapon モジュールでは、総弾数が 0 でなければビルド中も表示する(C# から武装を作る場合は `Weapon.CreateLoadedProjectilePreview`)。 |
| `MuzzleFlash` | `GameObject` | なし | 発射エフェクトのプレハブ(AssetBundle等から取得)。nullなら無し。 |
| `MuzzleFlashPosition` | `Vector3` | `(0, 0, 0)` | 発射エフェクトの位置(ブロックローカル)。発射音もここで鳴る。 |
| `MuzzleFlashRotation` | `Vector3` | `(0, 0, 0)` | 発射エフェクトの向き(オイラー角 \[度]、ブロックローカル)。 |
| `MuzzleFlashScale` | `Vector3` | `(1, 1, 1)` | 発射エフェクトの拡大率。 |
| `FireSounds` | `List<AudioClip>` | 空 | 発射音(ランダムに1つ。MuzzleFlashPositionで鳴る)。 |
| `ReloadSounds` | `List<AudioClip>` | 空 | リロード開始時の音(ランダムに1つ)。 |
| `Projectile` | `ProjectileSpec` | — | 撃つ弾。飛行中の弾は発射時のProjectileSpecを使い続けるので、差し替えは次の弾から効く(同じインスタンスの値を書き換えた場合は飛行中の弾にも効く)。弾の形は `ProjectileSpec.PoolKey` ごとに固定され、差し替えた先の弾は事前生成されず最初の発射時に作られる。 |

## ProjectileSpec

`public sealed class EWL.Api.ProjectileSpec`

弾1種類の定義(純データ)。弾体・命中・信管・見た目と、爆発・推進・誘導を持つ。複数の武装で同じインスタンスを共有してよい(書き換えると共有している全武装に効く)。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Mass` | `float` | `1` | 弾の質量 \[kg]。反動・推進の加速度と、物理的な衝突に効く(命中時に与える力はImpactForceで決まる)。 |
| `Drag` | `float` | `0` | 空気抵抗(Rigidbody.drag)。 |
| `AngularDrag` | `float` | `0` | 回転の抵抗(Rigidbody.angularDrag)。 |
| `IgnoreGravity` | `bool` | `false` | trueなら重力を受けない。 |
| `CollisionMode` | `CollisionDetectionMode` | `ContinuousDynamic` | 衝突検出の方式。速い弾がすり抜けないよう、既定はContinuousDynamic。 |
| `Lifetime` | `float` | `20` | 寿命 \[s]。着弾しなくてもこの時間で消える。 |
| `ColliderEnableDelay` | `float` | `0.02` | 発射後、弾のコライダーを有効にするまでの時間 \[s]。自機との干渉を防ぐ。初速の遅い弾(ロケット・ミサイル・爆弾等)は長めにする。0なら発射と同時に有効(負値は0扱い)。撃った武装ブロックとはこの値に関係なく衝突しない。 |
| `Colliders` | `List<ColliderShape>` | 空 | コライダー(弾ローカル)。空なら半径0.1の球。 |
| `Friction` | `float` | `0.6` | 弾の摩擦係数(静止・動摩擦とも)。 |
| `Bounciness` | `float` | `0` | 弾の反発係数(0〜1)。 |
| `FrictionCombine` | `PhysicMaterialCombine` | `Average` | 相手の摩擦との合成方法。 |
| `BounceCombine` | `PhysicMaterialCombine` | `Average` | 相手の反発との合成方法。 |
| `PoolSize` | `int` | `50` | 同時に飛べる弾の上限。武装の生成時にこの数まで弾を事前生成し、上限に達したら最古の弾を回収して撃つ。エフェクトもプレハブごとにこの数まで(超えた分は表示しない)。1〜1000に丸める。 |
| `PoolKey` | `string` | なし | 弾のプールを共有するキー。同じ種類の弾で同じ値にすると、弾のオブジェクトを使い回す。弾の形(Colliders)はキーごとに最初に作ったもので固定される。nullか空文字なら武装ごとの専用プール(実際のキーは `Weapon.PoolKey`)。 |
| `Mesh` | `Mesh` | なし | 見た目。nullなら球を表示。 |
| `Texture` | `Texture` | なし | 見た目のテクスチャ。nullならテクスチャなし(白)。 |
| `MeshPosition` | `Vector3` | `(0, 0, 0)` | 見た目の位置(弾ローカル)。Meshがnullのときは使わない。 |
| `MeshRotation` | `Vector3` | `(0, 0, 0)` | 見た目の向き(オイラー角 \[度]、弾ローカル)。Meshがnullのときは使わない。 |
| `MeshScale` | `Vector3` | `(1, 1, 1)` | 見た目の拡大率。Meshがnullのときは使わない。 |
| `Trail` | `GameObject` | なし | 弾に追従し、命中・消滅で切り離されてフェードするエフェクトのプレハブ。 |
| `AttachedEffect` | `GameObject` | なし | 弾に追従し、弾と一緒に消えるエフェクトのプレハブ(弾の光・炎など)。 |
| `ImpactForce` | `float` | `10` | 命中した剛体に「衝突直前の弾の速度 × この値」の力 \[N/(m/s)] を与える。ダメージ・力は最初の命中で1回だけ。 |
| `BlockDamage` | `float` | `0` | 直撃したとき、HPを持つブロックに与えるダメージ(爆風では使わない)。 |
| `EntityDamage` | `float` | `0` | 直撃したとき、レベルのエンティティ(人・動物など)に与えるダメージ(爆風では使わない)。 |
| `AttachOnHit` | `bool` | `false` | 命中物にくっついて止まり、以後は命中物と一緒に動く(矢など)。地形では、その場で止まる。 |
| `Penetration` | `float` | `0` | 貫通できる装甲の厚みの上限 \[m]。0なら貫通しない。装甲はマシンのブロックだけで、地形・レベルの物体には普通の弾として当たる(物理の衝突で跳ねる)。マシンのブロックに命中すると弾の進む向きに装甲を通り抜け、通った厚みの合計がこの値に達するまで貫通する。弾は装甲の中では止まらない。命中したブロック自体を抜けられないときは貫通せず、貫通しない弾と同じく普通に当たる(物理の衝突で装甲を押す)。厚みは進む向きに沿って測るので、斜めの装甲ほど厚い。上限に達する前に装甲を抜けたら、弾は出口の外へ出て命中直前の速度・向きのまま飛び続け、残りの貫通量で次の装甲も貫通する(爆発する弾は抜けた所で起爆を始める。起爆遅延の間は飛び続ける)。手前の装甲を抜けて奥のブロックで上限に達したら、そのブロックの入口の手前から命中直前の速度・向きで飛び続け、そのブロックには普通の弾として当たる。弾が入れない狭い隙間(0.05mか弾の長さの広い方まで)の先の装甲は、抜けずに続けて数える。貫通して置き直された弾は、その位置で重なるマシンのブロック(次に当たるブロックを除く)とは、弾が消えるまで衝突しない(通った装甲に穴が開いた扱い)。ダメージ・衝撃力・命中イベントは、抜けたブロックそれぞれに1回ずつ与える(命中イベントの位置はそのブロックへの入口)。 |
| `PenetrationMode` | `PenetrationMode` | `Total` | 貫通で厚みをどう数えるか。既定の `PenetrationMode.Total` は通った装甲の合計。 |
| `DespawnOnHit` | `bool` | `false` | 爆発しない弾を命中時に即消す。falseなら命中の1秒後に消える。 |
| `TimeFuse` | `float` | `INF` | 時限信管 \[s]。発射からこの時間で起爆(爆発しない弾は消える)。0は発射直後に起爆。既定は無限(時限信管なし)。 |
| `TimeFuseRandomExtra` | `float` | `0.05` | 時限信管に加える乱数 \[s](0〜この値)。発射時に決める。TimeFuseが無限のときは効かない。 |
| `DetonationDelay` | `float` | `0` | 命中から起爆までの遅延 \[s]。 |
| `DetonationDelayRandomExtra` | `float` | `0` | 起爆遅延に加える乱数幅 \[s](0〜この値)。 |
| `Explosion` | `ExplosionSpec` | — | 起爆したときの爆発。Radiusが0なら爆発しない。 |
| `Booster` | `BoosterSpec` | — | 推進(ロケット)。Thrustが0なら推進しない。 |
| `Guidance` | `GuidanceSpec` | — | 誘導。Gainが0なら誘導しない。目標は `Weapon.Target` で指定する。 |

## ExplosionSpec

`public sealed class EWL.Api.ExplosionSpec`

爆発1回の定義(純データ)。範囲内の剛体にAddExplosionForceと本体の爆発処理を与える。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Radius` | `float` | `0` | 半径 \[m]。0なら爆発しない。 |
| `Force` | `float` | `0` | AddExplosionForceのexplosionForce。 |
| `UpwardsModifier` | `float` | `0` | AddExplosionForceのupwardsModifier。 |
| `OnlyWithExplodingCannonballs` | `bool` | `false` | 通常は爆発しない弾として扱い、ゴッドツール「爆発する砲弾」が有効なときだけ爆発する。 |
| `ForwardOffset` | `float` | `0` | 爆発地点を弾の前方(機首の向き)へずらす距離 \[m]。爆風・エフェクト・音すべてに効く。0なら起爆した弾の位置(HEAT のメタルジェット等を表すときに使う)。 |
| `Effect` | `GameObject` | なし | 爆発エフェクトのプレハブ。nullなら無し。 |
| `FacesHitNormal` | `bool` | `false` | 爆発エフェクトを命中面の法線へ向ける。 |
| `Sounds` | `List<AudioClip>` | 空 | 爆発音(ランダムに1つ)。 |

## BoosterSpec

`public sealed class EWL.Api.BoosterSpec`

弾の推進(ロケット)と安定翼。Thrustが0なら推進しない。命中したら推力・安定翼とも止まる。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Thrust` | `float` | `0` | 推力 \[N]。弾の前方へ毎物理フレーム加える(加速度 = Thrust / 弾のMass)。 |
| `ThrustTime` | `float` | `INF` | 推力を出す時間 \[s]。発射からこの時間で推力が止まり(燃料切れ)、その後は慣性で飛ぶ(安定翼・誘導は働き続ける)。0は推力なし。既定は無限(弾が消えるまで)。発射時の値を使い、飛行中の弾は変えない。 |
| `Stability` | `float` | `0` | 風見安定(安定翼)の強さ \[(rad/s²)/(m/s)]。機首に垂直な速度成分 × この値の角加速度で、機首を飛ぶ向きへ寄せる(弾の質量・形に依らない)。横滑りが大きいほど強く戻り、重力で曲がる弾道にも機首が沿う。推力の有無・燃焼の終了に関係なく、命中まで飛行中ずっと働く。0なら働かない。速度そのものは変えないので、横滑りは機首が向き直って推力が掛かるか、`BoosterSpec.Lift`が働くことで消える。振動を抑えるには`ProjectileSpec.AngularDrag`を併用する。 |
| `Lift` | `float` | `0` | 揚力(胴体・翼が横滑りを打ち消す力)の強さ \[1/s]。機首に垂直な速度成分 × この値の加速度を、その成分を打ち消す向きに加え、飛ぶ向きを機首の方へ寄せる(弾の質量・形に依らない。速さは変えず、横滑りの分だけ減る)。推力の有無・燃焼の終了に関係なく、命中まで飛行中ずっと働くので、燃料が切れた後も誘導で曲がれる。0なら働かない。`BoosterSpec.Stability`(機首を飛ぶ向きへ寄せる)と併用すると、機首と飛ぶ向きが互いに寄り、横滑りしながら曲がる。物理の刻み1回で横滑りを打ち消し切る値(既定の刻み0.01秒なら100)より大きくしても、それ以上は強くならない。 |
| `MaxSpeed` | `float` | `0` | 推力で加速する上限の速さ \[m/s]。弾の速さがこの値以上の間は推力を出さない(最高速度。重力・爆風などで超えることはある)。0なら上限なし(抗力 `ProjectileSpec.Drag` と推力の釣り合いだけで決まる)。 |

## GuidanceSpec

`public sealed class EWL.Api.GuidanceSpec`

誘導。1.0までは挙動・係数を変えることがある(変えたらCHANGELOGに書く)。目標は `Weapon.Target` で指定し、`Weapon.TryFire` が発射を受け付けた時点の値で確定する(遅延・バーストで後から出る弾も同じ目標)。飛行中の弾の目標は変わらず、見失う(破壊・リスポーン・シミュ終了)か解除されると無誘導になり、別の目標を拾い直さない。発射から0.2秒は発射方向を保ち、その後は弾の向きをトルクで回す。狙うのは目標が弾の到達までに動く先(迎撃点。重力を受ける弾は落ちる分だけ上)で、速度は機首より遅れて曲がるので、機首は迎撃点への向きより速度のずれの分だけ先へ向ける。無誘導の弾は向きを保って直進する(目標なしで撃った弾は発射方向、見失い・解除の後はその時点の向き。発射方向へは戻らない)。向きを変えるだけなので、推進(`BoosterSpec`)と組み合わせて使う。無誘導になった後も推進・安定翼・抗力・重力・寿命はそのまま働く。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Gain` | `float` | `0` | 誘導の強さ(旋回トルクの係数)。0なら誘導しない。 |
| `PredictionIterations` | `int` | `3` | 迎撃点の予測の反復回数。到達時間を「距離 ÷ 速さ」で見積もって目標を動かし、その点で測り直すのを繰り返す(近似。多いほど動く目標に正確)。0なら目標の今の位置を狙う。物理フレームごとに毎回計算するので、大きくしすぎない。 |
| `ReleaseAngle` | `float` | `0` | 解除角 \[度]。弾の機首と「弾から目標への向き」のなす角がこの値を超えたら誘導を解除する(外れて目標を通り過ぎた弾が引き返さないように)。機首を軸にした円錐の半頂角で、90なら前方の半球にいる限り追い続ける。発射直後から判定する。0なら角度では解除しない。 |

## ColliderShape

`public sealed class EWL.Api.ColliderShape`

弾のコライダー1個。SizeはBoxの大きさ、RadiusはSphere/Capsule用、Height/Direction(0=X,1=Y,2=Z)はCapsule用。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Kind` | `ColliderKind` | `Sphere` | 形。 |
| `Position` | `Vector3` | `(0, 0, 0)` | 位置(弾ローカル)。 |
| `Rotation` | `Vector3` | `(0, 0, 0)` | 向き(オイラー角 \[度]、弾ローカル)。 |
| `Size` | `Vector3` | `(1, 1, 1)` | Boxの大きさ \[m]。 |
| `Radius` | `float` | `0.1` | Sphere・Capsuleの半径 \[m]。 |
| `Height` | `float` | `1` | Capsuleの高さ \[m]。 |
| `Direction` | `int` | `2` | Capsuleの向きの軸(0 = X、1 = Y、2 = Z)。 |

## ColliderKind

`public enum EWL.Api.ColliderKind`

弾のコライダーの形。

| 値 | 説明 |
|---|---|
| `Sphere` |  |
| `Capsule` |  |
| `Box` |  |

## ProjectileHit

`public sealed class EWL.Api.ProjectileHit`

弾の命中1回分(`Weapon.Hit` / `Weapon.AnyHit` の引数)。直撃は1回、爆風は剛体ごとに1回。撃った側の情報は発射時点のもの。どの武器の弾かは `ProjectileHit.Source`(`Weapon.AnyHit` はバニラの一部の武装も含む)。

| メンバー | 説明 |
|---|---|
| `HitSource Source { get; }` | どの武器の弾か。今後種類が増えうるので、知らない値は無視する。 |
| `Weapon Weapon { get; }` | 撃った武装(EWLの弾だけ。バニラの弾はnull)。弾が飛んでいる間に破棄されていれば `Weapon.IsAlive` がfalse。 |
| `BlockBehaviour ShooterBlock { get; }` | 撃ったブロック(シミュ用。EWLの弾だけで、バニラの弾はnull)。壊れた・シミュが終わった後はnull。 |
| `Attacker Shooter { get; }` | 撃った人(発射時点の値。撃ったブロックが消えても残る)。バニラの弾で分からないときはnull。 |
| `BlockBehaviour Block { get; }` | 当たったブロック(子ブロックを持つブロックは当たった子)。地形・エンティティ等はnull。 |
| `Collider Collider { get; }` | 当たったコライダー(直撃のみ。爆風と、当たったコライダーが分からないバニラの弾はnull)。 |
| `Vector3 Point { get; }` | 直撃は接触点、爆風は爆発の中心(ワールド座標)。 |
| `HitKind Kind { get; }` | 直撃か爆風か。今後種類が増えうるので、知らない値は無視する。 |

## HitKind

`public enum EWL.Api.HitKind`

命中の種類。

| 値 | 説明 |
|---|---|
| `Direct` | 弾が直接当たった。 |
| `Explosion` | 爆風が当たった。 |

## Attacker

`public sealed class EWL.Api.Attacker`

攻撃者のタグ(誰の弾か・誰が起爆させたか)。発射・起爆の時点の値なので、撃った人がリスポーン・観戦・退出しても変わらない。記録は実行権威側(SP・MPホスト・ローカルシミュ)だけで行う。キル・デスに数えるかは受け取る側が決める。

| メンバー | 説明 |
|---|---|
| `static Attacker GetDetonator(BlockBehaviour explosive)` | 爆発済みのバニラのボムを起爆させた人。分からない・まだ爆発していない・ボム以外・実行権威側でないときはnull。誘爆の連鎖は根本の原因までたどる。EWLの弾の直撃・爆風(その中で起きた誘爆を含む)はその撃ち手、バニラの矢・砲弾の直撃、ゴッドツール「爆発する砲弾」の爆風、発射済みロケットの衝突はその持ち主、落下・体当たり等のブロック衝突はボムの持ち主(自爆)。位置では推定しないので、グレネード・ロケット・燃料樽の爆風、着火、他Modの弾で起爆したものは不明(一部は今後対応することがある)。 |
| `ushort PlayerId { get; }` | プレイヤーのNetworkId。SPは0。退出した人の番号は後から参加した人に再利用されうる(同一人物かは `Attacker.Player` で比べる)。 |
| `MPTeam Team { get; }` | チーム。撃ったブロックが分かればそのブロックのチーム(シミュ開始時の値)、分からなければその時点のプレイヤーのチーム。SPは `MPTeam.None`。 |
| `Player Player { get; }` | MPのプレイヤー。接続ごとに別の参照なので、番号が再利用されても `Equals` で別人と分かる。退出後も `Name` は読める。SPはnull。 |

## BundleAssets

`public static class EWL.Api.BundleAssets`

`ModAssetBundle` からアセットを取り出す。本体の `LoadAsset` は呼ぶたびにバンドルの全アセットを読む(空バンドル検出)ので、結果をバンドル・名前・型ごとに覚える。見つからなかった名前も覚え、2回目以降は読まない。

本体はバンドルをMod発見時に非同期で1回だけ読み、通常は解放しない。シミュ開始が読込完了より早いこともある(起動直後のMP参加など)。未読込(`!bundle.Loaded`)なら `bundle.OnLoad` を購読して完了後に取り直し、シミュ停止で購読を解除する。読込済みのバンドルの `OnLoad` を購読すると本体が全アセットを読み直すので、購読は未読込のときだけにする。

| メンバー | 説明 |
|---|---|
| `static T Load<T>(ModAssetBundle bundle, string name)` | 未読込・読込エラー・見つからないときはnull。メインスレッドから呼ぶ。シミュ開始時などに取り出して持っておき、毎フレームは呼ばない。 |

## HitSource

`public enum EWL.Api.HitSource`

どの武器の弾か。

| 値 | 説明 |
|---|---|
| `Ewl` | EWLの武装(XMLカスタムモジュール・C# API。EWLを使う他Modのものを含む)。 |
| `VanillaCannon` | Besiege標準の大砲の砲弾(散弾砲は含まない)。ゴッドツール「爆発する砲弾」の爆風を含む。 |
| `VanillaCrossbow` | Besiege標準のクロスボウの矢。 |
| `VanillaFuelCannon` | Besiege標準のFuelCannonの火球。当たった相手はEWLが本体と同じ判定をやり直して求めるので、まれに本体とずれることがある。 |

## PenetrationMode

`public enum EWL.Api.PenetrationMode`

貫通(`ProjectileSpec.Penetration`)で厚みをどう数えるか。

| 値 | 説明 |
|---|---|
| `Total` | 通った装甲の厚みの合計で数える(接した装甲は続けて抜ける)。合計が上限に達したブロックには、その入口から普通の弾として当たる。 |
| `FirstBlock` | 当たったブロックだけを数える。厚みが上限を超えるなら貫通せず普通に当たり、抜けたら飛び続ける。貫通は1ブロックだけで、抜けた後は普通の弾として当たる(次のブロックは貫通せず、物理の衝突で跳ねる)。裏に別のブロックが接して(弾が入れない隙間で)いれば、前端が裏面の手前に来る所から飛び続け、そのブロックに物理の衝突で当たる。 |
