# Using XML

Commonly used combinations for `EwlWeapon`. For elements and default values not covered here, see the [XML Reference](../reference/xml-modules.md).
The examples omit common parts such as `modid` and `FireKey`.

## How to fire

### Auto-fire and toggle

```xml
<HoldToFire>false</HoldToFire>
```

- `true` (default): Fires every `FireInterval` while the key is held.
- `false`: Each press toggles continuous fire on and off.

To let the player choose, use a toggle (if a toggle exists, it takes precedence over `HoldToFire`).

```xml
<ModuleMapperTypes>
    <Key displayName="Fire" key="fire" default="C" />
    <Toggle displayName="Hold to fire" key="hold" default="true" />
</ModuleMapperTypes>
...
<HoldToFireToggle key="hold" />
```

### Delay, spread, and recoil

```xml
<FireDelay>0.2</FireDelay>               <!-- Time from the press until the projectile comes out [s] -->
<FireDelayRandomExtra>0.05</FireDelayRandomExtra>  <!-- Adds 0 to this value [s] to the delay -->
<Spread>0.5</Spread>                     <!-- Spread [deg] -->
<RecoilMultiplier>0.5</RecoilMultiplier> <!-- Recoil multiplier. 1 is physically correct recoil -->
```

### Burst

One press fires three shots, then waits `FireInterval` after the last one.

```xml
<BurstCount>3</BurstCount>
<BurstInterval>0.08</BurstInterval>
<FireInterval>1</FireInterval>
```

### Multiple weapons on one block

Writing several `EwlWeapon` elements makes each a separate weapon. With different keys they can be fired separately, and with the same key they fire simultaneously.

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

## Ammo, magazines, and reloading

```xml
<TotalAmmo>40</TotalAmmo>             <!-- Total ammo. Negative (default -1) means unlimited -->
<MagazineCapacity>8</MagazineCapacity> <!-- Magazine capacity. 0 (default) means no magazine -->
<ReloadTime>2.5</ReloadTime>          <!-- [s] -->
<AutoReload>true</AutoReload>         <!-- Reload automatically when empty -->
<ReloadKey key="reload" />            <!-- Key for manual reload (optional) -->
<ReloadSounds>
    <AudioClip name="reload" />
</ReloadSounds>
```

- A weapon that uses a magazine fills the magazine from the total ammo at simulation start, and the rest becomes reserve ammo. It cannot fire while reloading.
- When the machine's unlimited ammo is enabled, the total ammo is not consumed (the magazine's rounds still decrease, so reloading is still needed).
- To let the player choose automatic reloading, use `AutoReloadToggle`.

## Changing values with sliders

As with keys and toggles, define them in `ModuleMapperTypes` and reference them by `key`. A slider's value takes precedence over the XML value.

```xml
<ModuleMapperTypes>
    <Key displayName="Fire" key="fire" default="C" />
    <Slider displayName="Rate of fire" key="rate" min="0.5" max="10" default="2" />
    <Slider displayName="Fuse" key="fuse" min="0.5" max="10" default="3" />
</ModuleMapperTypes>
...
<FireRateSlider key="rate" />   <!-- [shots/s]. Its reciprocal is used for FireInterval. Does not fire at 0 or below -->
<TimeFuseSlider key="fuse" />   <!-- [s]. Used for Projectile/TimeFuse (0 detonates right after firing. It cannot be made to have no fuse) -->
```

| Element | How the value is used |
|---|---|
| `FireRateSlider` | 1 / value becomes `FireInterval` |
| `FireDelaySlider` | The value is added to `FireDelay` |
| `TimeFuseSlider` | The value becomes `Projectile/TimeFuse` |
| `MagazineCapacitySlider` | Rounded to an integer and used as `MagazineCapacity` |
| `PowerSlider` | The value is used as Power for the muzzle velocity (`MuzzleVelocity` = 100 × value × physics step ÷ `Projectile/Mass`. In regular Besiege it is value ÷ mass [m/s]) |
| `BoosterPowerSlider` | 100 × value becomes `Projectile/Booster/Thrust` [N] |
| `ThrustTimeSlider` | The value becomes `Projectile/Booster/ThrustTime` [s] |

