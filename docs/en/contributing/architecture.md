# Architecture

An explanation of how EWL's code is divided and how data and control flow.
For why it takes this shape, see [design-principles.md](design-principles.md).

## Project

- One csproj, one output DLL (`EsWeaponsLib.dll`). The root namespace is `EWL`.
- **Make folders and namespaces match** (`Weapons/` → `EWL.Weapons`).
- The target is Besiege 1.9.1 (Unity 5.4 / Mono / C# 6 / .NET 3.5).

```
src/EsWeaponsLib/
  Mod.cs          Entry point. Registers its own modules in OnLoad and initializes the network
  ModRoot.cs      Home for persistent objects (survives across scenes)
  SceneRoot.cs    Home for per-scene objects (projectile and effect pools)
  Modules/        XML custom modules (EwlWeapon, EwlBlockPhysics)           public
  Api/            Public C# API                                             public
  Weapons/        Fire control, magazines, projectiles, impacts, propulsion, guidance, projectile pools   internal
  Effects/        Effect and sound pools                                    internal
  Net/            MP synchronization of projectiles                         internal
  Besiege/        Interface to the Besiege base game (damage, explosions, sounds, authority checks, attacker tags)  internal
```

## Dependency direction

```
Modules/  ──(uses only EWL.Api)──▶  Api/  ──▶  Internals (Weapons/ Effects/ Net/ Besiege/)
```

- Of EWL's namespaces, `Modules/` uses only `EWL.Api`. The internals are visible because it is the same assembly, but it does not use them.
  If a module needs something that the API lacks, first add it to the API (then external mods can use it too).
- The internals do not reference `Modules/`. XML schema types are not brought into the internals, and data is passed using `Api/` types.
- `Api/` is a thin front door and holds no logic.
- Only the projectile management in `Weapons/` calls `Net/`.

The data types in `Api/` (such as `WeaponSpec`) and the XML schema types in `Modules/` (such as `EwlWeaponModule`) are **separate types**.
This is so that XmlSerializer's constraints (public fields, the `Element` base class, an XML-specific `Vector3`, reference names for resources) are not imposed on C# users.
The module does the conversion. XML element names correspond one-to-one with C# field names (directly under `EwlWeapon` = `WeaponSpec`,
`EwlWeapon/Projectile` = `ProjectileSpec`, `Projectile/Explosion` = `ExplosionSpec`).

## Visibility

- Only the documented types in `Api/` and `Modules/` are made `public`. Everything else is `internal`.
- The XML definition classes in `Modules/` are public because of XmlSerializer, but they are an "XML schema", not a C# API.
- Besiege's internal types are not exposed in the public API. `Modding.*` and UnityEngine types can be used.
  Whether to expose a type that is customarily used in mod development, like `BlockBehaviour`, is decided individually as a change to the public surface.

## Weapon flow

1. When a block's simulation starts, `EwlWeaponBehaviour` (or an external mod's custom module) creates a `WeaponSpec` and
   attaches a weapon with `Weapon.Create(block, spec)`. Internally this is a `WeaponDriver`.
2. When the consumer calls `Weapon.TryFire()`, `WeaponDriver` checks the interval, ammo, and reload, and spawns the projectile after the delay.
   `WeaponSpec` is read on every shot, so the consumer can rewrite values before firing (for linking to sliders, etc.).
3. A projectile (`Weapons/Projectile`) is taken from the pool and handles flight, impact, fuse, and explosion. Damage and explosions go through `Besiege/`.
4. On a hit, `Weapon.Hit` and `Weapon.AnyHit` fire (only on the side with execution authority).
5. When the simulation stops, the consumer calls `Weapon.Destroy()`. Even if the weapon that fired disappears, projectiles in flight keep flying to the end.

Projectiles of Besiege's standard cannon, crossbow, and FuelCannon are also sent to `Weapon.AnyHit`. The base game does not pass the shooter on a hit,
so `Besiege/VanillaProjectiles` attaches a watcher component to the projectile prototype (and to the MP host's projectile pool) when the block is initialized,
and on the cloned projectile it finds hits under the same conditions as the base game and reports them. The blast of exploding cannonballs is likewise handled by a component attached to the blast prototype.

## Execution authority and MP

- Whether to process weapons and projectiles is decided by "whether this peer is responsible for this simulation's physics" (`Besiege/Simulation`).
  `IsHost` is not used as a substitute. This is because MP has local simulation (a client simulates by itself).
- SP: Everything is processed locally.
- MP host: The authority for projectile spawning, hits, and damage. It distributes only the projectiles of the global simulation (`Net/ProjectileSync`).
  Spawning and despawning are sent each time, and positions are sent in snapshots at fixed intervals. Weapons are matched by "block" and "order of creation on the block".
- MP client: From the same weapon definition, it spawns visual-only projectiles without hit detection and moves them by velocity, drag, and gravity.
  The position and direction of explosions, and the state of remaining ammo and reloading, are received from the host.
- Local simulation: Projectiles are not distributed, and incoming data is discarded in the meantime.
- Players who join midway are not restored (they see the next projectiles).
- A `Weapon` is **created on all peers**. This is so that the clients' visual projectiles and impact effects come from the same definition.

## Pools

- Projectiles are pooled per key (`ProjectileSpec.PoolKey`; per weapon if empty). They are created in advance up to the limit (`PoolSize`), and when the limit is reached the oldest projectile is reclaimed and fired.
- Effects are pooled per prefab, and anything beyond the limit is not spawned.
- Idle projectiles and effects live under the per-scene home (`EsWeaponsLib (Scene)`), and when the scene changes everything is discarded and rebuilt.

## Input

- Key presses are taken from changes in `IsHeld`. This is because on the MP host, a client's "moment of pressing" is set for only one frame and can be missed.
- Actual keys and emulation (logic blocks, etc.) are separate paths, so look at both.
