# XML カスタムモジュール リファレンス

> このページは生成物です(`tools/gen-reference.ps1`)。手で編集せず、`src/` の `///` コメントを直して作り直してください。

EWL の XML カスタムモジュールの全要素です。使い方は [XML の使い方](../guide/xml-recipes.md) を参照してください。

- ブロック XML の `<Modules>` に、`modid="0d219960-b202-4906-b29d-8d90a61a89b8"`(EWL の Mod ID)を付けて書きます。
- **必須** と書いた要素以外は省略でき、省略した要素は既定値になります。単位は C# 側と同じです(`[m/s]`・`[s]`・`[度]`)。
- 位置・回転はブロックローカル、回転はオイラー角 [度] です。
- 再読込 ○: ブロックを定義した Mod の `Mod.xml` が `<Debug>true</Debug>` のとき、ゲーム中に XML を保存すると次のシミュレーション開始から反映されます。空欄の要素は Besiege の再起動が必要です。

## EwlWeapon

ブロックを武装にする XML カスタムモジュール。1ブロックに複数書ける(それぞれが別の武装になる)。FireKey 以外は省略でき、省略した要素は既定値になる。

C# では `WeaponSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `FireKey` **必須** | キー(`key` 属性 = ModuleMapperTypes のキー名) | — |  | 発射キー(必須)。ブロックの ModuleMapperTypes に定義したキーの名前を key 属性で書く。 |
| `HoldToFireToggle` | トグル(`key` 属性 = ModuleMapperTypes のトグル名) | なし |  | ON: 押している間連射 / OFF: 押すたびに連射の開始・停止を切り替え。省略時は HoldToFire。 |
| `ReloadKey` | キー(`key` 属性 = ModuleMapperTypes のキー名) | なし |  | 押すとリロード(MagazineCapacity が 1 以上のとき)。 |
| `AutoReloadToggle` | トグル(`key` 属性 = ModuleMapperTypes のトグル名) | なし |  | ON で弾倉が空になったら自動リロード。省略時は AutoReload。 |
| `FireRateSlider` | スライダー(`key` 属性 = ModuleMapperTypes のスライダー名) | なし |  | 値 \[発/s] の逆数を FireInterval に使う(XML の値より優先。0 以下なら撃たない)。発射ごとに読む。 |
| `FireDelaySlider` | スライダー(`key` 属性 = ModuleMapperTypes のスライダー名) | なし |  | 値 \[s] を FireDelay に加える。発射ごとに読む。 |
| `TimeFuseSlider` | スライダー(`key` 属性 = ModuleMapperTypes のスライダー名) | なし |  | 値 \[s] を Projectile/TimeFuse に使う(XML の値より優先)。Projectile/TimeFuseRandomExtra の乱数は別に加わる。発射ごとに読み、飛行中の弾は変えない。 |
| `MagazineCapacitySlider` | スライダー(`key` 属性 = ModuleMapperTypes のスライダー名) | なし |  | 値を四捨五入して MagazineCapacity に使う(XML の値より優先)。シミュ開始時に読む。 |
| `HoldToFire` | `true` / `false` | `true` | ○ | HoldToFireToggle が無いときの発射方式。true: 押している間連射 / false: 押すたびに連射の開始・停止を切り替え。 |
| `AssetBundle` | リソース名(`name` 属性) | なし | ○ | エフェクトのプレハブ(MuzzleFlash・Projectile/Trail・Projectile/AttachedEffect・Projectile/Explosion/Effect)を探す AssetBundle。Mac/Linux では「バンドル名+Mac」があればそちらを使う。 |
| `MuzzlePosition` | `x` `y` `z` 属性 | `(0, 0, 0)` | ○ | 発射位置。 |
| `MuzzleRotation` | `x` `y` `z` 属性 | `(0, 0, 0)` | ○ | 発射向き(オイラー角 \[度])。弾はこの回転の +Z へ飛ぶ。 |
| `MuzzleVelocity` | 数値 | `100` | ○ | 初速 \[m/s]。ブロックの速度に加算される。 |
| `EjectVelocity` | `x` `y` `z` 属性 | `(0, 0, 0)` | ○ | 発射時に弾のローカル軸で足す速度 \[m/s](ロケットを発射筒から押し出す等)。反動には含めない。 |
| `FireInterval` | 数値 | `0.5` | ○ | 最短発射間隔 \[s]。 |
| `FireDelay` | 数値 | `0` | ○ | 発射要求から弾が出るまでの固定遅延 \[s]。 |
| `FireDelayRandomExtra` | 数値 | `0` | ○ | 遅延に加える乱数幅 \[s](0〜この値)。 |
| `Spread` | 数値 | `0` | ○ | 拡散 \[度]。前方に tan(Spread) 半径の球内の乱数ベクトルを足して方向を決める。弾の向きもこの方向に合わせる。 |
| `RecoilMultiplier` | 数値 | `1` | ○ | 反動の倍率。弾の運動量 × この値の撃力をブロックへ逆向きに与える(1 で物理的に正しい反動)。 |
| `BurstCount` | 整数 | `1` | ○ | 1回の発射要求で撃つ弾数。1発ごとに弾数を消費し、弾切れで止まる。 |
| `BurstInterval` | 数値 | `0.1` | ○ | バースト内の弾の間隔 \[s]。次の発射要求は最後の弾から FireInterval 後に受け付ける。 |
| `TotalAmmo` | 整数 | `-1` | ○ | 総弾数(弾倉内を含む)。負値で無制限。マシンの無限弾薬設定時は消費しない(弾倉を使う武装は弾倉の弾だけ減り、予備弾は減らない)。武装の生成時だけ読む。 |
| `MagazineCapacity` | 整数 | `0` | ○ | 弾倉の容量。0 以下なら弾倉なし。武装の生成時に TotalAmmo から弾倉を満たし、残りを予備弾にする。 |
| `ReloadTime` | 数値 | `1` | ○ | リロード時間 \[s]。リロード中は撃てない。 |
| `AutoReload` | `true` / `false` | `true` | ○ | 弾倉が空になったら自動でリロードする。 |
| `ShowLoadedProjectile` | `true` / `false` | `false` | ○ | 撃てる状態の間、次の弾の見た目を発射位置に表示する(迫撃砲・爆弾など)。発射の瞬間に消え、発射間隔の後に戻る。弾切れ・リロード中は表示しない。見た目だけでコライダーは持たない。 |
| `MuzzleFlash` | 文字列 | なし | ○ | AssetBundle 内のプレハブ名。 |
| `MuzzleFlashPosition` | `x` `y` `z` 属性 | `(0, 0, 0)` | ○ | 発射エフェクトの位置(ブロックローカル)。発射音もここで鳴る。 |
| `MuzzleFlashRotation` | `x` `y` `z` 属性 | `(0, 0, 0)` | ○ | 発射エフェクトの向き(オイラー角 \[度]、ブロックローカル)。 |
| `MuzzleFlashScale` | `x` `y` `z` 属性 | `(1, 1, 1)` | ○ | 発射エフェクトの拡大率。 |
| `FireSounds` | `<AudioClip name="…" />` の並び(リソース名) | 空 | ○ | 発射音(ランダムに1つ。MuzzleFlashPosition で鳴る)。 |
| `ReloadSounds` | `<AudioClip name="…" />` の並び(リソース名) | 空 | ○ | リロード開始時の音(ランダムに1つ)。 |
| `Projectile` | 子要素([EwlWeapon/Projectile](#ewlweaponprojectile)) | — | ○ | 撃つ弾。飛行中の弾は発射時の ProjectileSpec を使い続けるので、差し替えは次の弾から効く(同じインスタンスの値を書き換えた場合は飛行中の弾にも効く)。弾の形は `ProjectileSpec.PoolKey` ごとに固定され、差し替えた先の弾は事前生成されず最初の発射時に作られる。 |

## EwlWeapon/Projectile

弾1種類の定義(純データ)。弾体・命中・信管・見た目と、爆発・推進・誘導を持つ。複数の武装で同じインスタンスを共有してよい(書き換えると共有している全武装に効く)。

C# では `ProjectileSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Mass` | 数値 | `1` | ○ | 弾の質量 \[kg]。反動・推進の加速度と、物理的な衝突に効く(命中時に与える力は ImpactForce で決まる)。 |
| `Drag` | 数値 | `0` | ○ | 空気抵抗(Rigidbody.drag)。 |
| `AngularDrag` | 数値 | `0` | ○ | 回転の抵抗(Rigidbody.angularDrag)。 |
| `IgnoreGravity` | `true` / `false` | `false` | ○ | true なら重力を受けない。 |
| `CollisionMode` | `Discrete` / `Continuous` / `ContinuousDynamic` | `ContinuousDynamic` | ○ | 衝突検出の方式。速い弾がすり抜けないよう、既定は ContinuousDynamic。 |
| `Lifetime` | 数値 | `20` | ○ | 寿命 \[s]。着弾しなくてもこの時間で消える。 |
| `ColliderEnableDelay` | 数値 | `0.02` | ○ | 発射後、弾のコライダーを有効にするまでの時間 \[s]。自機との干渉を防ぐ。初速の遅い弾(ロケット・ミサイル・爆弾等)は長めにする。0 なら発射と同時に有効(負値は 0 扱い)。撃った武装ブロックとはこの値に関係なく衝突しない。 |
| `Colliders` | `<SphereCollider>` / `<CapsuleCollider>` / `<BoxCollider>` の並び(ブロックの Colliders と同じ書式) | 空 | ○ | ブロックの Colliders と同じ書式(SphereCollider / CapsuleCollider / BoxCollider)。 |
| `Friction` | 数値 | `0.6` | ○ | 弾の摩擦係数(静止・動摩擦とも)。 |
| `Bounciness` | 数値 | `0` | ○ | 弾の反発係数(0〜1)。 |
| `FrictionCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | 相手の摩擦との合成方法。 |
| `BounceCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | 相手の反発との合成方法。 |
| `PoolSize` | 整数 | `50` | ○ | 同時に飛べる弾の上限。武装の生成時にこの数まで弾を事前生成し、上限に達したら最古の弾を回収して撃つ。エフェクトもプレハブごとにこの数まで(超えた分は表示しない)。1〜1000 に丸める。 |
| `Mesh` | メッシュ(`name` 属性と `Position` / `Rotation` / `Scale`) | なし | ○ | ブロックを定義した Mod のリソース名(Position/Rotation/Scale が ProjectileSpec の MeshPosition/MeshRotation/MeshScale)。 |
| `Texture` | リソース名(`name` 属性) | なし | ○ | 見た目のテクスチャ。null ならテクスチャなし(白)。 |
| `Trail` | 文字列 | なし | ○ | AssetBundle 内のプレハブ名。 |
| `AttachedEffect` | 文字列 | なし | ○ | AssetBundle 内のプレハブ名。 |
| `ImpactForce` | 数値 | `10` | ○ | 命中した剛体に「弾の速度 × この値」の力 \[N/(m/s)] を与える。ダメージ・力は最初の命中で1回だけ。 |
| `BlockDamage` | 数値 | `0` | ○ | 直撃したとき、HP を持つブロックに与えるダメージ(爆風では使わない)。 |
| `EntityDamage` | 数値 | `0` | ○ | 直撃したとき、レベルのエンティティ(人・動物など)に与えるダメージ(爆風では使わない)。 |
| `StickOnHit` | `true` / `false` | `false` | ○ | 命中物に刺さって止まる。 |
| `DespawnOnHit` | `true` / `false` | `false` | ○ | 爆発しない弾を命中時に即消す。false なら命中の1秒後に消える。 |
| `TimeFuse` | 数値 | `0` | ○ | 時限信管 \[s]。0 なら無効。経過で起爆(爆発しない弾は消える)。 |
| `TimeFuseRandomExtra` | 数値 | `0.05` | ○ | 時限信管に加える乱数 \[s](0〜この値)。発射時に決める。TimeFuse が 0 のときは使わない。 |
| `DetonationDelay` | 数値 | `0` | ○ | 命中から起爆までの遅延 \[s]。 |
| `DetonationDelayRandomExtra` | 数値 | `0` | ○ | 起爆遅延に加える乱数幅 \[s](0〜この値)。 |
| `Explosion` | 子要素([EwlWeapon/Projectile/Explosion](#ewlweaponprojectileexplosion)) | — | ○ | 起爆したときの爆発。Radius が 0 なら爆発しない。 |
| `Booster` | 子要素([EwlWeapon/Projectile/Booster](#ewlweaponprojectilebooster)) | — | ○ | 推進(ロケット)。Thrust が 0 なら推進しない。 |
| `Guidance` | 子要素([EwlWeapon/Projectile/Guidance](#ewlweaponprojectileguidance)) | — | ○ | 誘導(仮実装。要素ごと今後破壊的に変更する予定)。 |

## EwlWeapon/Projectile/Explosion

爆発1回の定義(純データ)。範囲内の剛体に AddExplosionForce と本体の爆発処理を与える。

C# では `ExplosionSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Radius` | 数値 | `0` | ○ | 半径 \[m]。0 なら爆発しない。 |
| `Force` | 数値 | `0` | ○ | AddExplosionForce の explosionForce。 |
| `UpwardsModifier` | 数値 | `0` | ○ | AddExplosionForce の upwardsModifier。 |
| `OnlyWithExplodingCannonballs` | `true` / `false` | `false` | ○ | 通常は爆発しない弾として扱い、ゴッドツール「爆発する砲弾」が有効なときだけ爆発する。 |
| `Effect` | 文字列 | なし | ○ | AssetBundle 内のプレハブ名。 |
| `FacesHitNormal` | `true` / `false` | `false` | ○ | 爆発エフェクトを命中面の法線へ向ける。 |
| `Sounds` | `<AudioClip name="…" />` の並び(リソース名) | 空 | ○ | 爆発音(ランダムに1つ)。 |

## EwlWeapon/Projectile/Booster

弾の推進(ロケット)。Thrust が 0 なら推進しない。命中したら推力は止まる。

C# では `BoosterSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Thrust` | 数値 | `0` | ○ | 推力 \[N]。弾の前方へ毎物理フレーム加える(加速度 = Thrust / 弾の Mass)。 |
| `BurnTime` | 数値 | `0` | ○ | 燃焼時間 \[s]。0 なら弾が消えるまで燃え続ける。 |
| `LateralDrag` | 数値 | `0` | ○ | 燃焼中、弾の向きに対する横滑り速度 × この値の力で横滑りを抑える(安定翼)\[N/(m/s)]。 |

## EwlWeapon/Projectile/Guidance

誘導(仮実装)。この型・XML の Guidance 要素を含め、目標の決め方・操舵とも今後破壊的に変更する予定。発射の瞬間に、発射方向の前方一定角度内にいる敵マシンのコアブロックのうち最も近いものを目標にし、飛行中は変えない。目標が無ければ発射方向を保って飛ぶ。弾の向きをトルクで変えるだけなので、推進(`BoosterSpec`)と組み合わせて使う。

C# では `GuidanceSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Gain` | 数値 | `0` | ○ | 誘導の強さ(旋回トルクの係数)。0 なら誘導しない。 |

## EwlBlockPhysics

ブロック自体の物理(接合強度・物理マテリアル・接合の回転軸・追加コライダー)を設定する XML カスタムモジュール。全要素を省略でき、省略した項目は Besiege 本体の値のまま変えない。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `JointStrength` | 数値 | `0` | ○ | 親ブロックとの接合の破断力・破断トルク。0 = 変えない。壊れない接合は INF。 |
| `Material` | 子要素([EwlBlockPhysics/Material](#ewlblockphysicsmaterial)) | — | ○ | 書けばブロックのコライダー全部に物理マテリアルを設定する。 |
| `Rotation` | 子要素([EwlBlockPhysics/Rotation](#ewlblockphysicsrotation)) | — | ○ | 書けば接合の回転軸を設定する。 |
| `AdditionalColliders` | `<SphereCollider>` / `<CapsuleCollider>` / `<BoxCollider>` の並び(ブロックの Colliders と同じ書式) | 空 |  | ブロックに足すコライダー。ブロックの Colliders と同じ書式(layer・trigger 属性も同じ)で、BoxCollider は Center も書ける。layer="12" にすると、他のブロックをそこに置け、シミュ開始時に置いたブロックがこのブロックへ接続する面になる。BoxCollider は6面のどこでも置け、クリックした点を面の中心から1マス単位に丸めた位置に置かれる。Sphere/Capsule はバニラと同じく、コライダーの Position + 当たった面の法線 × 0.5 に置かれる(当たった点ではない)。Material を書けば、これらにも適用する。 |

## EwlBlockPhysics/Material

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Friction` | 数値 | `0.6` | ○ | 摩擦係数(静止・動摩擦とも)。 |
| `Bounciness` | 数値 | `0` | ○ | 反発係数(0〜1)。 |
| `FrictionCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | 相手の摩擦との合成方法。 |
| `BounceCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | 相手の反発との合成方法。 |

## EwlBlockPhysics/Rotation

軸は Locked / Limited / Free。Spring・Damper は回転を元の向きへ戻す力、Limit は Limited の可動角\[度]。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `AxisX` | `Locked` / `Limited` / `Free` | `Locked` | ○ | X 軸まわりの回転(Locked = 固定 / Limited = Limit まで / Free = 自由)。 |
| `AxisY` | `Locked` / `Limited` / `Free` | `Locked` | ○ | Y 軸まわりの回転。 |
| `AxisZ` | `Locked` / `Limited` / `Free` | `Locked` | ○ | Z 軸まわりの回転。 |
| `Spring` | 数値 | `0` | ○ | 元の向きへ戻すばねの強さ。0 なら戻さない。 |
| `Damper` | 数値 | `0` | ○ | ばねの減衰。 |
| `Limit` | 数値 | `0` | ○ | Limited の軸の可動角 \[度]。 |
