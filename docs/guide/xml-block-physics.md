# ブロックの物理(EwlBlockPhysics)

`EwlBlockPhysics` は、武装ではなくブロック自体の物理を設定するXMLカスタムモジュールです。
全要素を省略でき、書かなかった項目はBesiegeの値のまま変わりません。全要素は [XMLリファレンス](../reference/xml-modules.md#ewlblockphysics) にあります。

```xml
<Modules>
    <EwlBlockPhysics modid="0d219960-b202-4906-b29d-8d90a61a89b8">
        <JointStrength>INF</JointStrength>
        <Material>
            <Friction>0.8</Friction>
            <Bounciness>0</Bounciness>
        </Material>
    </EwlBlockPhysics>
</Modules>
```

## 接合の強さ

`JointStrength` は親ブロックとの接合が壊れる力・トルクです。`INF` で壊れなくなります。書かなければ変えません(0 は、力が少しでもかかれば壊れる接合になります)。

## 物理マテリアル

`Material` を書くと、ブロックのすべてのコライダー(`AdditionalColliders` を含む)に摩擦・反発を設定します。

## 接合の回転軸

`Rotation` を書くと、親ブロックに対して回るブロックにできます(旗・アンテナの揺れなど)。

```xml
<Rotation>
    <AxisX>Limited</AxisX>   <!-- Locked(既定)/ Limited / Free -->
    <AxisY>Locked</AxisY>
    <AxisZ>Locked</AxisZ>
    <Limit>30</Limit>        <!-- Limited の軸の可動角 [度] -->
    <Spring>50</Spring>      <!-- 元の向きへ戻す強さ -->
    <Damper>5</Damper>
</Rotation>
```

## コライダーを足す

`AdditionalColliders` は、ブロックの `Colliders` と同じ書式で、ブロックにコライダーを足します。`BoxCollider` には `Center` も書けます。

```xml
<AdditionalColliders>
    <BoxCollider layer="12">
        <Position x="0" y="0.5" z="0" />
        <Rotation x="0" y="0" z="0" />
        <Scale x="1" y="0.1" z="1" />
    </BoxCollider>
</AdditionalColliders>
```

- `layer="12"` にすると、そこに他のブロックを置けるようになり、シミュレーション開始時にそのブロックがこのブロックへ接続します(ブロックを載せる台など)。
- `BoxCollider` は6面のどこにでも置け、クリックした点を面の中心から1マス単位に丸めた位置に置かれます。
- `SphereCollider` / `CapsuleCollider` は、Besiegeの標準と同じく「コライダーの位置 + 当たった面の向き × 0.5」に置かれます。
- `AdditionalColliders` を変えたときはBesiegeの再起動が必要です。
