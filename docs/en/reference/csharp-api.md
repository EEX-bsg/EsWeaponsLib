# C# API Reference

> [!NOTE]
> This page is a translation of the generated Japanese reference. This page is generated (`tools/gen-reference.ps1`). Do not edit it by hand; fix the `///` comments in `src/` and regenerate.

The public types of the `EWL.Api` namespace. For usage, see [Using C#](../guide/csharp.md).

- [Weapon](#weapon) — The weapon part.
- [WeaponSpec](#weaponspec) — The definition of one weapon (pure data).
- [ProjectileSpec](#projectilespec) — The definition of one kind of projectile (pure data).
- [ExplosionSpec](#explosionspec) — The definition of one explosion (pure data).
- [BoosterSpec](#boosterspec) — The projectile's propulsion (rocket) and stabilizing fins.
- [GuidanceSpec](#guidancespec) — Guidance (provisional implementation).
- [ColliderShape](#collidershape) — One projectile collider.
- [ColliderKind](#colliderkind) — The shape of a projectile collider.
- [ProjectileHit](#projectilehit) — One projectile hit (the argument of `Weapon.Hit` / `Weapon.AnyHit`).
- [HitKind](#hitkind) — The kind of hit.
- [Attacker](#attacker) — The attacker tag (whose projectile it was, who detonated it).
- [BundleAssets](#bundleassets) — Retrieves assets from a `ModAssetBundle`.
- [HitSource](#hitsource) — Which weapon the projectile came from.

## Weapon

`public sealed class EWL.Api.Weapon`

The weapon part. The consumer (BlockModuleBehaviour, etc.) calls `Weapon.Create` at simulation start and `Weapon.Destroy` at simulation stop. The consumer decides when to fire and calls `Weapon.TryFire`. EWL handles projectiles, impacts, explosions, and effects.

| Member | Description |
|---|---|
| `static Weapon Create(BlockBehaviour block, WeaponSpec spec)` | Attaches a weapon to a block. You may attach several to one block. |
| `static Weapon[] GetAll(GameObject block)` | The EWL weapons attached to a block (including those attached by other mods). |
| `static GameObject CreateLoadedProjectilePreview(BlockBehaviour block, WeaponSpec spec)` | Places the appearance of a loaded projectile (the same as `WeaponSpec.ShowLoadedProjectile`) on a block being built. It is visual only and has no hit detection. The caller decides whether to show it (the Spec's ShowLoadedProjectile is not consulted). The position and the projectile's shape are determined by the Spec at this time, and do not follow if the Spec is rewritten afterward. It is not shown during simulation (what is shown on the simulation side is `WeaponSpec.ShowLoadedProjectile`). It disappears together with the block, and to remove it partway, Destroy the return value. |
| `bool TryFire()` | Requests firing. On the side with execution authority (SP, MP Host, MP Client local simulation), it is accepted and returns true only when the interval, ammo, and not-reloading conditions allow. If BurstCount is 2 or more, this one call fires a burst. |
| `bool TryReload()` | Requests a reload. On the side with execution authority, it is accepted and returns true only when the weapon uses a magazine, the magazine has room, there is reserve ammo, and it is not already reloading. |
| `void Destroy()` | Detaches the weapon. Projectiles in flight remain until the simulation ends. |
| `WeaponSpec Spec { get; set; }` | The definition. It is read on every shot, so rewriting it takes effect from the next projectile. However, the total ammo (TotalAmmo) and the initial magazine fill are used only at `Weapon.Create`. |
| `int Ammo { get; }` | Remaining ammo ready to fire (for a weapon that uses a magazine, the rounds in the magazine). Negative means unlimited. |
| `int ReserveAmmo { get; }` | Reserve ammo outside the magazine. Negative means unlimited. 0 for a weapon that does not use a magazine. |
| `bool IsReloading { get; }` | True while reloading (cannot fire). |
| `string PoolKey { get; }` | The key of the projectile pool this weapon uses. If `ProjectileSpec.PoolKey` is empty, a key dedicated to this weapon. null if destroyed. The pool's home is "EsWeaponsLib (Scene)/ProjectilePool/(key)" directly under the scene, and idle and in-flight projectiles are its children (a projectile stuck to what it hit is a child of that object). Touching projectiles directly is not recommended (if an operation is missing, please propose adding it to the API). The layout of the home may change until 1.0. |
| `bool IsAlive { get; }` | False if destroyed. |
| `event Action Fired` | Each time a shot is fired (ammo is consumed). Per projectile in a burst. The projectile is spawned after the delay. |
| `event Action ReloadStarted` | When a reload starts. The magazine is filled after the reload time. |
| `event Action<ProjectileHit> Hit` | A projectile of this weapon hit something (direct hit or blast). Fires only on the side with execution authority (SP, MP host, local simulation). |
| `static event Action<ProjectileHit> AnyHit` | Fires every time a projectile hits, regardless of which weapon fired it. Used by the side that was hit to learn who shot it. It covers EWL projectiles (including weapons of other mods that use EWL) and the projectiles of some vanilla weapons (cannon, crossbow, FuelCannon, including the blast of the god tool "Exploding Cannonballs"). Which one it is can be told from `ProjectileHit.Source`. Other vanilla weapons (grapeshot cannon, flamethrower, etc.) may be supported in the future. Projectiles of mods using other mechanisms are not covered. Fires only on the side with execution authority. It is a static event, so always unsubscribe once you have subscribed. |

## WeaponSpec

`public sealed class EWL.Api.WeaponSpec`

The definition of one weapon (pure data). It holds the values of the launcher itself and the definition of the projectile to fire, `WeaponSpec.Projectile`. `Weapon` reads these values on every shot, so rewriting them before firing takes effect from that projectile (for linking to sliders, etc.). Every item has a default. Positions and rotations are block-local, and rotations are Euler angles \[deg]. Do not assign null to the definitions inside (Projectile, Explosion, Booster, Guidance) (leave unused features at their defaults = disabled).

| Field | Type | Default | Description |
|---|---|---|---|
| `MuzzlePosition` | `Vector3` | `(0, 0, 0)` | The muzzle position. |
| `MuzzleRotation` | `Vector3` | `(0, 0, 0)` | The firing direction (Euler angles \[deg]). The projectile flies along +Z of this rotation. |
| `MuzzleVelocity` | `float` | `100` | Muzzle velocity \[m/s]. Added to the block's velocity. |
| `EjectVelocity` | `Vector3` | `(0, 0, 0)` | Velocity added at firing along the projectile's local axes \[m/s] (for pushing a rocket out of its launch tube, etc.). Not included in recoil. |
| `FireInterval` | `float` | `0.5` | Minimum firing interval \[s]. |
| `FireDelay` | `float` | `0` | Fixed delay from the fire request until the projectile comes out \[s]. |
| `FireDelayRandomExtra` | `float` | `0` | Width of the random amount added to the delay \[s] (0 to this value). |
| `Spread` | `float` | `0` | Spread \[deg]. The direction is determined by adding a random vector within a sphere of radius tan(Spread) in front. The projectile's orientation is aligned to this direction too. |
| `RecoilMultiplier` | `float` | `1` | Recoil multiplier. An impulse of the projectile's momentum × this value is applied to the block in the opposite direction (1 is physically correct recoil). |
| `BurstCount` | `int` | `1` | Number of projectiles fired per fire request. Ammo is consumed per shot, and firing stops when ammo runs out. |
| `BurstInterval` | `float` | `0.1` | Interval between projectiles within a burst \[s]. The next fire request is accepted FireInterval after the last projectile. |
| `TotalAmmo` | `int` | `-1` | Total ammo (including the rounds in the magazine). Negative means unlimited. Not consumed when the machine's unlimited ammo setting is on (for a weapon that uses a magazine, only the magazine's rounds decrease, and reserve ammo does not). Read only when the weapon is created. |
| `MagazineCapacity` | `int` | `0` | Magazine capacity. 0 or below means no magazine. When the weapon is created, the magazine is filled from TotalAmmo and the rest becomes reserve ammo. |
| `ReloadTime` | `float` | `1` | Reload time \[s]. It cannot fire while reloading. |
| `AutoReload` | `bool` | `true` | Reloads automatically when the magazine becomes empty. |
| `ShowLoadedProjectile` | `bool` | `false` | While the weapon is ready to fire, shows the appearance of the next projectile at the muzzle (mortars, bombs, etc.). It disappears at the moment of firing and returns after the firing interval. It is not shown when out of ammo or while reloading. It is visual only and has no colliders. For the EwlWeapon module, it is also shown during building if the total ammo is not 0 (when creating a weapon from C#, use `Weapon.CreateLoadedProjectilePreview`). |
| `MuzzleFlash` | `GameObject` | none | The firing effect prefab (obtained from an AssetBundle, etc.). If null, there is none. |
| `MuzzleFlashPosition` | `Vector3` | `(0, 0, 0)` | Position of the firing effect (block-local). The firing sound plays here too. |
| `MuzzleFlashRotation` | `Vector3` | `(0, 0, 0)` | Orientation of the firing effect (Euler angles \[deg], block-local). |
| `MuzzleFlashScale` | `Vector3` | `(1, 1, 1)` | Scale of the firing effect. |
| `FireSounds` | `List<AudioClip>` | empty | Firing sounds (one at random; played at MuzzleFlashPosition). |
| `ReloadSounds` | `List<AudioClip>` | empty | Sounds at the start of a reload (one at random). |
| `Projectile` | `ProjectileSpec` | — | The projectile to fire. Projectiles in flight keep using the ProjectileSpec from the time of firing, so a replacement takes effect from the next projectile (rewriting values of the same instance also affects projectiles in flight). The projectile's shape is fixed per `ProjectileSpec.PoolKey`, and a replacement projectile is not pre-created but is created at the first shot. |

## ProjectileSpec

`public sealed class EWL.Api.ProjectileSpec`

The definition of one kind of projectile (pure data). It has the projectile body, hit, fuse, and appearance, plus explosion, propulsion, and guidance. The same instance may be shared by several weapons (rewriting it affects all weapons sharing it).

| Field | Type | Default | Description |
|---|---|---|---|
| `Mass` | `float` | `1` | Projectile mass \[kg]. Affects recoil, propulsion acceleration, and physical collisions (the force applied on a hit is determined by ImpactForce). |
| `Drag` | `float` | `0` | Air resistance (Rigidbody.drag). |
| `AngularDrag` | `float` | `0` | Rotational resistance (Rigidbody.angularDrag). |
| `IgnoreGravity` | `bool` | `false` | If true, it is not affected by gravity. |
| `CollisionMode` | `CollisionDetectionMode` | `ContinuousDynamic` | The collision detection mode. The default is ContinuousDynamic so that fast projectiles do not pass through things. |
| `Lifetime` | `float` | `20` | Lifetime \[s]. It disappears after this time even if it does not hit anything. |
| `ColliderEnableDelay` | `float` | `0.02` | Time from firing until the projectile's colliders are enabled \[s]. Prevents interference with your own machine. Make it longer for projectiles with a slow muzzle velocity (rockets, missiles, bombs, etc.). 0 enables them at the same time as firing (negative values are treated as 0). It never collides with the weapon block that fired it, regardless of this value. |
| `Colliders` | `List<ColliderShape>` | empty | Colliders (projectile-local). If empty, a sphere with radius 0.1. |
| `Friction` | `float` | `0.6` | The projectile's friction coefficient (both static and dynamic friction). |
| `Bounciness` | `float` | `0` | The projectile's bounciness coefficient (0 to 1). |
| `FrictionCombine` | `PhysicMaterialCombine` | `Average` | How it is combined with the other body's friction. |
| `BounceCombine` | `PhysicMaterialCombine` | `Average` | How it is combined with the other body's bounciness. |
| `PoolSize` | `int` | `50` | The limit on the number of projectiles in flight at once. When the weapon is created, projectiles are pre-created up to this number, and when the limit is reached the oldest projectile is reclaimed and fired. Effects are also limited to this number per prefab (any beyond it are not shown). Clamped to 1 to 1000. |
| `PoolKey` | `string` | none | The key for sharing a projectile pool. Setting the same value for projectiles of the same kind reuses the projectile objects. The projectile's shape (Colliders) is fixed per key to what was created first. If null or an empty string, a dedicated pool per weapon (the actual key is `Weapon.PoolKey`). |
| `Mesh` | `Mesh` | none | The appearance. If null, a sphere is displayed. |
| `Texture` | `Texture` | none | The appearance texture. If null, there is no texture (white). |
| `MeshPosition` | `Vector3` | `(0, 0, 0)` | Position of the appearance (projectile-local). Not used when Mesh is null. |
| `MeshRotation` | `Vector3` | `(0, 0, 0)` | Orientation of the appearance (Euler angles \[deg], projectile-local). Not used when Mesh is null. |
| `MeshScale` | `Vector3` | `(1, 1, 1)` | Scale of the appearance. Not used when Mesh is null. |
| `Trail` | `GameObject` | none | An effect prefab that follows the projectile and is detached and faded out on hit or disappearance. |
| `AttachedEffect` | `GameObject` | none | An effect prefab that follows the projectile and disappears with it (a projectile's glow, flame, etc.). |
| `ImpactForce` | `float` | `10` | Applies a force of "projectile speed × this value" \[N/(m/s)] to the rigid body hit. Damage and force are applied only once, on the first hit. |
| `BlockDamage` | `float` | `0` | The damage dealt to a block that has HP on a direct hit (not used for blasts). |
| `EntityDamage` | `float` | `0` | The damage dealt to a level entity (people, animals, etc.) on a direct hit (not used for blasts). |
| `AttachOnHit` | `bool` | `false` | Sticks to what it hit and stops, and afterward moves together with it (arrows, etc.). On terrain, it stops on the spot. |
| `DespawnOnHit` | `bool` | `false` | Removes a non-exploding projectile immediately on hit. If false, it disappears 1 second after the hit. |
| `TimeFuse` | `float` | `INF` | Timed fuse \[s]. Detonates this long after firing (a non-exploding projectile disappears). 0 detonates right after firing. The default is infinite (no timed fuse). |
| `TimeFuseRandomExtra` | `float` | `0.05` | Random amount added to the timed fuse \[s] (0 to this value). Decided at firing. Has no effect when TimeFuse is infinite. |
| `DetonationDelay` | `float` | `0` | Delay from the hit to detonation \[s]. |
| `DetonationDelayRandomExtra` | `float` | `0` | Width of the random amount added to the detonation delay \[s] (0 to this value). |
| `Explosion` | `ExplosionSpec` | — | The explosion when detonated. If Radius is 0, there is no explosion. |
| `Booster` | `BoosterSpec` | — | Propulsion (rocket). If Thrust is 0, there is no propulsion. |
| `Guidance` | `GuidanceSpec` | — | Guidance (provisional implementation). If Gain is 0, there is no guidance. |

## ExplosionSpec

`public sealed class EWL.Api.ExplosionSpec`

The definition of one explosion (pure data). It applies AddExplosionForce and the base game's explosion processing to rigid bodies within range.

| Field | Type | Default | Description |
|---|---|---|---|
| `Radius` | `float` | `0` | Radius \[m]. If 0, there is no explosion. |
| `Force` | `float` | `0` | The explosionForce of AddExplosionForce. |
| `UpwardsModifier` | `float` | `0` | The upwardsModifier of AddExplosionForce. |
| `OnlyWithExplodingCannonballs` | `bool` | `false` | Normally treated as a non-exploding projectile, and explodes only when the god tool "Exploding Cannonballs" is enabled. |
| `Effect` | `GameObject` | none | The explosion effect prefab. If null, there is none. |
| `FacesHitNormal` | `bool` | `false` | Orients the explosion effect along the normal of the surface hit. |
| `Sounds` | `List<AudioClip>` | empty | Explosion sounds (one at random). |

## BoosterSpec

`public sealed class EWL.Api.BoosterSpec`

The projectile's propulsion (rocket) and stabilizing fins. If Thrust is 0, there is no propulsion. On a hit, both thrust and the stabilizing fins stop.

| Field | Type | Default | Description |
|---|---|---|---|
| `Thrust` | `float` | `0` | Thrust \[N]. Applied forward on the projectile every physics frame (acceleration = Thrust / the projectile's Mass). |
| `ThrustTime` | `float` | `INF` | How long thrust is produced \[s]. Thrust stops this long after firing (fuel runs out), and the projectile then flies on inertia (stabilizing fins and guidance keep working). 0 means no thrust. The default is infinite (until the projectile disappears). The value at the time of firing is used; projectiles in flight are not changed. |
| `Stability` | `float` | `0` | Strength of the weathervane stabilization (stabilizing fins) \[(rad/s²)/(m/s)]. With an angular acceleration of (velocity component perpendicular to the nose) × this value, the nose is pulled toward the direction of flight (independent of the projectile's mass and shape). The larger the sideslip, the stronger the correction, and the nose also follows a trajectory that bends under gravity. It works throughout flight until the hit, regardless of whether thrust is present or burning has ended. If 0, it does not work. It does not change the velocity itself, so sideslip disappears as the nose turns to face forward and thrust is applied. To suppress oscillation, use it together with `ProjectileSpec.AngularDrag`. |

## GuidanceSpec

`public sealed class EWL.Api.GuidanceSpec`

Guidance (provisional implementation). Including this type and the XML Guidance element, both the way targets are decided and the steering are planned to be changed in a breaking way. At the moment of firing, it takes as the target the nearest of the core blocks of enemy machines within a fixed angle ahead of the firing direction, and does not change it during flight. If there is no target, it flies keeping the firing direction. It only changes the projectile's orientation by torque, so use it in combination with propulsion (`BoosterSpec`).

| Field | Type | Default | Description |
|---|---|---|---|
| `Gain` | `float` | `0` | Guidance strength (coefficient of the turning torque). If 0, there is no guidance. |

## ColliderShape

`public sealed class EWL.Api.ColliderShape`

One projectile collider. Size is the size of a Box, Radius is for Sphere/Capsule, and Height/Direction (0=X, 1=Y, 2=Z) are for Capsule.

| Field | Type | Default | Description |
|---|---|---|---|
| `Kind` | `ColliderKind` | `Sphere` | The shape. |
| `Position` | `Vector3` | `(0, 0, 0)` | Position (projectile-local). |
| `Rotation` | `Vector3` | `(0, 0, 0)` | Orientation (Euler angles \[deg], projectile-local). |
| `Size` | `Vector3` | `(1, 1, 1)` | Size of a Box \[m]. |
| `Radius` | `float` | `0.1` | Radius of a Sphere or Capsule \[m]. |
| `Height` | `float` | `1` | Height of a Capsule \[m]. |
| `Direction` | `int` | `2` | The axis of a Capsule's orientation (0 = X, 1 = Y, 2 = Z). |

## ColliderKind

`public enum EWL.Api.ColliderKind`

The shape of a projectile collider.

| Value | Description |
|---|---|
| `Sphere` |  |
| `Capsule` |  |
| `Box` |  |

## ProjectileHit

`public sealed class EWL.Api.ProjectileHit`

One projectile hit (the argument of `Weapon.Hit` / `Weapon.AnyHit`). A direct hit is once, and a blast is once per rigid body. The information about the shooter is the value at the time of firing. Which weapon the projectile came from is in `ProjectileHit.Source` (`Weapon.AnyHit` also includes some vanilla weapons).

| Member | Description |
|---|---|
| `HitSource Source { get; }` | Which weapon the projectile came from. More kinds may be added in the future, so ignore values you do not know. |
| `Weapon Weapon { get; }` | The weapon that fired (EWL projectiles only. null for vanilla projectiles). If it was destroyed while the projectile was in flight, `Weapon.IsAlive` is false. |
| `BlockBehaviour ShooterBlock { get; }` | The block that fired (the simulation instance. EWL projectiles only; null for vanilla projectiles). null after it is destroyed or the simulation has ended. |
| `Attacker Shooter { get; }` | The shooter (the value at the time of firing. It remains even if the block that fired disappears). null when it cannot be determined for a vanilla projectile. |
| `BlockBehaviour Block { get; }` | The block that was hit (for a block with child blocks, the child that was hit). null for terrain, entities, etc. |
| `Collider Collider { get; }` | The collider that was hit (direct hits only. null for blasts, and for vanilla projectiles where the collider hit is unknown). |
| `Vector3 Point { get; }` | For a direct hit, the contact point; for a blast, the center of the explosion (world coordinates). |
| `HitKind Kind { get; }` | Direct hit or blast. More kinds may be added in the future, so ignore values you do not know. |

## HitKind

`public enum EWL.Api.HitKind`

The kind of hit.

| Value | Description |
|---|---|
| `Direct` | The projectile hit directly. |
| `Explosion` | A blast hit. |

## Attacker

`public sealed class EWL.Api.Attacker`

The attacker tag (whose projectile it was, who detonated it). It holds the values at the time of firing or detonation, so it does not change even if the shooter respawns, spectates, or leaves. Recording is done only on the side with execution authority (SP, MP host, local simulation). Whether to count it as a kill or death is decided by the receiving side.

| Member | Description |
|---|---|
| `static Attacker GetDetonator(BlockBehaviour explosive)` | The person who detonated a vanilla bomb that has exploded. null when unknown, not yet exploded, not a bomb, or not on the side with execution authority. Chains of sympathetic detonation are traced to the root cause. A direct hit or blast of an EWL projectile (including sympathetic detonations that occurred within it) is attributed to its shooter; a direct hit of a vanilla arrow or shell, the blast of the god tool "Exploding Cannonballs", or the collision of a launched rocket is attributed to its owner; and a block collision such as a fall or ramming is attributed to the bomb's owner (self-detonation). Since it does not infer by position, bombs detonated by the blast or ignition of grenades, rockets, or fuel barrels, or by projectiles of other mods, are unknown (some may be supported in the future). |
| `ushort PlayerId { get; }` | The player's NetworkId. 0 in SP. The number of a player who left may be reused for a player who joins later (to check whether they are the same person, compare with `Attacker.Player`). |
| `MPTeam Team { get; }` | The team. If the shooting block is known, that block's team (the value at simulation start); if not, the player's team at that time. `MPTeam.None` in SP. |
| `Player Player { get; }` | The MP player. Since it is a separate reference per connection, even if the number is reused, `Equals` can tell it is a different person. `Name` can still be read after they leave. null in SP. |

## BundleAssets

`public static class EWL.Api.BundleAssets`

Retrieves assets from a `ModAssetBundle`. The base game's `LoadAsset` reads all of the bundle's assets every time it is called (empty-bundle detection), so the result is remembered per bundle, name, and type. Names that were not found are also remembered and are not read from the second time on.

The base game reads a bundle asynchronously only once, when the mod is discovered, and normally does not release it. The simulation start can be earlier than the load completion (such as joining MP right after launch). If it is not yet loaded (`!bundle.Loaded`), subscribe to `bundle.OnLoad` to retrieve again after completion, and unsubscribe when the simulation stops. Subscribing to `OnLoad` of an already loaded bundle makes the base game reload all assets, so subscribe only when it is not yet loaded.

| Member | Description |
|---|---|
| `static T Load<T>(ModAssetBundle bundle, string name)` | null if not yet loaded, on a load error, or if not found. Call from the main thread. Retrieve and hold it at simulation start or the like, and do not call it every frame. |

## HitSource

`public enum EWL.Api.HitSource`

Which weapon the projectile came from.

| Value | Description |
|---|---|
| `Ewl` | An EWL weapon (XML custom module or C# API, including those of other mods that use EWL). |
| `VanillaCannon` | A shell of Besiege's standard cannon (the grapeshot cannon is not included). Includes the blast of the god tool "Exploding Cannonballs". |
| `VanillaCrossbow` | An arrow of Besiege's standard crossbow. |
| `VanillaFuelCannon` | A fireball of Besiege's standard FuelCannon. Whom it hit is found by EWL redoing the same determination as the base game, so it may rarely differ from the base game. |
