# C# API リファレンス

> このページは生成物です(`tools/gen-reference.ps1`)。手で編集せず、`src/` の `///` コメントを直して作り直してください。

名前空間 `EWL.Api` の公開型です。使い方は [C# で使う](../guide/csharp.md) を参照してください。

- [Weapon](#weapon) — 武装部品。
- [WeaponSpec](#weaponspec) — 武装1基の定義(純データ)。
- [ProjectileSpec](#projectilespec) — 弾1種類の定義(純データ)。
- [ExplosionSpec](#explosionspec) — 爆発1回の定義(純データ)。
- [BoosterSpec](#boosterspec) — 弾の推進(ロケット)。
- [GuidanceSpec](#guidancespec) — 誘導(仮実装)。
- [ColliderShape](#collidershape) — 弾のコライダー1個。
- [ColliderKind](#colliderkind) — 弾のコライダーの形。
- [ProjectileHit](#projectilehit) — EWL の弾の命中1回分(`Weapon.Hit` / `Weapon.AnyHit` の引数)。
- [HitKind](#hitkind) — 命中の種類。
- [Attacker](#attacker) — 攻撃者のタグ(誰の弾か・誰が起爆させたか)。
- [BundleAssets](#bundleassets) — `ModAssetBundle` からアセットを取り出す。

## Weapon

`public sealed class EWL.Api.Weapon`

武装部品。利用側(BlockModuleBehaviour 等)がシミュ開始で `Weapon.Create` し、シミュ停止で `Weapon.Destroy` する。いつ撃つかは利用側が決めて `Weapon.TryFire` を呼ぶ。弾体・着弾・爆発・エフェクトは EWL が行う。

| メンバー | 説明 |
|---|---|
| `static Weapon Create(BlockBehaviour block, WeaponSpec spec)` | ブロックに武装を付ける。1ブロックに複数付けてよい。 |
| `static Weapon[] GetAll(GameObject block)` | ブロックに付いている EWL の武装(他 Mod が付けたものを含む)。 |
| `bool TryFire()` | 発射を要求する。実行権威側(SP・MP Host・MP Client ローカルシミュ)で、間隔・弾数・リロード中でないことが許すときだけ受け付けて true。BurstCount が 2 以上なら、この1回でバーストを撃つ。 |
| `bool TryReload()` | リロードを要求する。実行権威側で、弾倉を使い、弾倉に空きがあり、予備弾があり、リロード中でないときだけ受け付けて true。 |
| `void Destroy()` | 武装を外す。飛んでいる弾はシミュ終了まで残る。 |
| `WeaponSpec Spec { get; set; }` | 定義。発射のたびに読まれるので、書き換えると次の弾から反映される。ただし総弾数(TotalAmmo)と弾倉の初期の詰め方は `Weapon.Create` の時だけ使う。 |
| `int Ammo { get; }` | すぐ撃てる残弾(弾倉を使う武装は弾倉内の弾)。負値は無制限。 |
| `int ReserveAmmo { get; }` | 弾倉の外の予備弾。負値は無制限。弾倉を使わない武装は 0。 |
| `bool IsReloading { get; }` | リロード中なら true(撃てない)。 |
| `string PoolKey { get; }` | この武装が使う弾のプールのキー。`ProjectileSpec.PoolKey` が空ならこの武装専用のキー。破棄済みなら null。プールの置き場はシーン直下の "EsWeaponsLib (Scene)/ProjectilePool/(キー)" で、待機中・飛行中の弾はその子(刺さった弾は刺さった先の子)。弾に直接触るのは推奨しない(足りない操作は API への追加を提案してほしい)。置き場の構成は 1.0 まで変わりうる。 |
| `bool IsAlive { get; }` | 破棄済みなら false。 |
| `event Action Fired` | 1発撃つ(弾数を消費する)たび。バーストでは弾ごと。弾の生成は遅延の後。 |
| `event Action ReloadStarted` | リロードを始めたとき。弾倉はリロード時間の後に満たされる。 |
| `event Action<ProjectileHit> Hit` | この武装の弾が何かに当たった(直撃・爆風)。実行権威側(SP・MP ホスト・ローカルシミュ)でだけ発火する。 |
| `static event Action<ProjectileHit> AnyHit` | どの武装の弾が当たっても発火する(他 Mod の武装を含む)。被弾した側が撃った相手を知るために使う。実行権威側でだけ発火する。静的イベントなので、購読したら必ず解除する。 |

## WeaponSpec

`public sealed class EWL.Api.WeaponSpec`

武装1基の定義(純データ)。発射機そのものの値と、撃つ弾の定義 `WeaponSpec.Projectile` を持つ。`Weapon` は発射のたびにこの値を読むので、発射前に書き換えればその弾から反映される(スライダー連動など)。全項目に既定値あり。位置・回転はブロックローカルで、回転はオイラー角 \[度]。中の定義(Projectile・Explosion・Booster・Guidance)に null を代入しない(使わない機能は既定値のまま = 無効)。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `MuzzlePosition` | `Vector3` | `(0, 0, 0)` | 発射位置。 |
| `MuzzleRotation` | `Vector3` | `(0, 0, 0)` | 発射向き(オイラー角 \[度])。弾はこの回転の +Z へ飛ぶ。 |
| `MuzzleVelocity` | `float` | `100` | 初速 \[m/s]。ブロックの速度に加算される。 |
| `EjectVelocity` | `Vector3` | `(0, 0, 0)` | 発射時に弾のローカル軸で足す速度 \[m/s](ロケットを発射筒から押し出す等)。反動には含めない。 |
| `FireInterval` | `float` | `0.5` | 最短発射間隔 \[s]。 |
| `FireDelay` | `float` | `0` | 発射要求から弾が出るまでの固定遅延 \[s]。 |
| `FireDelayRandomExtra` | `float` | `0` | 遅延に加える乱数幅 \[s](0〜この値)。 |
| `Spread` | `float` | `0` | 拡散 \[度]。前方に tan(Spread) 半径の球内の乱数ベクトルを足して方向を決める。弾の向きもこの方向に合わせる。 |
| `RecoilMultiplier` | `float` | `1` | 反動の倍率。弾の運動量 × この値の撃力をブロックへ逆向きに与える(1 で物理的に正しい反動)。 |
| `BurstCount` | `int` | `1` | 1回の発射要求で撃つ弾数。1発ごとに弾数を消費し、弾切れで止まる。 |
| `BurstInterval` | `float` | `0.1` | バースト内の弾の間隔 \[s]。次の発射要求は最後の弾から FireInterval 後に受け付ける。 |
| `TotalAmmo` | `int` | `-1` | 総弾数(弾倉内を含む)。負値で無制限。マシンの無限弾薬設定時は消費しない(弾倉を使う武装は弾倉の弾だけ減り、予備弾は減らない)。武装の生成時だけ読む。 |
| `MagazineCapacity` | `int` | `0` | 弾倉の容量。0 以下なら弾倉なし。武装の生成時に TotalAmmo から弾倉を満たし、残りを予備弾にする。 |
| `ReloadTime` | `float` | `1` | リロード時間 \[s]。リロード中は撃てない。 |
| `AutoReload` | `bool` | `true` | 弾倉が空になったら自動でリロードする。 |
| `ShowLoadedProjectile` | `bool` | `false` | 撃てる状態の間、次の弾の見た目を発射位置に表示する(迫撃砲・爆弾など)。発射の瞬間に消え、発射間隔の後に戻る。弾切れ・リロード中は表示しない。見た目だけでコライダーは持たない。 |
| `MuzzleFlash` | `GameObject` | なし | 発射エフェクトのプレハブ(AssetBundle 等から取得)。null なら無し。 |
| `MuzzleFlashPosition` | `Vector3` | `(0, 0, 0)` | 発射エフェクトの位置(ブロックローカル)。発射音もここで鳴る。 |
| `MuzzleFlashRotation` | `Vector3` | `(0, 0, 0)` | 発射エフェクトの向き(オイラー角 \[度]、ブロックローカル)。 |
| `MuzzleFlashScale` | `Vector3` | `(1, 1, 1)` | 発射エフェクトの拡大率。 |
| `FireSounds` | `List<AudioClip>` | 空 | 発射音(ランダムに1つ。MuzzleFlashPosition で鳴る)。 |
| `ReloadSounds` | `List<AudioClip>` | 空 | リロード開始時の音(ランダムに1つ)。 |
| `Projectile` | `ProjectileSpec` | — | 撃つ弾。飛行中の弾は発射時の ProjectileSpec を使い続けるので、差し替えは次の弾から効く(同じインスタンスの値を書き換えた場合は飛行中の弾にも効く)。弾の形は `ProjectileSpec.PoolKey` ごとに固定され、差し替えた先の弾は事前生成されず最初の発射時に作られる。 |

## ProjectileSpec

`public sealed class EWL.Api.ProjectileSpec`

弾1種類の定義(純データ)。弾体・命中・信管・見た目と、爆発・推進・誘導を持つ。複数の武装で同じインスタンスを共有してよい(書き換えると共有している全武装に効く)。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Mass` | `float` | `1` | 弾の質量 \[kg]。反動・推進の加速度と、物理的な衝突に効く(命中時に与える力は ImpactForce で決まる)。 |
| `Drag` | `float` | `0` | 空気抵抗(Rigidbody.drag)。 |
| `AngularDrag` | `float` | `0` | 回転の抵抗(Rigidbody.angularDrag)。 |
| `IgnoreGravity` | `bool` | `false` | true なら重力を受けない。 |
| `CollisionMode` | `CollisionDetectionMode` | `ContinuousDynamic` | 衝突検出の方式。速い弾がすり抜けないよう、既定は ContinuousDynamic。 |
| `Lifetime` | `float` | `20` | 寿命 \[s]。着弾しなくてもこの時間で消える。 |
| `ColliderEnableDelay` | `float` | `0.02` | 発射後、弾のコライダーを有効にするまでの時間 \[s]。自機との干渉を防ぐ。初速の遅い弾(ロケット・ミサイル・爆弾等)は長めにする。0 なら発射と同時に有効(負値は 0 扱い)。撃った武装ブロックとはこの値に関係なく衝突しない。 |
| `Colliders` | `List<ColliderShape>` | 空 | コライダー(弾ローカル)。空なら半径 0.1 の球。 |
| `Friction` | `float` | `0.6` | 弾の摩擦係数(静止・動摩擦とも)。 |
| `Bounciness` | `float` | `0` | 弾の反発係数(0〜1)。 |
| `FrictionCombine` | `PhysicMaterialCombine` | `Average` | 相手の摩擦との合成方法。 |
| `BounceCombine` | `PhysicMaterialCombine` | `Average` | 相手の反発との合成方法。 |
| `PoolSize` | `int` | `50` | 同時に飛べる弾の上限。武装の生成時にこの数まで弾を事前生成し、上限に達したら最古の弾を回収して撃つ。エフェクトもプレハブごとにこの数まで(超えた分は表示しない)。1〜1000 に丸める。 |
| `PoolKey` | `string` | なし | 弾のプールを共有するキー。同じ種類の弾で同じ値にすると、弾のオブジェクトを使い回す。弾の形(Colliders)はキーごとに最初に作ったもので固定される。null か空文字なら武装ごとの専用プール(実際のキーは `Weapon.PoolKey`)。 |
| `Mesh` | `Mesh` | なし | 見た目。null なら球を表示。 |
| `Texture` | `Texture` | なし | 見た目のテクスチャ。null ならテクスチャなし(白)。 |
| `MeshPosition` | `Vector3` | `(0, 0, 0)` | 見た目の位置(弾ローカル)。Mesh が null のときは使わない。 |
| `MeshRotation` | `Vector3` | `(0, 0, 0)` | 見た目の向き(オイラー角 \[度]、弾ローカル)。Mesh が null のときは使わない。 |
| `MeshScale` | `Vector3` | `(1, 1, 1)` | 見た目の拡大率。Mesh が null のときは使わない。 |
| `Trail` | `GameObject` | なし | 弾に追従し、命中・消滅で切り離されてフェードするエフェクトのプレハブ。 |
| `AttachedEffect` | `GameObject` | なし | 弾に追従し、弾と一緒に消えるエフェクトのプレハブ(弾の光・炎など)。 |
| `ImpactForce` | `float` | `10` | 命中した剛体に「弾の速度 × この値」の力 \[N/(m/s)] を与える。ダメージ・力は最初の命中で1回だけ。 |
| `BlockDamage` | `float` | `0` | 直撃したとき、HP を持つブロックに与えるダメージ(爆風では使わない)。 |
| `EntityDamage` | `float` | `0` | 直撃したとき、レベルのエンティティ(人・動物など)に与えるダメージ(爆風では使わない)。 |
| `StickOnHit` | `bool` | `false` | 命中物に刺さって止まる。 |
| `DespawnOnHit` | `bool` | `false` | 爆発しない弾を命中時に即消す。false なら命中の1秒後に消える。 |
| `TimeFuse` | `float` | `0` | 時限信管 \[s]。0 なら無効。経過で起爆(爆発しない弾は消える)。 |
| `TimeFuseRandomExtra` | `float` | `0.05` | 時限信管に加える乱数 \[s](0〜この値)。発射時に決める。TimeFuse が 0 のときは使わない。 |
| `DetonationDelay` | `float` | `0` | 命中から起爆までの遅延 \[s]。 |
| `DetonationDelayRandomExtra` | `float` | `0` | 起爆遅延に加える乱数幅 \[s](0〜この値)。 |
| `Explosion` | `ExplosionSpec` | — | 起爆したときの爆発。Radius が 0 なら爆発しない。 |
| `Booster` | `BoosterSpec` | — | 推進(ロケット)。Thrust が 0 なら推進しない。 |
| `Guidance` | `GuidanceSpec` | — | 誘導(仮実装)。Gain が 0 なら誘導しない。 |

## ExplosionSpec

`public sealed class EWL.Api.ExplosionSpec`

爆発1回の定義(純データ)。範囲内の剛体に AddExplosionForce と本体の爆発処理を与える。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Radius` | `float` | `0` | 半径 \[m]。0 なら爆発しない。 |
| `Force` | `float` | `0` | AddExplosionForce の explosionForce。 |
| `UpwardsModifier` | `float` | `0` | AddExplosionForce の upwardsModifier。 |
| `OnlyWithExplodingCannonballs` | `bool` | `false` | 通常は爆発しない弾として扱い、ゴッドツール「爆発する砲弾」が有効なときだけ爆発する。 |
| `Effect` | `GameObject` | なし | 爆発エフェクトのプレハブ。null なら無し。 |
| `FacesHitNormal` | `bool` | `false` | 爆発エフェクトを命中面の法線へ向ける。 |
| `Sounds` | `List<AudioClip>` | 空 | 爆発音(ランダムに1つ)。 |

## BoosterSpec

`public sealed class EWL.Api.BoosterSpec`

弾の推進(ロケット)。Thrust が 0 なら推進しない。命中したら推力は止まる。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Thrust` | `float` | `0` | 推力 \[N]。弾の前方へ毎物理フレーム加える(加速度 = Thrust / 弾の Mass)。 |
| `BurnTime` | `float` | `0` | 燃焼時間 \[s]。0 なら弾が消えるまで燃え続ける。 |
| `LateralDrag` | `float` | `0` | 燃焼中、弾の向きに対する横滑り速度 × この値の力で横滑りを抑える(安定翼)\[N/(m/s)]。 |

## GuidanceSpec

`public sealed class EWL.Api.GuidanceSpec`

誘導(仮実装)。この型・XML の Guidance 要素を含め、目標の決め方・操舵とも今後破壊的に変更する予定。発射の瞬間に、発射方向の前方一定角度内にいる敵マシンのコアブロックのうち最も近いものを目標にし、飛行中は変えない。目標が無ければ発射方向を保って飛ぶ。弾の向きをトルクで変えるだけなので、推進(`BoosterSpec`)と組み合わせて使う。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Gain` | `float` | `0` | 誘導の強さ(旋回トルクの係数)。0 なら誘導しない。 |

## ColliderShape

`public sealed class EWL.Api.ColliderShape`

弾のコライダー1個。Size は Box の大きさ、Radius は Sphere/Capsule 用、Height/Direction(0=X,1=Y,2=Z)は Capsule 用。

| フィールド | 型 | 既定値 | 説明 |
|---|---|---|---|
| `Kind` | `ColliderKind` | `Sphere` | 形。 |
| `Position` | `Vector3` | `(0, 0, 0)` | 位置(弾ローカル)。 |
| `Rotation` | `Vector3` | `(0, 0, 0)` | 向き(オイラー角 \[度]、弾ローカル)。 |
| `Size` | `Vector3` | `(1, 1, 1)` | Box の大きさ \[m]。 |
| `Radius` | `float` | `0.1` | Sphere・Capsule の半径 \[m]。 |
| `Height` | `float` | `1` | Capsule の高さ \[m]。 |
| `Direction` | `int` | `2` | Capsule の向きの軸(0 = X、1 = Y、2 = Z)。 |

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

EWL の弾の命中1回分(`Weapon.Hit` / `Weapon.AnyHit` の引数)。直撃は1回、爆風は剛体ごとに1回。撃った側の情報は発射時点のもの。

| メンバー | 説明 |
|---|---|
| `Weapon Weapon { get; }` | 撃った武装。弾が飛んでいる間に破棄されていれば `Weapon.IsAlive` が false。 |
| `BlockBehaviour ShooterBlock { get; }` | 撃ったブロック(シミュ用)。壊れた・シミュが終わった後は null。 |
| `Attacker Shooter { get; }` | 撃った人(発射時点の値。撃ったブロックが消えても残る)。 |
| `BlockBehaviour Block { get; }` | 当たったブロック(子ブロックを持つブロックは当たった子)。地形・エンティティ等は null。 |
| `Collider Collider { get; }` | 当たったコライダー(直撃のみ。爆風は null)。 |
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

攻撃者のタグ(誰の弾か・誰が起爆させたか)。発射・起爆の時点の値なので、撃った人がリスポーン・観戦・退出しても変わらない。記録は実行権威側(SP・MP ホスト・ローカルシミュ)だけで行う。キル・デスに数えるかは受け取る側が決める。

| メンバー | 説明 |
|---|---|
| `static Attacker GetDetonator(BlockBehaviour explosive)` | 爆発済みのバニラのボムを起爆させた人。分からない・まだ爆発していない・ボム以外・実行権威側でないときは null。誘爆の連鎖は根本の原因までたどる。EWL の弾の直撃・爆風(その中で起きた誘爆を含む)はその撃ち手、バニラの矢・砲弾の直撃と発射済みロケットの衝突はその持ち主、落下・体当たり等のブロック衝突はボムの持ち主(自爆)。位置では推定しないので、グレネード・ロケット・燃料樽の爆風、着火、他 Mod の弾で起爆したものは不明。 |
| `ushort PlayerId { get; }` | プレイヤーの NetworkId。SP は 0。退出した人の番号は後から参加した人に再利用されうる(同一人物かは `Attacker.Player` で比べる)。 |
| `MPTeam Team { get; }` | チーム。撃ったブロックが分かればそのブロックのチーム(シミュ開始時の値)、分からなければその時点のプレイヤーのチーム。SP は `MPTeam.None`。 |
| `Player Player { get; }` | MP のプレイヤー。接続ごとに別の参照なので、番号が再利用されても `Equals` で別人と分かる。退出後も `Name` は読める。SP は null。 |

## BundleAssets

`public static class EWL.Api.BundleAssets`

`ModAssetBundle` からアセットを取り出す。本体の `LoadAsset` は呼ぶたびにバンドルの全アセットを読む(空バンドル検出)ので、結果をバンドル・名前・型ごとに覚える。見つからなかった名前も覚え、2回目以降は読まない。

本体はバンドルを Mod 発見時に非同期で1回だけ読み、通常は解放しない。シミュ開始が読込完了より早いこともある(起動直後の MP 参加など)。未読込(`!bundle.Loaded`)なら `bundle.OnLoad` を購読して完了後に取り直し、シミュ停止で購読を解除する。読込済みのバンドルの `OnLoad` を購読すると本体が全アセットを読み直すので、購読は未読込のときだけにする。

| メンバー | 説明 |
|---|---|
| `static T Load<T>(ModAssetBundle bundle, string name)` | 未読込・読込エラー・見つからないときは null。メインスレッドから呼ぶ。シミュ開始時などに取り出して持っておき、毎フレームは呼ばない。 |
