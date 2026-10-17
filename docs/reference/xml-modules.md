# XMLカスタムモジュール リファレンス

> [!NOTE]
> このページは生成物です(`tools/gen-reference.ps1`)。手で編集せず、`src/` の `///` コメントを直して作り直してください。

EWLのXMLカスタムモジュールの全要素です。使い方は [XMLの使い方](../guide/xml-recipes.md) を参照してください。

- ブロックXMLの `<Modules>` に、`modid="0d219960-b202-4906-b29d-8d90a61a89b8"`(EWLのMod ID)を付けて書きます。
- **必須** と書いた要素以外は省略でき、省略した要素は既定値になります。単位はC#側と同じです(`[m/s]`・`[s]`・`[度]`)。
- 位置・回転はブロックローカル、回転はオイラー角 [度] です。
- 「書き方」の見方: ベクトル = `<要素名 x="0" y="0" z="1" />`、キー参照・トグル参照・スライダー参照 = `<要素名 key="…" />`(`key` はブロックの `ModuleMapperTypes` に定義した名前)、リソース名 = `<要素名 name="…" />`(ブロックを定義したModの `Resources` の名前)、メッシュ = リソース名に `Position` / `Rotation` / `Scale` を足したもの、`<AudioClip />` の並び = `<AudioClip name="…" />` を並べる、コライダーの並び = ブロックの `Colliders` と同じ書式。
- 再読込 ○: ブロックを定義したModの `Mod.xml` が `<Debug>true</Debug>` のとき、ゲーム中にXMLを保存すると次のシミュレーション開始から反映されます。空欄の要素はBesiegeの再起動が必要です。

## EwlWeapon

ブロックを武装にするXMLカスタムモジュール。1ブロックに複数書ける(それぞれが別の武装になる)。FireKey以外は省略でき、省略した要素は既定値になる。

