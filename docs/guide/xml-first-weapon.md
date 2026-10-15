# 最初の武装を作る(XML)

C# を書かずに、XML だけで撃てるブロックを作ります。完成形はリポジトリの `EwlSample/` にあります。

前提: Besiege の Mod の作り方(`Mod.xml`・ブロック XML・リソース)を知っていること。EWL が導入済みであること。

## 1. Mod を用意する

普通の Mod と同じです。EWL の C# を使わないので `Assemblies` は空のままで構いません。

```xml
<Mod>
    <Name>My Weapons</Name>
    <Author>you</Author>
    <Version>0.1.0</Version>
    <Description>Weapons built on E'sWeaponsLib.</Description>
    <Debug>true</Debug>
    <MultiplayerCompatible>true</MultiplayerCompatible>

    <Assemblies />
    <Blocks>
        <Block path="Blocks/MyGun.xml" />
    </Blocks>
    <Resources>
        <Mesh name="box" path="box.obj" />
        <Texture name="olive" path="olive.png" />
    </Resources>
    <!-- ID は Besiege の Mod ツールが生成した値を使う -->
</Mod>
```

## 2. ブロックに EwlWeapon を書く

ブロック XML の `Modules` に `EwlWeapon` を書きます。`modid` は EWL の Mod ID です。

```xml
<Block>
    <!-- Name・ID・Mesh・Colliders などは普通のブロックと同じ -->

    <ModuleMapperTypes>
        <Key displayName="Fire" key="fire" default="C" />
    </ModuleMapperTypes>
    <Modules>
        <EwlWeapon modid="0d219960-b202-4906-b29d-8d90a61a89b8">
            <FireKey key="fire" />
            <MuzzlePosition x="0" y="0" z="1.3" />
            <MuzzleVelocity>150</MuzzleVelocity>
            <FireInterval>0.3</FireInterval>
            <Projectile>
                <Mass>0.5</Mass>
                <BlockDamage>1</BlockDamage>
            </Projectile>
        </EwlWeapon>
    </Modules>
</Block>
```

- `FireKey` だけが必須です。`key` には `ModuleMapperTypes` で定義したキーの `key` を書きます。
- 書かなかった要素は既定値になります(初速 100 m/s、間隔 0.5 秒、弾は質量 1 の小さな球、ダメージ 0、無制限の弾数など)。
- `MuzzlePosition` はブロックローカルの発射位置です。弾は `MuzzleRotation` の +Z 方向(既定ではブロックの前方)へ飛びます。
  **発射位置はブロックのコライダーの外**に置いてください。

## 3. 試す

1. Besiege を起動し、ブロックを置いてシミュレーションを始める。
2. `C` を押すと撃つ。押している間は連射されます(`HoldToFire` の既定が `true`)。
3. ブロックが反動で後ろへ押されれば発射できています。

`Debug` を `true` にしておくと、ゲーム中に XML を保存するだけで多くの値が次のシミュレーションから反映されます。

## 4. 爆発させる

弾に `Explosion` を足すと、命中で爆発します。`Radius` が 0 でなければ爆発する弾になります。

```xml
<Projectile>
    <Mass>0.5</Mass>
    <Lifetime>8</Lifetime>
    <BlockDamage>1</BlockDamage>
    <EntityDamage>50</EntityDamage>
    <Explosion>
        <Radius>2</Radius>
        <Force>1500</Force>
        <UpwardsModifier>0.5</UpwardsModifier>
    </Explosion>
</Projectile>
```

## 次に読むもの

- [XML の使い方](xml-recipes.md): 弾倉・バースト・スライダー・見た目・エフェクトと音・ロケットなど。
- [XML リファレンス](../reference/xml-modules.md): 全要素と既定値。
- [基本の考え方](concepts.md): MP・実行権威・よくある失敗。
