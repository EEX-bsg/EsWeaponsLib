# XMLの使い方

`EwlWeapon` でよく使う組み合わせです。ここに無い要素と既定値は [XMLリファレンス](../reference/xml-modules.md) を見てください。
例では `modid` と `FireKey` などの共通部分を省いています。

## 撃ち方

### 連射と切替

```xml
<HoldToFire>false</HoldToFire>
```

- `true`(既定): 押している間、`FireInterval` ごとに撃つ。
- `false`: 押すたびに連射の開始・停止を切り替える。

プレイヤーに選ばせるときはトグルを使います(トグルがあれば `HoldToFire` より優先)。

```xml
<ModuleMapperTypes>
    <Key displayName="Fire" key="fire" default="C" />
    <Toggle displayName="Hold to fire" key="hold" default="true" />
</ModuleMapperTypes>
...
<HoldToFireToggle key="hold" />
```

### 単発撃ち

```xml
<FireMode>SemiAuto</FireMode>
```

- キーを押すたびに1回だけ撃つ(`BurstCount` が2以上ならバースト1回)。押し続けても次は撃たない。
- `FireInterval` の間に押した分は撃たずに捨てる。
- `SemiAuto` のとき `HoldToFire` と `HoldToFireToggle` は使わない。既定は `Auto`(連射)。

### 遅延・拡散・反動

```xml
<FireDelay>0.2</FireDelay>               <!-- 押してから弾が出るまで [s] -->
<FireDelayRandomExtra>0.05</FireDelayRandomExtra>  <!-- 遅延に 0〜この値 [s] を足す -->
<Spread>0.5</Spread>                     <!-- 拡散 [度] -->
<RecoilMultiplier>0.5</RecoilMultiplier> <!-- 反動の倍率。1 で物理的に正しい反動 -->
```

### バースト

1回押すと3発撃ち、最後の弾から `FireInterval` 待ちます。

```xml
<BurstCount>3</BurstCount>
<BurstInterval>0.08</BurstInterval>
<FireInterval>1</FireInterval>
```

### 1ブロックに複数の武装

`EwlWeapon` を複数書くと、それぞれが別の武装になります。キーを分ければ撃ち分けられ、同じキーなら同時に撃ちます。

```xml
<Modules>
    <EwlWeapon modid="0d219960-b202-4906-b29d-8d90a61a89b8">
        <FireKey key="fire" />
        <MuzzlePosition x="-0.3" y="0" z="1.3" />
    </EwlWeapon>
    <EwlWeapon modid="0d219960-b202-4906-b29d-8d90a61a89b8">
        <FireKey key="fire" />
        <MuzzlePosition x="0.3" y="0" z="1.3" />
    </EwlWeapon>
</Modules>
```

## 弾数・弾倉・リロード

```xml
<TotalAmmo>40</TotalAmmo>             <!-- 総弾数。負値(既定 -1)で無制限 -->
<MagazineCapacity>8</MagazineCapacity> <!-- 弾倉の容量。0(既定)なら弾倉なし -->
<ReloadTime>2.5</ReloadTime>          <!-- [s] -->
<AutoReload>true</AutoReload>         <!-- 空になったら自動でリロード -->
<ReloadKey key="reload" />            <!-- 手動リロードのキー(任意) -->
<ReloadSounds>
    <AudioClip name="reload" />
</ReloadSounds>
```

- 弾倉を使う武装は、シミュレーション開始時に総弾数から弾倉を満たし、残りが予備弾になります。リロード中は撃てません。
- マシンの無限弾薬が有効なときは総弾数を消費しません(弾倉の弾は減るので、リロードは要ります)。
- 自動リロードをプレイヤーに選ばせるときは `AutoReloadToggle` を使います。

## スライダーで値を変える

キーやトグルと同じく `ModuleMapperTypes` に定義し、`key` で参照します。スライダーの値はXMLの値より優先されます。

