# XML Custom Module Reference

> [!NOTE]
> This page is a translation of the generated Japanese reference. This page is generated (`tools/gen-reference.ps1`). Do not edit it by hand; fix the `///` comments in `src/` and regenerate.

All elements of EWL's XML custom modules. For usage, see [Using XML](../guide/xml-recipes.md).

- Write them in the block XML's `<Modules>`, with `modid="0d219960-b202-4906-b29d-8d90a61a89b8"` (EWL's Mod ID) attached.
- Elements other than those marked **required** can be omitted, and omitted elements take their default values. Units are the same as on the C# side (`[m/s]`, `[s]`, `[deg]`).
- Positions and rotations are block-local, and rotations are Euler angles [deg].
- How to read "Syntax": vector = `<ElementName x="0" y="0" z="1" />`; key reference, toggle reference, and slider reference = `<ElementName key="…" />` (`key` is a name defined in the block's `ModuleMapperTypes`); resource name = `<ElementName name="…" />` (a name in `Resources` of the mod that defines the block); mesh = a resource name plus `Position` / `Rotation` / `Scale`; list of `<AudioClip />` = a sequence of `<AudioClip name="…" />`; list of colliders = the same format as the block's `Colliders`.
- Reload ○: When the `Mod.xml` of the mod that defines the block has `<Debug>true</Debug>`, saving the XML while in the game applies it from the next simulation start. Elements with a blank cell require a Besiege restart.

## EwlWeapon

An XML custom module that makes a block a weapon. You can write several on one block (each becomes a separate weapon). Everything except FireKey can be omitted, and omitted elements take their default values.

In C# this corresponds to `WeaponSpec`.

| Element | Syntax | Default | Reload | Description |
|---|---|---|---|---|
| `FireKey` **required** | Key reference | — |  | The fire key (required). Write, in the key attribute, the name of a key defined in the block's ModuleMapperTypes. |
| `HoldToFireToggle` | Toggle reference | none |  | ON: fire continuously while held / OFF: each press toggles continuous fire on and off. If omitted, HoldToFire is used. |
| `ReloadKey` | Key reference | none |  | Pressing it reloads (when MagazineCapacity is 1 or more). |
| `AutoReloadToggle` | Toggle reference | none |  | When ON, reloads automatically when the magazine becomes empty. If omitted, AutoReload is used. |
| `FireRateSlider` | Slider reference | none |  | The reciprocal of the value \[shots/s] is used for FireInterval (takes precedence over the XML value; does not fire at 0 or below). Read on every shot. |
| `FireDelaySlider` | Slider reference | none |  | The value \[s] is added to FireDelay. Read on every shot. |
| `TimeFuseSlider` | Slider reference | none |  | The value \[s] is used for Projectile/TimeFuse (takes precedence over the XML value). The random amount of Projectile/TimeFuseRandomExtra is added separately. Read on every shot; projectiles in flight are not changed. |
| `MagazineCapacitySlider` | Slider reference | none |  | The value, rounded, is used for MagazineCapacity (takes precedence over the XML value). Read at simulation start. |
| `PowerSlider` | Slider reference | none |  | The value is used as Power (on the same scale as Besiege's official Shooting module) to determine the muzzle velocity (takes precedence over the XML MuzzleVelocity). Power is the strength of "launching with a force of 100 × Power \[N] applied for one physics step", and MuzzleVelocity = 100 × Power × physics step \[s] ÷ Projectile/Mass. In regular Besiege, where the physics step is 0.01 s, this is Power ÷ Projectile/Mass \[m/s]. The recoil is also determined from this muzzle velocity. Read on every shot. Not used if Projectile/Mass is 0 or below. |
| `BoosterPowerSlider` | Slider reference | none |  | 100 times the value is used for Projectile/Booster/Thrust \[N] (the same scale as PowerSlider; takes precedence over the XML value). Read on every shot. If the value changes during the simulation, the thrust of projectiles in flight changes too. |
| `ThrustTimeSlider` | Slider reference | none |  | The value \[s] is used for Projectile/Booster/ThrustTime (takes precedence over the XML value; when fuel runs out the thrust stops, and the projectile then flies on inertia). Read on every shot; projectiles in flight are not changed. |
| `HoldToFire` | `true` / `false` | `true` | ○ | The firing mode when there is no HoldToFireToggle. true: fire continuously while held / false: each press toggles continuous fire on and off. |
| `AssetBundle` | Resource name | none | ○ | The AssetBundle in which to look for effect prefabs (MuzzleFlash, Projectile/Trail, Projectile/AttachedEffect, Projectile/Explosion/Effect). On Mac/Linux, if there is a "bundle name + Mac", it is used instead. |
| `MuzzlePosition` | Vector | `(0, 0, 0)` | ○ | The muzzle position. |
| `MuzzleRotation` | Vector | `(0, 0, 0)` | ○ | The firing direction (Euler angles \[deg]). The projectile flies along +Z of this rotation. |
| `MuzzleVelocity` | Number | `100` | ○ | Muzzle velocity \[m/s]. Added to the block's velocity. |
| `EjectVelocity` | Vector | `(0, 0, 0)` | ○ | Velocity added at firing along the projectile's local axes \[m/s] (for pushing a rocket out of its launch tube, etc.). Not included in recoil. |
| `FireInterval` | Number | `0.5` | ○ | Minimum firing interval \[s]. |
| `FireDelay` | Number | `0` | ○ | Fixed delay from the fire request until the projectile comes out \[s]. |
| `FireDelayRandomExtra` | Number | `0` | ○ | Width of the random amount added to the delay \[s] (0 to this value). |
| `Spread` | Number | `0` | ○ | Spread \[deg]. The direction is determined by adding a random vector within a sphere of radius tan(Spread) in front. The projectile's orientation is aligned to this direction too. |
| `RecoilMultiplier` | Number | `1` | ○ | Recoil multiplier. An impulse of the projectile's momentum × this value is applied to the block in the opposite direction (1 is physically correct recoil). |
| `BurstCount` | Integer | `1` | ○ | Number of projectiles fired per fire request. Ammo is consumed per shot, and firing stops when ammo runs out. |
| `BurstInterval` | Number | `0.1` | ○ | Interval between projectiles within a burst \[s]. The next fire request is accepted FireInterval after the last projectile. |
| `TotalAmmo` | Integer | `-1` | ○ | Total ammo (including the rounds in the magazine). Negative means unlimited. Not consumed when the machine's unlimited ammo setting is on (for a weapon that uses a magazine, only the magazine's rounds decrease, and reserve ammo does not). Read only when the weapon is created. |
| `MagazineCapacity` | Integer | `0` | ○ | Magazine capacity. 0 or below means no magazine. When the weapon is created, the magazine is filled from TotalAmmo and the rest becomes reserve ammo. |
| `ReloadTime` | Number | `1` | ○ | Reload time \[s]. It cannot fire while reloading. |
| `AutoReload` | `true` / `false` | `true` | ○ | Reloads automatically when the magazine becomes empty. |
| `ShowLoadedProjectile` | `true` / `false` | `false` | ○ | While the weapon is ready to fire, shows the appearance of the next projectile at the muzzle (mortars, bombs, etc.). It disappears at the moment of firing and returns after the firing interval. It is not shown when out of ammo or while reloading. It is visual only and has no colliders. For the EwlWeapon module, it is also shown during building if the total ammo is not 0 (when creating a weapon from C#, use `Weapon.CreateLoadedProjectilePreview`). |
| `MuzzleFlash` | String | none | ○ | A prefab name in the AssetBundle. |
| `MuzzleFlashPosition` | Vector | `(0, 0, 0)` | ○ | Position of the firing effect (block-local). The firing sound plays here too. |
| `MuzzleFlashRotation` | Vector | `(0, 0, 0)` | ○ | Orientation of the firing effect (Euler angles \[deg], block-local). |
| `MuzzleFlashScale` | Vector | `(1, 1, 1)` | ○ | Scale of the firing effect. |
| `FireSounds` | List of `<AudioClip />` | empty | ○ | Firing sounds (one at random; played at MuzzleFlashPosition). |
| `ReloadSounds` | List of `<AudioClip />` | empty | ○ | Sounds at the start of a reload (one at random). |
| `Projectile` | Child element ([EwlWeapon/Projectile](#ewlweapon-projectile)) | — | ○ | The projectile to fire. Projectiles in flight keep using the ProjectileSpec from the time of firing, so a replacement takes effect from the next projectile (rewriting values of the same instance also affects projectiles in flight). The projectile's shape is fixed per `ProjectileSpec.PoolKey`, and a replacement projectile is not pre-created but is created at the first shot. |

## EwlWeapon/Projectile

The definition of one kind of projectile (pure data). It has the projectile body, hit, fuse, and appearance, plus explosion, propulsion, and guidance. The same instance may be shared by several weapons (rewriting it affects all weapons sharing it).

In C# this corresponds to `ProjectileSpec`.

| Element | Syntax | Default | Reload | Description |
|---|---|---|---|---|
| `Mass` | Number | `1` | ○ | Projectile mass \[kg]. Affects recoil, propulsion acceleration, and physical collisions (the force applied on a hit is determined by ImpactForce). |
| `Drag` | Number | `0` | ○ | Air resistance (Rigidbody.drag). |
| `AngularDrag` | Number | `0` | ○ | Rotational resistance (Rigidbody.angularDrag). |
| `IgnoreGravity` | `true` / `false` | `false` | ○ | If true, it is not affected by gravity. |
| `CollisionMode` | `Discrete` / `Continuous` / `ContinuousDynamic` | `ContinuousDynamic` | ○ | The collision detection mode. The default is ContinuousDynamic so that fast projectiles do not pass through things. |
| `Lifetime` | Number | `20` | ○ | Lifetime \[s]. It disappears after this time even if it does not hit anything. |
| `ColliderEnableDelay` | Number | `0.02` | ○ | Time from firing until the projectile's colliders are enabled \[s]. Prevents interference with your own machine. Make it longer for projectiles with a slow muzzle velocity (rockets, missiles, bombs, etc.). 0 enables them at the same time as firing (negative values are treated as 0). It never collides with the weapon block that fired it, regardless of this value. |
| `Colliders` | List of `<SphereCollider>` / `<CapsuleCollider>` / `<BoxCollider>` | empty | ○ | The same format as the block's Colliders (SphereCollider / CapsuleCollider / BoxCollider). |
| `Friction` | Number | `0.6` | ○ | The projectile's friction coefficient (both static and dynamic friction). |
| `Bounciness` | Number | `0` | ○ | The projectile's bounciness coefficient (0 to 1). |
| `FrictionCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | How it is combined with the other body's friction. |
| `BounceCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | How it is combined with the other body's bounciness. |
| `PoolSize` | Integer | `50` | ○ | The limit on the number of projectiles in flight at once. When the weapon is created, projectiles are pre-created up to this number, and when the limit is reached the oldest projectile is reclaimed and fired. Effects are also limited to this number per prefab (any beyond it are not shown). Clamped to 1 to 1000. |
| `Mesh` | Mesh | none | ○ | A resource name of the mod that defines the block (Position/Rotation/Scale are ProjectileSpec's MeshPosition/MeshRotation/MeshScale). |
| `Texture` | Resource name | none | ○ | The appearance texture. If null, there is no texture (white). |
| `Trail` | String | none | ○ | A prefab name in the AssetBundle. |
| `AttachedEffect` | String | none | ○ | A prefab name in the AssetBundle. |
| `ImpactForce` | Number | `10` | ○ | Applies a force of "projectile speed × this value" \[N/(m/s)] to the rigid body hit. Damage and force are applied only once, on the first hit. |
| `BlockDamage` | Number | `0` | ○ | The damage dealt to a block that has HP on a direct hit (not used for blasts). |
| `EntityDamage` | Number | `0` | ○ | The damage dealt to a level entity (people, animals, etc.) on a direct hit (not used for blasts). |
| `AttachOnHit` | `true` / `false` | `false` | ○ | Sticks to what it hit and stops, and afterward moves together with it (arrows, etc.). On terrain, it stops on the spot. |
| `DespawnOnHit` | `true` / `false` | `false` | ○ | Removes a non-exploding projectile immediately on hit. If false, it disappears 1 second after the hit. |
| `TimeFuse` | Number | `INF` | ○ | Timed fuse \[s]. Detonates this long after firing (a non-exploding projectile disappears). 0 detonates right after firing. The default is infinite (no timed fuse). |
| `TimeFuseRandomExtra` | Number | `0.05` | ○ | Random amount added to the timed fuse \[s] (0 to this value). Decided at firing. Has no effect when TimeFuse is infinite. |
| `DetonationDelay` | Number | `0` | ○ | Delay from the hit to detonation \[s]. |
| `DetonationDelayRandomExtra` | Number | `0` | ○ | Width of the random amount added to the detonation delay \[s] (0 to this value). |
| `Explosion` | Child element ([EwlWeapon/Projectile/Explosion](#ewlweapon-projectile-explosion)) | — | ○ | The explosion when detonated. If Radius is 0, there is no explosion. |
| `Booster` | Child element ([EwlWeapon/Projectile/Booster](#ewlweapon-projectile-booster)) | — | ○ | Propulsion (rocket). If Thrust is 0, there is no propulsion. |
| `Guidance` | Child element ([EwlWeapon/Projectile/Guidance](#ewlweapon-projectile-guidance)) | — | ○ | Guidance (provisional implementation). The target is specified with C# `Weapon.Target` (with XML alone there is no target, and the projectile flies unguided). |

## EwlWeapon/Projectile/Explosion

The definition of one explosion (pure data). It applies AddExplosionForce and the base game's explosion processing to rigid bodies within range.

In C# this corresponds to `ExplosionSpec`.

| Element | Syntax | Default | Reload | Description |
|---|---|---|---|---|
| `Radius` | Number | `0` | ○ | Radius \[m]. If 0, there is no explosion. |
| `Force` | Number | `0` | ○ | The explosionForce of AddExplosionForce. |
| `UpwardsModifier` | Number | `0` | ○ | The upwardsModifier of AddExplosionForce. |
| `OnlyWithExplodingCannonballs` | `true` / `false` | `false` | ○ | Normally treated as a non-exploding projectile, and explodes only when the god tool "Exploding Cannonballs" is enabled. |
| `Effect` | String | none | ○ | A prefab name in the AssetBundle. |
| `FacesHitNormal` | `true` / `false` | `false` | ○ | Orients the explosion effect along the normal of the surface hit. |
| `Sounds` | List of `<AudioClip />` | empty | ○ | Explosion sounds (one at random). |

## EwlWeapon/Projectile/Booster

The projectile's propulsion (rocket) and stabilizing fins. If Thrust is 0, there is no propulsion. On a hit, both thrust and the stabilizing fins stop.

In C# this corresponds to `BoosterSpec`.

| Element | Syntax | Default | Reload | Description |
|---|---|---|---|---|
| `Thrust` | Number | `0` | ○ | Thrust \[N]. Applied forward on the projectile every physics frame (acceleration = Thrust / the projectile's Mass). |
| `ThrustTime` | Number | `INF` | ○ | How long thrust is produced \[s]. Thrust stops this long after firing (fuel runs out), and the projectile then flies on inertia (stabilizing fins and guidance keep working). 0 means no thrust. The default is infinite (until the projectile disappears). The value at the time of firing is used; projectiles in flight are not changed. |
| `Stability` | Number | `0` | ○ | Strength of the weathervane stabilization (stabilizing fins) \[(rad/s²)/(m/s)]. With an angular acceleration of (velocity component perpendicular to the nose) × this value, the nose is pulled toward the direction of flight (independent of the projectile's mass and shape). The larger the sideslip, the stronger the correction, and the nose also follows a trajectory that bends under gravity. It works throughout flight until the hit, regardless of whether thrust is present or burning has ended. If 0, it does not work. It does not change the velocity itself, so sideslip disappears as the nose turns to face forward and thrust is applied. To suppress oscillation, use it together with `ProjectileSpec.AngularDrag`. |
| `MaxSpeed` | Number | `0` | ○ | The upper speed that thrust accelerates to \[m/s]. While the projectile's speed is at or above this value, no thrust is produced (top speed; it can be exceeded by gravity, blasts, etc.). 0 means no limit (decided only by the balance of thrust and drag `ProjectileSpec.Drag`). |

## EwlWeapon/Projectile/Guidance

Guidance (provisional implementation). The steering formula and coefficients may change in the future. The target is specified with `Weapon.Target` and is fixed by its value at the moment `Weapon.TryFire` accepts the shot (projectiles that come out later due to delay or burst share the same target). The target of a projectile in flight does not change; when it is lost (destroyed, respawned, simulation ended) or released, the projectile becomes unguided and does not pick up another target. For 0.2 s after firing it keeps the firing direction, then turns the projectile toward the target by torque. An unguided projectile keeps its heading and flies straight (a projectile fired without a target keeps the firing direction; after losing or releasing the target, the heading at that moment; it does not turn back to the firing direction). It only changes the orientation, so use it in combination with propulsion (`BoosterSpec`). After becoming unguided, propulsion, stabilizing fins, drag, gravity, and lifetime keep working as before.

In C# this corresponds to `GuidanceSpec`.

| Element | Syntax | Default | Reload | Description |
|---|---|---|---|---|
| `Gain` | Number | `0` | ○ | Guidance strength (coefficient of the turning torque). If 0, there is no guidance. |
| `ReleaseAngle` | Number | `0` | ○ | Release angle \[deg]. When the angle between the projectile's nose and the direction from the projectile to the target exceeds this value, guidance is released (so that a projectile that missed and passed the target does not turn back). It is the half-angle of a cone around the nose; 90 means it keeps tracking as long as the target is in the forward hemisphere. Checked from right after firing. 0 means no release by angle. |

## EwlBlockPhysics

An XML custom module that configures the block's own physics (joint strength, physics material, joint rotation axes, additional colliders). Every element can be omitted, and omitted items stay at the Besiege base game's values.

| Element | Syntax | Default | Reload | Description |
|---|---|---|---|---|
| `JointStrength` | Number | `-1` | ○ | Breaking force and breaking torque of the joint with the parent block. If omitted, nothing changes. 0 is a joint that breaks under any force at all. An unbreakable joint is INF. |
| `Material` | Child element ([EwlBlockPhysics/Material](#ewlblockphysics-material)) | — | ○ | If written, sets a physics material on all of the block's colliders. |
| `Rotation` | Child element ([EwlBlockPhysics/Rotation](#ewlblockphysics-rotation)) | — | ○ | If written, sets the joint's rotation axes. |
| `AdditionalColliders` | List of `<SphereCollider>` / `<CapsuleCollider>` / `<BoxCollider>` | empty |  | Colliders added to the block. The same format as the block's Colliders (the layer and trigger attributes are the same too), and BoxCollider can also have a Center. With layer="12", other blocks can be placed there, and the placed block becomes a face that connects to this block at simulation start. A BoxCollider can be placed on any of the six faces, at the position of the clicked point rounded to a one-cell unit from the face center. Sphere/Capsule, as in vanilla, are placed at the collider's Position + the normal of the face hit × 0.5 (not the point hit). If Material is written, it applies to these too. |

## EwlBlockPhysics/Material

| Element | Syntax | Default | Reload | Description |
|---|---|---|---|---|
| `Friction` | Number | `0.6` | ○ | Friction coefficient (both static and dynamic friction). |
| `Bounciness` | Number | `0` | ○ | Bounciness coefficient (0 to 1). |
| `FrictionCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | How it is combined with the other body's friction. |
| `BounceCombine` | `Average` / `Minimum` / `Multiply` / `Maximum` | `Average` | ○ | How it is combined with the other body's bounciness. |

## EwlBlockPhysics/Rotation

The axes are Locked / Limited / Free. Spring and Damper are the force that returns the rotation to its original orientation, and Limit is the range of motion of a Limited axis \[deg].

| Element | Syntax | Default | Reload | Description |
|---|---|---|---|---|
| `AxisX` | `Locked` / `Limited` / `Free` | `Locked` | ○ | Rotation about the X axis (Locked = fixed / Limited = up to Limit / Free = unrestricted). |
| `AxisY` | `Locked` / `Limited` / `Free` | `Locked` | ○ | Rotation about the Y axis. |
| `AxisZ` | `Locked` / `Limited` / `Free` | `Locked` | ○ | Rotation about the Z axis. |
| `Spring` | Number | `0` | ○ | Strength of the spring that returns it to its original orientation. If 0, it is not returned. |
| `Damper` | Number | `0` | ○ | Damping of the spring. |
| `Limit` | Number | `0` | ○ | Range of motion of a Limited axis \[deg]. |
