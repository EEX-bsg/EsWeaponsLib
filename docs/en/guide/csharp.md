# Using C#

How to use EWL from C#. All API members are listed in the [C# API Reference](../reference/csharp-api.md).
Read [Core Concepts](concepts.md) first.

## Setup

1. Reference `EsWeaponsLib.dll` from your mod's C# project (it is in EWL's mod folder).
   **Do not bundle the DLL with your mod** (turn off "Copy Local" on the reference). Ask users to install EWL separately.
2. Write `using EWL.Api;`. Use only the `EWL.Api` namespace (other namespaces are internal and change without notice).

Write `<LoadOrder>1</LoadOrder>` (1 or higher) in `Mod.xml` so that your mod loads after EWL ([Depending on EWL](concepts.md#depending-on-ewl)).
So that nothing crashes in an environment without EWL, check in `OnLoad` before proceeding to code that uses EWL types.

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
        // The version is the Version in Mod.xml. It can be checked without touching EWL's types
        var ewlVersion = Mods.GetVersion(EwlModId);
        Debug.Log("[MyMod] E'sWeaponsLib " + ewlVersion);
        RegisterModules(); // Put the code that uses EWL's types in a separate method
    }

    // If inlined, OnLoad would look for EWL's types at that point, so inlining is forbidden (System.Runtime.CompilerServices)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RegisterModules()
    {
        CustomModules.AddBlockModule<MyCannonModule, MyCannonBehaviour>("MyCannon", true);
    }
}
```

## Observing weapons

EWL weapons (including those created by `EwlWeapon`) can be retrieved from the block's GameObject.

```csharp
foreach (var weapon in Weapon.GetAll(block.gameObject))
{
    int ammo = weapon.Ammo;            // Ammo ready to fire (negative means unlimited)
    int reserve = weapon.ReserveAmmo;  // Reserve ammo outside the magazine
    bool reloading = weapon.IsReloading;
}
```

- Weapons are created when the simulation starts. Retrieve them from the simulation block (the in-simulation instance of a `BlockBehaviour`).
- You can subscribe to the events `Fired` (each shot), `ReloadStarted`, and `Hit`. **If you subscribe, always unsubscribe when the simulation stops.**

### Whose projectile hit

`Weapon.AnyHit` is a static event that fires every time a projectile hits, regardless of which weapon fired it. It covers the following.

- EWL projectiles. These are from blocks with EWL's XML custom modules and from weapons created with EWL's API (`Weapon`), including those of other mods that use EWL.
- Among Besiege's standard weapons, projectiles of the cannon, crossbow, and FuelCannon. For the cannon, this includes the blast of the god tool "Exploding Cannonballs".
  Other standard weapons (the grapeshot cannon, flamethrower, etc.) may be supported in the future.
- Projectiles of mods that use other mechanisms are not reported.

You can tell which weapon a projectile came from with `hit.Source` (`HitSource`). For projectiles of standard weapons, `hit.Weapon` and `hit.ShooterBlock` are null.

```csharp
void OnEnable()  { Weapon.AnyHit += OnAnyHit; }
void OnDisable() { Weapon.AnyHit -= OnAnyHit; }