```xml
<ModuleMapperTypes>
    <Key displayName="Fire" key="fire" default="C" />
    <Slider displayName="Rate of fire" key="rate" min="0.5" max="10" default="2" />
    <Slider displayName="Fuse" key="fuse" min="0.5" max="10" default="3" />
</ModuleMapperTypes>
...
<FireRateSlider key="rate" />   <!-- [発/s]。逆数を FireInterval に使う。0 以下なら撃たない -->
<TimeFuseSlider key="fuse" />   <!-- [s]。Projectile/TimeFuse に使う(0 は発射直後に起爆。信管なしにはできない) -->
```

| 要素 | 値の使い道 |
|---|---|
| `FireRateSlider` | 1 / 値 を `FireInterval` に |
| `FireDelaySlider` | 値を `FireDelay` に足す |
| `TimeFuseSlider` | 値を `Projectile/TimeFuse` に |
| `MagazineCapacitySlider` | 四捨五入して `MagazineCapacity` に |
| `PowerSlider` | 値を Power として初速に(`MuzzleVelocity` = 100 × 値 × 物理ステップ ÷ `Projectile/Mass`。通常の Besiege では 値 ÷ 質量 [m/s]) |
| `BoosterPowerSlider` | 100 × 値 を `Projectile/Booster/Thrust` [N] に |
| `ThrustTimeSlider` | 値を `Projectile/Booster/ThrustTime` [s] に |

`PowerSlider` の Power は、Besiege 公式の Shooting モジュールと同じ尺度です(100 × Power [N] の力を1物理ステップかけて撃ち出す強さ)。Power で調整してきた砲は、値を換算せずにそのまま使えます。

ブロック XML でスライダーの `min`・`max` を後から変えても、保存済みの機体には変える前の値が残り、その値で撃ちます。

## 弾丸

### 物理

```xml
<Projectile>
    <Mass>2</Mass>                  <!-- [kg]。反動と命中時の衝撃に効く -->
    <Drag>0.05</Drag>               <!-- 空気抵抗 -->
    <IgnoreGravity>false</IgnoreGravity>
    <Lifetime>10</Lifetime>         <!-- [s]。何にも当たらなくてもこの時間で消える -->
    <ImpactForce>10</ImpactForce>   <!-- 命中した剛体に 弾の速度 × この値 の力 -->
    <AerodynamicStability>0.1</AerodynamicStability> <!-- [1/m]。飛行中に機首を飛ぶ向きへ寄せる。約 1/値 m 飛ぶ間に向き直る -->
</Projectile>
```

`AerodynamicStability` を入れると、曲射した砲弾の機首が放物線に沿って下がります。向き直りは速いほど強く、転がるほど遅い弾にはほとんど効きません。

### 見た目とコライダー

`Mesh` と `Texture` には、ブロックを定義したModの `Mod.xml` の `Resources` に書いた名前を使います。コライダーはブロックの `Colliders` と同じ書式です。
コライダーを書かなければ半径0.1の球になります。

```xml
<Projectile>
    <Mesh name="shell">
        <Position x="0" y="0" z="0" />
        <Rotation x="0" y="0" z="0" />
        <Scale x="0.2" y="0.2" z="0.5" />
    </Mesh>
    <Texture name="shell" />
    <Colliders>
        <SphereCollider>
            <Position x="0" y="0" z="0" />
            <Radius>0.1</Radius>
        </SphereCollider>
    </Colliders>
</Projectile>
```

### ダメージ・くっつく弾・消える弾

```xml
<BlockDamage>1</BlockDamage>     <!-- HP を持つブロックへのダメージ -->
<EntityDamage>50</EntityDamage>  <!-- 人・動物などへのダメージ -->
<AttachOnHit>true</AttachOnHit>  <!-- 命中物にくっついて一緒に動く(矢など) -->
<DespawnOnHit>true</DespawnOnHit> <!-- 爆発しない弾を命中で即消す。既定は命中の1秒後に消える -->
```

ダメージと命中の力は、最初の命中で1回だけ与えます。

### 貫通

命中すると弾の進む向きに装甲を通り抜け、通った厚みの合計が `Penetration` [m] に達した所で止まって、そこで命中・起爆します。
その手前で装甲を抜けたら、弾は出口の外へ出て命中直前の速度・向きのまま飛び続け、残りの貫通量で次の装甲も貫通します。
爆発する弾は、抜けた所で起爆を始めます(起爆遅延の間は飛び続けます)。

