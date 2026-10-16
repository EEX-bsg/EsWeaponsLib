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
    <Slider displayName="Fuse" key="fuse" min="0" max="10" default="3" />
</ModuleMapperTypes>
...
<FireRateSlider key="rate" />   <!-- [発/s]。逆数を FireInterval に使う。0 以下なら撃たない -->
<TimeFuseSlider key="fuse" />   <!-- [s]。Projectile/TimeFuse に使う -->
```

| 要素 | 値の使い道 | 読むとき |
|---|---|---|
| `FireRateSlider` | 1 / 値 を `FireInterval` に | 発射ごと |
| `FireDelaySlider` | 値を `FireDelay` に足す | 発射ごと |
| `TimeFuseSlider` | 値を `Projectile/TimeFuse` に | 発射ごと(飛んでいる弾は変わらない) |
| `MagazineCapacitySlider` | 四捨五入して `MagazineCapacity` に | シミュレーション開始時 |
| `PowerSlider` | 値を Power として初速に(`MuzzleVelocity` = 100 × 値 × 物理ステップ ÷ `Projectile/Mass`。通常の Besiege では 値 ÷ 質量 [m/s]) | 発射ごと |
| `BoosterPowerSlider` | 100 × 値 を `Projectile/Booster/Thrust` [N] に | 発射ごと(飛んでいる弾にも効く) |

`PowerSlider` の Power は、Besiege 公式の Shooting モジュールと同じ尺度です(100 × Power [N] の力を1物理ステップかけて撃ち出す強さ)。Power で調整してきた砲は、値を換算せずにそのまま使えます。

ブロック XML でスライダーの `min`・`max` を後から変えても、保存済みの機体には変える前の値が残り、その値で撃ちます。

## 弾

### 物理

```xml
<Projectile>
    <Mass>2</Mass>                  <!-- [kg]。反動と命中時の衝撃に効く -->
    <Drag>0.05</Drag>               <!-- 空気抵抗 -->
    <IgnoreGravity>false</IgnoreGravity>
    <Lifetime>10</Lifetime>         <!-- [s]。何にも当たらなくてもこの時間で消える -->
    <ImpactForce>10</ImpactForce>   <!-- 命中した剛体に 弾の速度 × この値 の力 -->
</Projectile>
```

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

### 信管と爆発

```xml
<Projectile>
    <TimeFuse>3</TimeFuse>                 <!-- 発射から 3 秒で起爆。0(既定)なら時限信管なし -->
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
    <Booster>
        <Thrust>400</Thrust>         <!-- [N] -->
        <BurnTime>2</BurnTime>       <!-- [s]。0 なら消えるまで燃える -->
        <LateralDrag>2</LateralDrag> <!-- 横滑りを抑える(安定翼) -->
    </Booster>
</Projectile>
```

## 誘導(仮実装)

`Projectile/Guidance/Gain` を0より大きくすると、発射の瞬間に前方の敵マシンを1つ選んで曲がっていきます。推進と組み合わせて使います。

> [!WARNING]
> **誘導は仮実装です。** 目標の選び方・操舵とも、要素ごと作り直す予定です(破壊的変更になります)。

## 弾の数が多い武装

同時に飛べる弾の数は `Projectile/PoolSize`(既定50)までです。上限に達すると最も古い弾を消して撃ちます。
連射の速い武装や寿命の長い弾では増やしてください(弾はシミュレーション開始時にこの数だけ作られます)。