The Power of `PowerSlider` is on the same scale as Besiege's official Shooting module (the strength of launching with a force of 100 × Power [N] applied for one physics step). A cannon you have been tuning by Power can be used as is, without converting the values.

Even if you later change a slider's `min` or `max` in the block XML, machines already saved keep the value from before the change and fire with that value.

## Projectiles

### Physics

```xml
<Projectile>
    <Mass>2</Mass>                  <!-- [kg]. Affects recoil and the impact on hit -->
    <Drag>0.05</Drag>               <!-- Air resistance -->
    <IgnoreGravity>false</IgnoreGravity>
    <Lifetime>10</Lifetime>         <!-- [s]. Disappears after this time even if it hits nothing -->
    <ImpactForce>10</ImpactForce>   <!-- Applies a force of projectile speed × this value to the rigid body hit -->
</Projectile>
```

### Appearance and colliders

For `Mesh` and `Texture`, use names written in `Resources` of the `Mod.xml` of the mod that defines the block. Colliders use the same format as the block's `Colliders`.
If you do not write a collider, it is a sphere with radius 0.1.

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

### Damage, sticking projectiles, and vanishing projectiles

```xml
<BlockDamage>1</BlockDamage>     <!-- Damage to blocks that have HP -->
<EntityDamage>50</EntityDamage>  <!-- Damage to people, animals, and so on -->
<AttachOnHit>true</AttachOnHit>  <!-- Sticks to what it hit and moves with it (arrows, etc.) -->
<DespawnOnHit>true</DespawnOnHit> <!-- Removes a non-exploding projectile immediately on hit. By default it disappears 1 second after the hit -->
```

Damage and the impact force are applied only once, on the first hit.

### Fuses and explosions

```xml
<Projectile>
    <TimeFuse>3</TimeFuse>                 <!-- Detonates 3 seconds after firing. If not written, there is no timed fuse (0 detonates right after firing) -->
    <DetonationDelay>0.05</DetonationDelay> <!-- Delay from the hit to detonation [s] -->
    <Explosion>
        <Radius>4</Radius>                 <!-- 0 (default) means no explosion -->
        <Force>8000</Force>
        <UpwardsModifier>0.5</UpwardsModifier>
    </Explosion>
</Projectile>
```

Setting `Explosion/OnlyWithExplodingCannonballs` to `true` makes it normally not explode, and explode only when the god tool "Exploding Cannonballs" is enabled.

## Effects and sounds

Effects are specified by prefab name inside an AssetBundle. There is one `AssetBundle` per weapon, and all effects are looked up in it.
Sounds are specified by the name of an `AudioClip` in `Resources`, and when you write several, one is played at random each time.

```xml
<!-- Resources in Mod.xml -->
<AssetBundle name="fx" path="fx" />
<AudioClip name="fire1" path="fire1.ogg" />
<AudioClip name="boom" path="boom.ogg" />
```