```xml
<Penetration>0.3</Penetration> <!-- 0なら貫通しない(既定) -->
```

- 厚みは進む向きに沿って測ります。斜めの装甲ほど厚くなり、止まりやすくなります。
- 接した装甲は合わせた厚みで数えます(例: 上限0.3mの弾が厚さ0.15mの板を抜けると、裏に接した丸太に0.15m入った所で止まる)。弾が入れない狭い隙間(0.05mか弾の長さの広い方まで)も、接しているものとして扱います。
- `PenetrationMode` で厚みの数え方を選べます。
  - `Total`(既定): 接した装甲の合計で数える(上の例)。
  - `FirstBlock`: 当たったブロックだけを数える。上限より厚ければその中で止まり、抜けたら飛び続ける。裏に別のブロックが接していれば、抜けずにその表面で止まる。例: 上限0.3mの弾が厚さ0.15mの板に当たると、板の裏(=裏に接する丸太の表面)で止まる。1ブロックに複数のコライダーがあっても、ブロック単位で数えます。

```xml
<Penetration>0.3</Penetration>
<PenetrationMode>FirstBlock</PenetrationMode>
```
- ダメージ・衝撃力・命中イベントは、通ったブロックそれぞれに1回ずつ与えます(命中イベントの位置はそのブロックへの入口)。止まった弾はそのブロックにくっつきます(`AttachOnHit` の有無に依らない)。
- 抜けたときは速度を落としません。ただし命中した装甲は、物理の衝突で押されます。
- 凹んだ形のメッシュのコライダーでは、厚みが正確に測れないことがあります。

### 信管と爆発

```xml
<Projectile>
    <TimeFuse>3</TimeFuse>                 <!-- 発射から 3 秒で起爆。書かなければ時限信管なし(0 は発射直後に起爆) -->
    <DetonationDelay>0.05</DetonationDelay> <!-- 命中から起爆までの遅延 [s] -->
    <Explosion>
        <Radius>4</Radius>                 <!-- 0(既定)なら爆発しない -->
        <Force>8000</Force>
        <UpwardsModifier>0.5</UpwardsModifier>
    </Explosion>
</Projectile>
```

`Explosion/OnlyWithExplodingCannonballs` を `true` にすると、普段は爆発せず、ゴッドツール「爆発する砲弾」が有効なときだけ爆発します。

## エフェクトと音

エフェクトはAssetBundle内のプレハブ名で指定します。`AssetBundle` は武装ごとに1つで、すべてのエフェクトをそこから探します。
音は `Resources` の `AudioClip` の名前で指定し、複数書くと毎回ランダムに1つ鳴ります。

```xml
<!-- Mod.xml の Resources -->
<AssetBundle name="fx" path="fx" />
<AudioClip name="fire1" path="fire1.ogg" />
<AudioClip name="boom" path="boom.ogg" />
```

```xml
<!-- ブロックの EwlWeapon -->
<AssetBundle name="fx" />
<MuzzleFlash>MuzzleFlash</MuzzleFlash>
<MuzzleFlashPosition x="0" y="0" z="1.3" />
<FireSounds>
    <AudioClip name="fire1" />
</FireSounds>
<Projectile>
    <Trail>SmokeTrail</Trail>            <!-- 弾に付き、命中・消滅で切り離されてフェードする -->
    <AttachedEffect>Glow</AttachedEffect> <!-- 弾に付き、弾と一緒に消える -->
    <Explosion>
        <Radius>3</Radius>
        <Effect>Explosion</Effect>
        <FacesHitNormal>true</FacesHitNormal>
        <Sounds>
            <AudioClip name="boom" />
        </Sounds>
    </Explosion>
</Projectile>
```

Mac・Linuxでは、「バンドル名 + `Mac`」のAssetBundleがあればそちらを使います。

## 待機弾の表示

迫撃砲や爆弾のように、撃つ前の弾を発射位置に見せます。発射の瞬間に消え、次に撃てるようになると戻ります。ビルド中も、発射位置に弾を表示します(`TotalAmmo` が 0 の武装を除く)。