void OnAnyHit(ProjectileHit hit)
{
    if (hit.Block == null) return;          // Terrain or an entity
    Attacker shooter = hit.Shooter;         // The shooter (value at the time of firing)
    if (hit.Kind == HitKind.Explosion) { /* Blast */ }
}
```

- Hit events fire **only on the side that has execution authority** (SP, the MP host, and a client during local simulation).
- A direct hit fires once, and a blast fires once per rigid body. `HitKind` and `HitSource` may gain new values in the future, so ignore values you do not know.
- Standard cannon shells bounce, so one shell may fire the event several times. For targets hit by a FuelCannon fireball, EWL redoes the same determination as the base game to find them, so rarely it may be off.
- When the shooter of a standard weapon's projectile cannot be determined, `hit.Shooter` is null.
- `Attacker` holds the values at the time of firing or detonation. It can be read even if the shooter has left. To compare whether two are the same person, use `Attacker.Player`
  (`PlayerId` of a player who left may be reused for a player who joins later).
- Who detonated a vanilla bomb can be found with `Attacker.GetDetonator(bomb)` (null if unknown).
  A bomb detonated by a direct hit from a standard cannon or crossbow, or by the blast of "Exploding Cannonballs", is attributed to that shooter. Bombs detonated by the blast or ignition of grenades, rockets, and fuel barrels cannot be traced at present.
- This feature can be used when your own mod determines things such as kills and deaths.

## Building a weapon in your own module

Create a `WeaponSpec` (the definition) and attach it to a block with `Weapon.Create`. EWL handles projectiles, hits, explosions, effects, magazines, and MP synchronization.
What you decide is "what kind of weapon it is" and "when it fires".

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

### Rules to follow

| Rule | Reason |
|---|---|
| Call `Weapon.Create` at simulation start, **on all peers** | So that clients spawn the visual projectiles and impact effects from the same definition. MP correspondence is by "block" and "order of creation on the block", so the order of creation must also be the same on all peers |
| Call `Destroy` when the simulation stops | Pair creation with destruction |
| Call `TryFire` / `TryReload` on the side that has execution authority | Elsewhere they do nothing and return `false`. Calling them from `SimulateUpdateHost` is enough |
| For key presses, check both `IsHeld` and emulation | On the MP host, the client's "moment of pressing" can be missed. Input from logic blocks arrives on the emulation side |
| Do not put null in the definitions inside `WeaponSpec` (`Projectile`, `Explosion`, etc.) | Leave unused features at their defaults (disabled when 0) |

### Changing values on every shot

`Weapon.Spec` is read on every shot, so rewriting it before `TryFire` takes effect from that projectile (for linking to sliders, etc.).

```csharp
_weapon.Spec.MuzzleVelocity = _powerSlider.Value;
_weapon.TryFire();
```

- Only `TotalAmmo` and the initial magazine fill are read solely at `Weapon.Create`.
- Projectiles in flight keep using the `ProjectileSpec` from the time of firing. Replacing `Spec.Projectile` with a different instance takes effect from the next projectile
  (rewriting values of the same instance also affects projectiles in flight).

### Showing a waiting projectile while building

`ShowLoadedProjectile` works only during simulation. To show the projectile at the muzzle during building as well, call `Weapon.CreateLoadedProjectilePreview` in `OnBlockPlaced`.

```csharp
public override void OnBlockPlaced()
{
    Weapon.CreateLoadedProjectilePreview(BlockBehaviour, BuildMySpec());
}
```

- It is visual only and has no hit detection. The conditions for displaying it (for example, not showing it for a weapon that has no projectiles) are decided by your mod.
- The position and the projectile's shape are determined by the Spec at the time of the call. Rewriting the Spec afterward does not change them.
- It is not displayed during simulation. It disappears together with the block. To remove it partway, `Destroy` the returned GameObject.

### Effects and assets

For `WeaponSpec.MuzzleFlash` and the like, put a GameObject prefab directly, and for `FireSounds` and the like, an AudioClip.
When retrieving from an AssetBundle, use `BundleAssets.Load<T>(bundle, name)`.
It remembers the result so that retrieving repeatedly from the same bundle stays cheap.

- The bundle may still be loading (`!bundle.Loaded`). In that case, subscribe to `bundle.OnLoad`, retrieve after loading, and unsubscribe when the simulation stops.
- Subscribing to `OnLoad` of an already loaded bundle makes Besiege reload all assets, so subscribe only when it is not yet loaded.

## What is not possible yet

- Parts to create and manipulate projectiles directly (custom guidance, submunitions, and so on). They will be published, together with the MP synchronization method, when they are actually needed.
  If you need them, tell us your use case in an issue.
- An API to check EWL's version from C#.
