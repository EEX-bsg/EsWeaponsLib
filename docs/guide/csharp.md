# C#で使う

C#からEWLを使う方法です。APIの全メンバーは [C# APIリファレンス](../reference/csharp-api.md) にあります。
先に [基本の考え方](concepts.md) を読んでください。

## 準備

1. 自分のModのC#プロジェクトから `EsWeaponsLib.dll` を参照します(EWLのModフォルダにあります)。
   **DLLを自分のModに同梱しないでください**(参照の「ローカルにコピー」を切る)。利用者にはEWLを別に導入してもらいます。
2. `using EWL.Api;` を書きます。使うのは `EWL.Api` 名前空間だけです(他の名前空間は内部用で、予告なく変わります)。

`Mod.xml` に `<LoadOrder>1</LoadOrder>`(1以上)を書いて、EWLより後に読み込まれるようにします([依存のしかた](concepts.md#依存のしかた))。
EWLが入っていない環境でも落ちないよう、`OnLoad` で確かめてからEWLの型を使うコードへ進みます。

```csharp
public class MyMod : ModEntryPoint
{
    private static readonly Guid EwlModId = new Guid("0d219960-b202-4906-b29d-8d90a61a89b8");

    public override void OnLoad()
    {
        if (!Mods.IsModLoaded(EwlModId))
        {
            Debug.LogWarning("[MyMod] E'sWeaponsLib is not loaded.");
            return;
        }
        RegisterModules(); // EWL の型を使う処理は別メソッドに分ける
    }

    // インライン化されると OnLoad の時点で EWL の型を探しに行くので禁止する(System.Runtime.CompilerServices)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterModules()
    {
        CustomModules.AddBlockModule<MyCannonModule, MyCannonBehaviour>("MyCannon", true);
    }
}
```

## 武装を観測する

EWLの武装(`EwlWeapon` で作られたものを含む)は、ブロックのGameObjectから取り出せます。

```csharp
foreach (var weapon in Weapon.GetAll(block.gameObject))
{
    int ammo = weapon.Ammo;            // すぐ撃てる弾(負値は無制限)
    int reserve = weapon.ReserveAmmo;  // 弾倉の外の予備弾
    bool reloading = weapon.IsReloading;
}
```

- 武装はシミュレーション開始時に作られます。シミュレーション用のブロック(`BlockBehaviour` のシミュレーション中のもの)から取り出してください。
- イベント `Fired`(1発ごと)・`ReloadStarted`・`Hit` を購読できます。**購読したら、シミュレーション停止で必ず解除**してください。

### 誰の弾が当たったか

`Weapon.AnyHit` は、EWL の弾が当たるたびに、撃った武装を問わず発火する静的イベントです。
対象は EWL の弾だけです。EWL の XML カスタムモジュールのブロックと、EWL の API(`Weapon`)で作った武装は、EWL を使う他の Mod のものも含まれます。
EWL を使わない武装の弾(Besiege 標準の武器や、他の仕組みの Mod の弾)は通知されません。

```csharp
void OnEnable()  { Weapon.AnyHit += OnAnyHit; }
void OnDisable() { Weapon.AnyHit -= OnAnyHit; }

void OnAnyHit(ProjectileHit hit)
{
    if (hit.Block == null) return;          // 地形やエンティティ
    Attacker shooter = hit.Shooter;         // 撃った人(発射時点の値)
    if (hit.Kind == HitKind.Explosion) { /* 爆風 */ }
}
```

- 命中のイベントは**実行権威のある側でだけ**発火します(SP・MPホスト・ローカルシミュレーション中のクライアント)。
- 直撃は1回、爆風は剛体ごとに1回発火します。`HitKind` は今後増えることがあるので、知らない値は無視してください。
- `Attacker` は発射・起爆の時点の値です。撃った人が退出していても読めます。同一人物かどうかは `Attacker.Player` で比べます
  (`PlayerId` は退出した人の番号が後から参加した人に再利用されることがあります)。
- バニラのボムを誰が起爆させたかは `Attacker.GetDetonator(bomb)` で分かります(分からなければnull)。
- キル・デスに数えるかどうかは、あなたのModが決めます。

## 自分のモジュールで武装を作る

`WeaponSpec`(定義)を作り、`Weapon.Create` でブロックに付けます。弾・命中・爆発・エフェクト・弾倉・MP同期はEWLが行います。
あなたが決めるのは「どんな武装か」と「いつ撃つか」です。

```csharp
public class MyCannonBehaviour : BlockModuleBehaviour<MyCannonModule>
{
    private MKey _fireKey;
    private Weapon _weapon;

    public override void SafeAwake()
    {
        _fireKey = GetKey(Module.FireKey);
    }

    public override void OnSimulateStart()
    {
        var spec = new WeaponSpec
        {
            MuzzlePosition = new Vector3(0f, 0f, 1.3f),
            MuzzleVelocity = 200f,
            FireInterval = 0.5f,
        };
        spec.Projectile.Mass = 0.5f;
        spec.Projectile.Explosion.Radius = 4f;
        spec.Projectile.Explosion.Force = 2000f;
        _weapon = Weapon.Create(BlockBehaviour, spec);
    }

    public override void OnSimulateStop()
    {
        if (_weapon != null) _weapon.Destroy();
        _weapon = null;
    }

    public override void SimulateUpdateHost()
    {
        if (_weapon != null && (_fireKey.IsHeld || _fireKey.EmulationHeld(true))) _weapon.TryFire();
    }
}
```

### 守ること

| 約束 | 理由 |
|---|---|
| `Weapon.Create` はシミュレーション開始で、**全ピアで**呼ぶ | クライアントの見た目の弾・着弾エフェクトを同じ定義から出すため。MPの対応付けは「ブロック」と「ブロック上で作った順番」なので、作る順番も全ピアで同じにする |
| `Destroy` はシミュレーション停止で呼ぶ | 生成と破棄を対にする |
| `TryFire` / `TryReload` は実行権威のある側で呼ぶ | それ以外では何もせず `false` を返す。`SimulateUpdateHost` から呼べばよい |
| キーの押下は `IsHeld` とエミュレーションの両方を見る | MPホストでは、クライアントの「押した瞬間」を取りこぼすことがある。ロジックブロックからの入力はエミュレーション側に来る |
| `WeaponSpec` の中の定義(`Projectile`・`Explosion` など)にnullを入れない | 使わない機能は既定値のまま(0なら無効) |

### 撃つたびに値を変える

`Weapon.Spec` は発射のたびに読まれるので、`TryFire` の前に書き換えればその弾から反映されます(スライダー連動など)。

```csharp
_weapon.Spec.MuzzleVelocity = _powerSlider.Value;
_weapon.TryFire();
```

- `TotalAmmo` と弾倉の初期の詰め方だけは `Weapon.Create` の時にしか読みません。
- 飛んでいる弾は発射時の `ProjectileSpec` を使い続けます。`Spec.Projectile` を別のインスタンスに差し替えると次の弾から効きます
  (同じインスタンスの値を書き換えると、飛んでいる弾にも効きます)。

### ビルド中に待機弾を見せる

`ShowLoadedProjectile` はシミュレーション中だけ効きます。ビルド中も発射位置に弾を見せるには、`OnBlockPlaced` で `Weapon.CreateLoadedProjectilePreview` を呼びます。

```csharp
public override void OnBlockPlaced()
{
    Weapon.CreateLoadedProjectilePreview(BlockBehaviour, BuildMySpec());
}
```

- 見た目だけで、当たり判定はありません。表示する条件(弾が無い武装は出さない等)は、あなたのModが決めます。
- 位置と弾の形は、呼んだ時の Spec で決まります。後から Spec を書き換えても変わりません。
- シミュレーション中は表示されません。ブロックを消せば一緒に消えます。途中で消すときは、戻り値の GameObject を `Destroy` します。

### エフェクトとアセット

`WeaponSpec.MuzzleFlash` などにはGameObjectのプレハブ、`FireSounds` などにはAudioClipを直接入れます。
AssetBundleから取り出すときは `BundleAssets.Load<T>(bundle, name)` を使ってください。
同じバンドルから何度も取り出しても重くならないよう、結果を覚えます。

- バンドルが読込中(`!bundle.Loaded`)のこともあります。そのときは `bundle.OnLoad` を購読して読込後に取り出し、シミュレーション停止で解除します。
- 読込済みのバンドルの `OnLoad` を購読すると、Besiegeが全アセットを読み直すので、購読は未読込のときだけにしてください。

## 今はまだできないこと

- 弾を直接作る・操作する部品(独自の誘導・子弾など)。実際に必要になった時点で、MP同期の方法と一緒に公開します。
  必要なときはIssueで用途を教えてください。
- EWLの版をC#から確かめるAPI。