```xml
<ShowLoadedProjectile>true</ShowLoadedProjectile>
```

## ロケット

推進(`Booster`)を付けると、弾が自分で加速します。発射筒から押し出すだけなら初速を0にして `EjectVelocity` を使います。

```xml
<MuzzleVelocity>0</MuzzleVelocity>
<EjectVelocity x="0" y="0" z="5" />     <!-- 弾のローカル軸で足す速度 [m/s]。反動に含めない -->
<Projectile>
    <ColliderEnableDelay>0.3</ColliderEnableDelay> <!-- 遅い弾が自機に当たらないよう延ばす -->
    <AngularDrag>4</AngularDrag>  <!-- 安定翼で向き直るときの振動を抑える -->
    <Booster>
        <Thrust>400</Thrust>         <!-- [N] -->
        <ThrustTime>2</ThrustTime>   <!-- 推力を出す時間 [s]。書かなければ弾が消えるまで -->
        <Stability>0.5</Stability>   <!-- 安定翼: 機首を飛ぶ向きへ寄せる。重力で落ちる弾道にも機首が沿う -->
        <MaxSpeed>150</MaxSpeed>     <!-- 最高速度 [m/s]。これ以上は推力で加速しない。書かなければ上限なし -->
    </Booster>
</Projectile>
```

推力だけでは弾が加速し続けます。速さを抑えるには `MaxSpeed`(推力で加速する上限)か、`Drag`(抗力。推力と釣り合う速さで頭打ちになり、加速も緩やかになる)を使います。

推力の燃焼時間をプレイヤーに決めさせるときは `ThrustTimeSlider` を使います。燃料が切れると推力が止まり、その後は慣性で飛びます(安定翼・誘導は働き続けます)。燃料切れなし(無限)にする武装は、スライダーを置かず `ThrustTime` も書きません。

```xml
<ModuleMapperTypes>
    <Slider displayName="Burn time" key="burn" min="0.1" max="10" default="2" />
</ModuleMapperTypes>
...
<ThrustTimeSlider key="burn" />
```

## 誘導

`Projectile/Guidance/Gain` を0より大きくすると、弾が目標の敵マシンへ曲がっていきます。向きを変えるだけなので、推進と組み合わせて使います。
目標の決まり方・飛び方・解除の詳しい説明は [誘導ミサイル](guidance.md) にあります。

```xml
<Projectile>
    <Booster>
        <Thrust>1000</Thrust>
        <Stability>0.5</Stability>
    </Booster>
    <Guidance>
        <Gain>0.25</Gain>             <!-- 誘導の強さ -->
        <ReleaseAngle>90</ReleaseAngle> <!-- 機首と目標方向の角がこれを超えたら誘導をやめる [度]。書かなければやめない -->
    </Guidance>
</Projectile>
```

- **目標はXMLだけでは決まりません。** C#で `Weapon.Target` を指定します(照準の作り方は利用側が決めます。[C#の使い方](csharp.md#誘導の目標を指定する))。指定が無い弾は無誘導で飛びます。
- 目標は発射を受け付けた時点で確定し、飛んでいる弾の目標は変わりません。目標が壊れる・リスポーンする・解除されると、その弾は無誘導になり、別の敵を追い直しません。
- `ReleaseAngle` は、外れて目標を通り過ぎた弾が引き返さないための設定です。90なら、目標が機首の前方の半球にいる限り追い続けます。発射直後から判定するので、目標から大きく外れた向きに撃つと、すぐ無誘導になります。
- 無誘導の弾は向きを保って直進します(目標なしで撃った弾は発射方向、目標を失った弾はその時点の向き)。推進・安定翼・抗力・重力・寿命はそのまま働きます。

> [!NOTE]
> 1.0までは、誘導の挙動・係数を変えることがあります(変えたらCHANGELOGに書きます)。

## 弾の数が多い武装

同時に飛べる弾の数は `Projectile/PoolSize`(既定50)までです。上限に達すると最も古い弾を消して撃ちます。
連射の速い武装や寿命の長い弾では増やしてください(弾はシミュレーション開始時にこの数だけ作られます)。
