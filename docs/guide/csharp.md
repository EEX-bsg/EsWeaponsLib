# C# で使う

C# から EWL を使う方法です。API の全メンバーは [C# API リファレンス](../reference/csharp-api.md)、
動く例はリポジトリの `EwlSampleCSharp/` にあります。先に [基本の考え方](concepts.md) を読んでください。

## 準備

1. 自分の Mod の C# プロジェクトから `EsWeaponsLib.dll` を参照します(EWL の Mod フォルダにあります)。
   **DLL を自分の Mod に同梱しないでください**(参照の「ローカルにコピー」を切る)。利用者には EWL を別に導入してもらいます。
2. `using EWL.Api;` を書きます。使うのは `EWL.Api` 名前空間だけです(他の名前空間は内部用で、予告なく変わります)。

EWL はタイトル画面で読み込まれるので、あなたの Mod より必ず先に読み込まれます。
EWL が入っていない環境でも落ちないよう、`OnLoad` で確かめてから EWL の型を使うコードへ進みます。

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

EWL の武装(`EwlWeapon` で作られたものを含む)は、ブロックの GameObject から取り出せます。

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

`Weapon.AnyHit` は、どの武装の弾が当たっても発火する静的イベントです(他 Mod の武装を含む)。

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

- 命中のイベントは**実行権威のある側でだけ**発火します(SP・MP ホスト・ローカルシミュレーション中のクライアント)。
- 直撃は1回、爆風は剛体ごとに1回発火します。`HitKind` は今後増えることがあるので、知らない値は無視してください。
- `Attacker` は発射・起爆の時点の値です。撃った人が退出していても読めます。同一人物かどうかは `Attacker.Player` で比べます
  (`PlayerId` は退出した人の番号が後から参加した人に再利用されることがあります)。
- バニラのボムを誰が起爆させたかは `Attacker.GetDetonator(bomb)` で分かります(分からなければ null)。
- キル・デスに数えるかどうかは、あなたの Mod が決めます。

## 自分のモジュールで武装を作る

`WeaponSpec`(定義)を作り、`Weapon.Create` でブロックに付けます。弾・命中・爆発・エフェクト・弾倉・MP 同期は EWL が行います。
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
| `Weapon.Create` はシミュレーション開始で、**全ピアで**呼ぶ | クライアントの見た目の弾・着弾エフェクトを同じ定義から出すため。MP の対応付けは「ブロック」と「ブロック上で作った順番」なので、作る順番も全ピアで同じにする |
| `Destroy` はシミュレーション停止で呼ぶ | 生成と破棄を対にする |
| `TryFire` / `TryReload` は実行権威のある側で呼ぶ | それ以外では何もせず `false` を返す。`SimulateUpdateHost` から呼べばよい |
| キーの押下は `IsHeld` とエミュレーションの両方を見る | MP ホストでは、クライアントの「押した瞬間」を取りこぼすことがある。ロジックブロックからの入力はエミュレーション側に来る |
| `WeaponSpec` の中の定義(`Projectile`・`Explosion` など)に null を入れない | 使わない機能は既定値のまま(0 なら無効) |

### 撃つたびに値を変える

`Weapon.Spec` は発射のたびに読まれるので、`TryFire` の前に書き換えればその弾から反映されます(スライダー連動など)。

```csharp
_weapon.Spec.MuzzleVelocity = _powerSlider.Value;
_weapon.TryFire();
```

- `TotalAmmo` と弾倉の初期の詰め方だけは `Weapon.Create` の時にしか読みません。
- 飛んでいる弾は発射時の `ProjectileSpec` を使い続けます。`Spec.Projectile` を別のインスタンスに差し替えると次の弾から効きます
  (同じインスタンスの値を書き換えると、飛んでいる弾にも効きます)。

### エフェクトとアセット

`WeaponSpec.MuzzleFlash` などには GameObject のプレハブ、`FireSounds` などには AudioClip を直接入れます。
AssetBundle から取り出すときは `BundleAssets.Load<T>(bundle, name)` を使ってください。
同じバンドルから何度も取り出しても重くならないよう、結果を覚えます。

- バンドルが読込中(`!bundle.Loaded`)のこともあります。そのときは `bundle.OnLoad` を購読して読込後に取り出し、シミュレーション停止で解除します。
- 読込済みのバンドルの `OnLoad` を購読すると、Besiege が全アセットを読み直すので、購読は未読込のときだけにしてください。

## 今はまだできないこと

- 弾を直接作る・操作する部品(独自の誘導・子弾など)。実際に必要になった時点で、MP 同期の方法と一緒に公開します。
  必要なときは Issue で用途を教えてください。
- EWL の版を C# から確かめる API。
