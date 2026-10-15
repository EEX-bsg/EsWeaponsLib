# 構成

EWLのコードがどう分かれ、データと制御がどう流れるかの説明です。
なぜこの形なのかは [design-principles.md](design-principles.md) を参照してください。

## プロジェクト

- csproj 1つ、出力DLL 1つ(`EsWeaponsLib.dll`)。ルートnamespaceは `EWL`。
- **フォルダとnamespaceを一致させます**(`Weapons/` → `EWL.Weapons`)。
- 対象はBesiege 1.9.1(Unity 5.4 / Mono / C# 6 / .NET 3.5)。

```
src/EsWeaponsLib/
  Mod.cs          起動点。OnLoad で自分のモジュールを登録し、ネットワークを初期化する
  ModRoot.cs      常駐するオブジェクトの置き場(シーンをまたいで残る)
  SceneRoot.cs    シーンごとのオブジェクトの置き場(弾・エフェクトのプール)
  Modules/        XML カスタムモジュール(EwlWeapon・EwlBlockPhysics)       public
  Api/            公開 C# API                                              public
  Weapons/        射撃制御・弾倉・弾体・着弾・推進・誘導・弾のプール         internal
  Effects/        エフェクトと音のプール                                    internal
  Net/            弾体の MP 同期                                            internal
  Besiege/        Besiege 本体との接点(ダメージ・爆発・音・権威判定・攻撃者のタグ)  internal
```

## 依存の向き

```
Modules/  ──(EWL.Api だけを使う)──▶  Api/  ──▶  内部(Weapons/ Effects/ Net/ Besiege/)
```

- `Modules/` はEWLのnamespaceのうち `EWL.Api` だけを使います。同じアセンブリなので内部も見えますが、使いません。
  モジュールに必要でAPIに無いものは、まずAPIに足します(そうすれば外部Modも使えます)。
- 内部は `Modules/` を参照しません。XMLスキーマの型を内部へ持ち込まず、データは `Api/` の型で渡します。
- `Api/` は薄い窓口で、ロジックを持ちません。
- `Net/` を呼ぶのは `Weapons/` の弾体管理だけです。

`Api/` のデータ型(`WeaponSpec` など)と `Modules/` のXMLスキーマ型(`EwlWeaponModule` など)は**別の型**です。
XmlSerializerの制約(publicフィールド・`Element` 基底・XML用の `Vector3`・リソースの参照名)をC#の利用者に押し付けないためです。
変換はモジュールが行います。XMLの要素名はC#のフィールド名と1対1で対応します(`EwlWeapon` 直下 = `WeaponSpec`、
`EwlWeapon/Projectile` = `ProjectileSpec`、`Projectile/Explosion` = `ExplosionSpec`)。

## 公開範囲

- `public` にするのは `Api/` と `Modules/` の、文書化する型だけです。それ以外は `internal` にします。
- `Modules/` のXML定義クラスはXmlSerializerの都合でpublicですが、これは「XMLスキーマ」で、C# APIではありません。
- 公開APIにBesiege本体の内部型を出しません。`Modding.*` とUnityEngineの型は使えます。
  `BlockBehaviour` のようにMod開発で慣例的に使う型を出すかどうかは、公開面の変更として個別に判断します。

## 武装の流れ

1. ブロックのシミュレーションが始まると、`EwlWeaponBehaviour`(または外部Modの独自モジュール)が `WeaponSpec` を作り、
   `Weapon.Create(block, spec)` で武装を付けます。中身は内部の `WeaponDriver` です。
2. 利用側が `Weapon.TryFire()` を呼ぶと、`WeaponDriver` が間隔・弾数・リロードを確かめ、遅延の後に弾を出します。
   `WeaponSpec` は発射のたびに読まれるので、利用側は発射前に値を書き換えられます(スライダー連動など)。
3. 弾(`Weapons/Projectile`)はプールから取り出され、飛行・命中・信管・爆発を処理します。ダメージと爆発は `Besiege/` を通します。
4. 命中すると `Weapon.Hit` と `Weapon.AnyHit` が発火します(実行権威側だけ)。
5. シミュレーションが止まると、利用側が `Weapon.Destroy()` を呼びます。撃った武装が消えても、飛んでいる弾は最後まで飛びます。

Besiege 標準の大砲・クロスボウ・FuelCannon の弾も `Weapon.AnyHit` に流します。本体は命中に撃ち手を渡さないので、
`Besiege/VanillaProjectiles` がブロックの初期化のときに弾の見本(と MP ホストの弾のプール)へ監視部品を付け、
複製された弾の側で、本体と同じ条件の命中を見つけて通知します。爆発する砲弾の爆風も、爆風の見本に付けた部品が担当します。

## 実行権威とMP

- 武装・弾の処理をするかどうかは、「このピアがこのシミュレーションの物理を担当しているか」で判定します(`Besiege/Simulation`)。
  ホストかどうか(`IsHost`)では代用しません。MPにはローカルシミュレーション(クライアントが自分だけでシミュレーションする)があるためです。
- SP: すべてローカルで処理します。
- MPホスト: 弾の生成・命中・ダメージの権威です。グローバルなシミュレーションの弾だけを配信します(`Net/ProjectileSync`)。
  生成・消滅はその都度、位置は一定間隔のスナップショットで送ります。武装は「ブロック」と「ブロック上で作られた順番」で対応付けます。
- MPクライアント: 同じ武装の定義から、当たり判定のない見た目だけの弾を出し、速度・抗力・重力で動かします。
  爆発の位置と向き、残弾・リロードの状態はホストから受け取ります。
- ローカルシミュレーション: 弾は配信せず、その間は受信も捨てます。
- 途中参加は復元しません(次の弾から見えます)。
- `Weapon` は**全ピアで作ります**。クライアントの見た目の弾と着弾エフェクトを同じ定義から出すためです。

## プール

- 弾はキー(`ProjectileSpec.PoolKey`。空なら武装ごと)単位でプールします。上限(`PoolSize`)まで事前に作り、上限に達したら最も古い弾を回収して撃ちます。
- エフェクトはプレハブ単位でプールし、上限を超えた分は出しません。
- 待機中の弾・エフェクトはシーンごとの置き場(`EsWeaponsLib (Scene)`)の下にあり、シーンが変わると全部捨てて作り直します。

## 入力

- キーの押下は `IsHeld` の変化で取ります。MPホストでは、クライアントの「押した瞬間」が1フレームしか立たず、取りこぼすことがあるためです。
- 実際のキーとエミュレーション(ロジックブロックなど)は別の経路なので、両方を見ます。