C#では `WeaponSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `FireKey` **必須** | キー参照 | — |  | 発射キー(必須)。ブロックのModuleMapperTypesに定義したキーの名前をkey属性で書く。 |
| `HoldToFireToggle` | トグル参照 | なし |  | ON: 押している間連射 / OFF: 押すたびに連射の開始・停止を切り替え。省略時はHoldToFire。 |
| `ReloadKey` | キー参照 | なし |  | 押すとリロード(MagazineCapacityが1以上のとき)。 |
| `AutoReloadToggle` | トグル参照 | なし |  | ONで弾倉が空になったら自動リロード。省略時はAutoReload。 |
| `FireRateSlider` | スライダー参照 | なし |  | 値 \[発/s] の逆数をFireIntervalに使う(XMLの値より優先。0以下なら撃たない)。発射ごとに読む。 |
| `FireDelaySlider` | スライダー参照 | なし |  | 値 \[s] をFireDelayに加える。発射ごとに読む。 |
| `TimeFuseSlider` | スライダー参照 | なし |  | 値 \[s] をProjectile/TimeFuseに使う(XMLの値より優先)。Projectile/TimeFuseRandomExtraの乱数は別に加わる。発射ごとに読み、飛行中の弾は変えない。 |
| `MagazineCapacitySlider` | スライダー参照 | なし |  | 値を四捨五入してMagazineCapacityに使う(XMLの値より優先)。シミュ開始時に読む。 |
| `PowerSlider` | スライダー参照 | なし |  | 値をPower(Besiege公式のShootingモジュールと同じ尺度)として初速を決める(XMLのMuzzleVelocityより優先)。Powerは「100 × Power \[N] の力を1物理ステップかけて撃ち出す」強さで、MuzzleVelocity = 100 × Power × 物理ステップ \[s] ÷ Projectile/Mass。物理ステップが0.01秒の通常のBesiegeでは Power ÷ Projectile/Mass \[m/s]。反動もこの初速から決まる。発射ごとに読む。Projectile/Massが0以下なら使わない。 |
| `BoosterPowerSlider` | スライダー参照 | なし |  | 値の100倍をProjectile/Booster/Thrust \[N] に使う(PowerSliderと同じ尺度。XMLの値より優先)。発射ごとに読む。シミュ中に値が変わると、飛んでいる弾の推力も変わる。 |
| `ThrustTimeSlider` | スライダー参照 | なし |  | 値 \[s] をProjectile/Booster/ThrustTimeに使う(XMLの値より優先。燃料切れで推力が止まり、その後は慣性で飛ぶ)。ThrustCutoffToggleがあるときは、そのトグルがONのときだけ使う。発射ごとに読み、飛行中の弾は変えない。 |
| `ThrustCutoffToggle` | トグル参照 | なし |  | ON: ThrustTimeSliderの時間で推力を止める / OFF: XMLのProjectile/Booster/ThrustTimeを使う(既定は無限 = 燃料切れなし)。スライダーの値では「無限」を表せないので、燃料切れの有無はこのトグルで切り替える。発射ごとに読む。 |
| `HoldToFire` | `true` / `false` | `true` | ○ | HoldToFireToggleが無いときの発射方式。true: 押している間連射 / false: 押すたびに連射の開始・停止を切り替え。 |
| `AssetBundle` | リソース名 | なし | ○ | エフェクトのプレハブ(MuzzleFlash・Projectile/Trail・Projectile/AttachedEffect・Projectile/Explosion/Effect)を探すAssetBundle。Mac/Linuxでは「バンドル名+Mac」があればそちらを使う。 |
| `MuzzlePosition` | ベクトル | `(0, 0, 0)` | ○ | 発射位置。 |
| `MuzzleRotation` | ベクトル | `(0, 0, 0)` | ○ | 発射向き(オイラー角 \[度])。弾はこの回転の +Zへ飛ぶ。 |
| `MuzzleVelocity` | 数値 | `100` | ○ | 初速 \[m/s]。ブロックの速度に加算される。 |
| `EjectVelocity` | ベクトル | `(0, 0, 0)` | ○ | 発射時に弾のローカル軸で足す速度 \[m/s](ロケットを発射筒から押し出す等)。反動には含めない。 |
| `FireInterval` | 数値 | `0.5` | ○ | 最短発射間隔 \[s]。 |
| `FireDelay` | 数値 | `0` | ○ | 発射要求から弾が出るまでの固定遅延 \[s]。 |
| `FireDelayRandomExtra` | 数値 | `0` | ○ | 遅延に加える乱数幅 \[s](0〜この値)。 |
| `Spread` | 数値 | `0` | ○ | 拡散 \[度]。前方にtan(Spread) 半径の球内の乱数ベクトルを足して方向を決める。弾の向きもこの方向に合わせる。 |
| `RecoilMultiplier` | 数値 | `1` | ○ | 反動の倍率。弾の運動量 × この値の撃力をブロックへ逆向きに与える(1で物理的に正しい反動)。 |
| `BurstCount` | 整数 | `1` | ○ | 1回の発射要求で撃つ弾数。1発ごとに弾数を消費し、弾切れで止まる。 |
| `BurstInterval` | 数値 | `0.1` | ○ | バースト内の弾の間隔 \[s]。次の発射要求は最後の弾からFireInterval後に受け付ける。 |
| `TotalAmmo` | 整数 | `-1` | ○ | 総弾数(弾倉内を含む)。負値で無制限。マシンの無限弾薬設定時は消費しない(弾倉を使う武装は弾倉の弾だけ減り、予備弾は減らない)。武装の生成時だけ読む。 |
| `MagazineCapacity` | 整数 | `0` | ○ | 弾倉の容量。0以下なら弾倉なし。武装の生成時にTotalAmmoから弾倉を満たし、残りを予備弾にする。 |
| `ReloadTime` | 数値 | `1` | ○ | リロード時間 \[s]。リロード中は撃てない。 |
| `AutoReload` | `true` / `false` | `true` | ○ | 弾倉が空になったら自動でリロードする。 |
| `ShowLoadedProjectile` | `true` / `false` | `false` | ○ | 撃てる状態の間、次の弾の見た目を発射位置に表示する(迫撃砲・爆弾など)。発射の瞬間に消え、発射間隔の後に戻る。弾切れ・リロード中は表示しない。見た目だけでコライダーは持たない。EwlWeapon モジュールでは、総弾数が 0 でなければビルド中も表示する(C# から武装を作る場合は `Weapon.CreateLoadedProjectilePreview`)。 |
| `MuzzleFlash` | 文字列 | なし | ○ | AssetBundle内のプレハブ名。 |
| `MuzzleFlashPosition` | ベクトル | `(0, 0, 0)` | ○ | 発射エフェクトの位置(ブロックローカル)。発射音もここで鳴る。 |
| `MuzzleFlashRotation` | ベクトル | `(0, 0, 0)` | ○ | 発射エフェクトの向き(オイラー角 \[度]、ブロックローカル)。 |
| `MuzzleFlashScale` | ベクトル | `(1, 1, 1)` | ○ | 発射エフェクトの拡大率。 |
| `FireSounds` | `<AudioClip />` の並び | 空 | ○ | 発射音(ランダムに1つ。MuzzleFlashPositionで鳴る)。 |
| `ReloadSounds` | `<AudioClip />` の並び | 空 | ○ | リロード開始時の音(ランダムに1つ)。 |
| `Projectile` | 子要素([EwlWeapon/Projectile](#ewlweapon-projectile)) | — | ○ | 撃つ弾。飛行中の弾は発射時のProjectileSpecを使い続けるので、差し替えは次の弾から効く(同じインスタンスの値を書き換えた場合は飛行中の弾にも効く)。弾の形は `ProjectileSpec.PoolKey` ごとに固定され、差し替えた先の弾は事前生成されず最初の発射時に作られる。 |

## EwlWeapon/Projectile

弾1種類の定義(純データ)。弾体・命中・信管・見た目と、爆発・推進・誘導を持つ。複数の武装で同じインスタンスを共有してよい(書き換えると共有している全武装に効く)。

C#では `ProjectileSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Mass` | 数値 | `1` | ○ | 弾の質量 \[kg]。反動・推進の加速度と、物理的な衝突に効く(命中時に与える力はImpactForceで決まる)。 |
| `Drag` | 数値 | `0` | ○ | 空気抵抗(Rigidbody.drag)。 |
| `AngularDrag` | 数値 | `0` | ○ | 回転の抵抗(Rigidbody.angularDrag)。 |
| `IgnoreGravity` | `true` / `false` | `false` | ○ | trueなら重力を受けない。 |
| `CollisionMode` | `Discrete` / `Continuous` / `ContinuousDynamic` | `ContinuousDynamic` | ○ | 衝突検出の方式。速い弾がすり抜けないよう、既定はContinuousDynamic。 |
| `Lifetime` | 数値 | `20` | ○ | 寿命 \[s]。着弾しなくてもこの時間で消える。 |
| `ColliderEnableDelay` | 数値 | `0.02` | ○ | 発射後、弾のコライダーを有効にするまでの時間 \[s]。自機との干渉を防ぐ。初速の遅い弾(ロケット・ミサイル・爆弾等)は長めにする。0なら発射と同時に有効(負値は0扱い)。撃った武装ブロックとはこの値に関係なく衝突しない。 |
| `Colliders` | `<SphereCollider>` / `<CapsuleCollider>` / `<BoxCollider>` の並び | 空 | ○ | ブロックのCollidersと同じ書式(SphereCollider / CapsuleCollider / BoxCollider)。 |
| `Friction` | 数値 | `0.6` | ○ | 弾の摩擦係数(静止・動摩擦とも)。 |
| `Bounciness` | 数値 | `0` | ○ | 弾の反発係数(0〜1)。 |
| `FrictionCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | 相手の摩擦との合成方法。 |
| `BounceCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | 相手の反発との合成方法。 |
| `PoolSize` | 整数 | `50` | ○ | 同時に飛べる弾の上限。武装の生成時にこの数まで弾を事前生成し、上限に達したら最古の弾を回収して撃つ。エフェクトもプレハブごとにこの数まで(超えた分は表示しない)。1〜1000に丸める。 |
| `Mesh` | メッシュ | なし | ○ | ブロックを定義したModのリソース名(Position/Rotation/ScaleがProjectileSpecのMeshPosition/MeshRotation/MeshScale)。 |
| `Texture` | リソース名 | なし | ○ | 見た目のテクスチャ。nullならテクスチャなし(白)。 |
| `Trail` | 文字列 | なし | ○ | AssetBundle内のプレハブ名。 |
| `AttachedEffect` | 文字列 | なし | ○ | AssetBundle内のプレハブ名。 |
| `ImpactForce` | 数値 | `10` | ○ | 命中した剛体に「弾の速度 × この値」の力 \[N/(m/s)] を与える。ダメージ・力は最初の命中で1回だけ。 |
| `BlockDamage` | 数値 | `0` | ○ | 直撃したとき、HPを持つブロックに与えるダメージ(爆風では使わない)。 |
| `EntityDamage` | 数値 | `0` | ○ | 直撃したとき、レベルのエンティティ(人・動物など)に与えるダメージ(爆風では使わない)。 |
| `AttachOnHit` | `true` / `false` | `false` | ○ | 命中物にくっついて止まり、以後は命中物と一緒に動く(矢など)。地形では、その場で止まる。 |
| `DespawnOnHit` | `true` / `false` | `false` | ○ | 爆発しない弾を命中時に即消す。falseなら命中の1秒後に消える。 |
| `TimeFuse` | 数値 | `INF` | ○ | 時限信管 \[s]。発射からこの時間で起爆(爆発しない弾は消える)。0は発射直後に起爆。既定は無限(時限信管なし)。 |
| `TimeFuseRandomExtra` | 数値 | `0.05` | ○ | 時限信管に加える乱数 \[s](0〜この値)。発射時に決める。TimeFuseが無限のときは効かない。 |
| `DetonationDelay` | 数値 | `0` | ○ | 命中から起爆までの遅延 \[s]。 |
| `DetonationDelayRandomExtra` | 数値 | `0` | ○ | 起爆遅延に加える乱数幅 \[s](0〜この値)。 |
| `Explosion` | 子要素([EwlWeapon/Projectile/Explosion](#ewlweapon-projectile-explosion)) | — | ○ | 起爆したときの爆発。Radiusが0なら爆発しない。 |
| `Booster` | 子要素([EwlWeapon/Projectile/Booster](#ewlweapon-projectile-booster)) | — | ○ | 推進(ロケット)。Thrustが0なら推進しない。 |
| `Guidance` | 子要素([EwlWeapon/Projectile/Guidance](#ewlweapon-projectile-guidance)) | — | ○ | 誘導(仮実装。要素ごと今後破壊的に変更する予定)。 |

## EwlWeapon/Projectile/Explosion

爆発1回の定義(純データ)。範囲内の剛体にAddExplosionForceと本体の爆発処理を与える。

C#では `ExplosionSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Radius` | 数値 | `0` | ○ | 半径 \[m]。0なら爆発しない。 |
| `Force` | 数値 | `0` | ○ | AddExplosionForceのexplosionForce。 |
| `UpwardsModifier` | 数値 | `0` | ○ | AddExplosionForceのupwardsModifier。 |
| `OnlyWithExplodingCannonballs` | `true` / `false` | `false` | ○ | 通常は爆発しない弾として扱い、ゴッドツール「爆発する砲弾」が有効なときだけ爆発する。 |
| `Effect` | 文字列 | なし | ○ | AssetBundle内のプレハブ名。 |
| `FacesHitNormal` | `true` / `false` | `false` | ○ | 爆発エフェクトを命中面の法線へ向ける。 |
| `Sounds` | `<AudioClip />` の並び | 空 | ○ | 爆発音(ランダムに1つ)。 |

## EwlWeapon/Projectile/Booster

弾の推進(ロケット)と安定翼。Thrustが0なら推進しない。命中したら推力・安定翼とも止まる。

C#では `BoosterSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Thrust` | 数値 | `0` | ○ | 推力 \[N]。弾の前方へ毎物理フレーム加える(加速度 = Thrust / 弾のMass)。 |
| `ThrustTime` | 数値 | `INF` | ○ | 推力を出す時間 \[s]。発射からこの時間で推力が止まり(燃料切れ)、その後は慣性で飛ぶ(安定翼・誘導は働き続ける)。0は推力なし。既定は無限(弾が消えるまで)。発射時の値を使い、飛行中の弾は変えない。 |
| `Stability` | 数値 | `0` | ○ | 風見安定(安定翼)の強さ \[(rad/s²)/(m/s)]。機首に垂直な速度成分 × この値の角加速度で、機首を飛ぶ向きへ寄せる(弾の質量・形に依らない)。横滑りが大きいほど強く戻り、重力で曲がる弾道にも機首が沿う。推力の有無・燃焼の終了に関係なく、命中まで飛行中ずっと働く。0なら働かない。速度そのものは変えないので、横滑りは機首が向き直って推力が掛かることで消える。振動を抑えるには`ProjectileSpec.AngularDrag`を併用する。 |

## EwlWeapon/Projectile/Guidance

誘導(仮実装)。この型・XMLのGuidance要素を含め、目標の決め方・操舵とも今後破壊的に変更する予定。発射の瞬間に、発射方向の前方一定角度内にいる敵マシンのコアブロックのうち最も近いものを目標にし、飛行中は変えない。目標が無ければ発射方向を保って飛ぶ。弾の向きをトルクで変えるだけなので、推進(`BoosterSpec`)と組み合わせて使う。

C#では `GuidanceSpec` に当たります。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Gain` | 数値 | `0` | ○ | 誘導の強さ(旋回トルクの係数)。0なら誘導しない。 |

## EwlBlockPhysics

ブロック自体の物理(接合強度・物理マテリアル・接合の回転軸・追加コライダー)を設定するXMLカスタムモジュール。全要素を省略でき、省略した項目はBesiege本体の値のまま変えない。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `JointStrength` | 数値 | `-1` | ○ | 親ブロックとの接合の破断力・破断トルク。省略すると変えない。0は力が少しでもかかれば壊れる接合。壊れない接合はINF。 |
| `Material` | 子要素([EwlBlockPhysics/Material](#ewlblockphysics-material)) | — | ○ | 書けばブロックのコライダー全部に物理マテリアルを設定する。 |
| `Rotation` | 子要素([EwlBlockPhysics/Rotation](#ewlblockphysics-rotation)) | — | ○ | 書けば接合の回転軸を設定する。 |
| `AdditionalColliders` | `<SphereCollider>` / `<CapsuleCollider>` / `<BoxCollider>` の並び | 空 |  | ブロックに足すコライダー。ブロックのCollidersと同じ書式(layer・trigger属性も同じ)で、BoxColliderはCenterも書ける。layer="12" にすると、他のブロックをそこに置け、シミュ開始時に置いたブロックがこのブロックへ接続する面になる。BoxColliderは6面のどこでも置け、クリックした点を面の中心から1マス単位に丸めた位置に置かれる。Sphere/Capsuleはバニラと同じく、コライダーのPosition + 当たった面の法線 × 0.5に置かれる(当たった点ではない)。Materialを書けば、これらにも適用する。 |

## EwlBlockPhysics/Material

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `Friction` | 数値 | `0.6` | ○ | 摩擦係数(静止・動摩擦とも)。 |
| `Bounciness` | 数値 | `0` | ○ | 反発係数(0〜1)。 |
| `FrictionCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | 相手の摩擦との合成方法。 |
| `BounceCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | 相手の反発との合成方法。 |

## EwlBlockPhysics/Rotation

軸はLocked / Limited / Free。Spring・Damperは回転を元の向きへ戻す力、LimitはLimitedの可動角\[度]。

| 要素 | 書き方 | 既定値 | 再読込 | 説明 |
|---|---|---|---|---|
| `AxisX` | `Locked` / `Limited` / `Free` | `Locked` | ○ | X軸まわりの回転(Locked = 固定 / Limited = Limitまで / Free = 自由)。 |
| `AxisY` | `Locked` / `Limited` / `Free` | `Locked` | ○ | Y軸まわりの回転。 |
| `AxisZ` | `Locked` / `Limited` / `Free` | `Locked` | ○ | Z軸まわりの回転。 |
| `Spring` | 数値 | `0` | ○ | 元の向きへ戻すばねの強さ。0なら戻さない。 |
| `Damper` | 数値 | `0` | ○ | ばねの減衰。 |
| `Limit` | 数値 | `0` | ○ | Limitedの軸の可動角 \[度]。 |