```xml
<!-- EwlWeapon of the block -->
<AssetBundle name="fx" />
<MuzzleFlash>MuzzleFlash</MuzzleFlash>
<MuzzleFlashPosition x="0" y="0" z="1.3" />
<FireSounds>
    <AudioClip name="fire1" />
</FireSounds>
<Projectile>
    <Trail>SmokeTrail</Trail>            <!-- Attached to the projectile; detached and faded out on hit or disappearance -->
    <AttachedEffect>Glow</AttachedEffect> <!-- Attached to the projectile; disappears with it -->
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

On Mac and Linux, if there is an AssetBundle named "bundle name + `Mac`", it is used instead.

## Showing a waiting projectile

Like a mortar or a bomb, this shows the projectile before firing at the muzzle. It disappears at the moment of firing and comes back once the weapon can fire again. It also shows the projectile at the muzzle during building (except for weapons whose `TotalAmmo` is 0).

```xml
<ShowLoadedProjectile>true</ShowLoadedProjectile>
```

## Rockets

Adding propulsion (`Booster`) makes the projectile accelerate by itself. To merely push it out of the launch tube, set the muzzle velocity to 0 and use `EjectVelocity`.

```xml
<MuzzleVelocity>0</MuzzleVelocity>
<EjectVelocity x="0" y="0" z="5" />     <!-- Velocity added along the projectile's local axes [m/s]. Not included in recoil -->
<Projectile>
    <ColliderEnableDelay>0.3</ColliderEnableDelay> <!-- Lengthen it so that slow projectiles do not hit your own machine -->
    <AngularDrag>4</AngularDrag>  <!-- Suppresses oscillation when turning to face forward with stabilizing fins -->
    <Booster>
        <Thrust>400</Thrust>         <!-- [N] -->
        <ThrustTime>2</ThrustTime>   <!-- How long thrust is produced [s]. If not written, until the projectile disappears -->
        <Stability>0.5</Stability>   <!-- Stabilizing fins: pull the nose toward the direction of flight. The nose also follows a trajectory that drops under gravity -->
        <MaxSpeed>150</MaxSpeed>     <!-- Top speed [m/s]. Thrust does not accelerate beyond this. If not written, no limit -->
    </Booster>
</Projectile>
```

With thrust alone, the projectile keeps accelerating. To hold its speed down, use `MaxSpeed` (the upper speed thrust accelerates to) or `Drag` (drag; the speed levels off where it balances thrust, and acceleration also becomes gentler).

To let the player decide the thrust burn time, use `ThrustTimeSlider`. When the fuel runs out, thrust stops and the projectile flies on inertia afterward (stabilizing fins and guidance keep working). For a weapon with no fuel limit (infinite), do not place a slider and do not write `ThrustTime`.

```xml
<ModuleMapperTypes>
    <Slider displayName="Burn time" key="burn" min="0.1" max="10" default="2" />
</ModuleMapperTypes>
...
<ThrustTimeSlider key="burn" />
```

## Guidance

When `Projectile/Guidance/Gain` is greater than 0, the projectile curves toward its target enemy machine. It only changes the orientation, so use it in combination with propulsion.
How the target is decided, how it flies, and how guidance is released are explained in detail in [Guided missiles](guidance.md).

```xml
<Projectile>
    <Booster>
        <Thrust>1000</Thrust>
        <Stability>0.5</Stability>
    </Booster>
    <Guidance>
        <Gain>0.25</Gain>             <!-- Guidance strength -->
        <ReleaseAngle>90</ReleaseAngle> <!-- Stop guiding when the angle between the nose and the target direction exceeds this [deg]. If not written, never stops -->
    </Guidance>
</Projectile>
```

- **XML alone does not decide the target.** Specify `Weapon.Target` in C# (how to aim is up to your mod; see [Using C#](csharp.md#specifying-the-guidance-target)). A projectile with no target flies unguided.
- The target is fixed at the moment the shot is accepted, and the target of a projectile in flight does not change. When the target is destroyed, respawns, or guidance is released, the projectile becomes unguided and does not chase another enemy.
- `ReleaseAngle` keeps a projectile that missed and passed the target from turning back. With 90, it keeps tracking as long as the target is in the forward hemisphere of the nose. It is checked from right after firing, so firing in a direction far away from the target makes the projectile unguided at once.
- An unguided projectile keeps its heading and flies straight (a projectile fired without a target keeps the firing direction; one that lost its target keeps the heading at that moment). Propulsion, stabilizing fins, drag, gravity, and lifetime keep working.

> [!NOTE]
> Until 1.0, guidance behavior and coefficients may change (changes are recorded in the CHANGELOG).

## Weapons with many projectiles

The number of projectiles that can be in flight at once is limited to `Projectile/PoolSize` (default 50). When the limit is reached, the oldest projectile is removed and the new one fires.
Increase it for weapons with a fast rate of fire or projectiles with a long lifetime (that many projectiles are created at simulation start).
