# Guided missiles

How EWL's guided projectiles decide their target and how they fly.

> [!WARNING]
> Guidance is under active development. Targeting, flight behavior, elements and coefficients may change significantly.
> Before building a mod that relies on guidance, check the current state with the author. Changes are recorded in the [CHANGELOG](https://github.com/EEX-bsg/EsWeaponsLib/blob/main/CHANGELOG.md).

## Usage

Guidance only turns the projectile's orientation (its nose). The force that pushes it forward comes from propulsion (`Booster`), so use them together.

```xml
<Projectile>
    <Drag>0.9</Drag>                    <!-- Drag. Speed levels off where it balances thrust -->
    <AngularDrag>15</AngularDrag>       <!-- Rotational drag. Suppresses oscillation when turning -->
    <IgnoreGravity>true</IgnoreGravity>
    <Booster>
        <Thrust>1000</Thrust>           <!-- Thrust [N] -->
        <Stability>0.5</Stability>      <!-- Stabilizing fins -->
        <MaxSpeed>220</MaxSpeed>        <!-- Top speed [m/s] -->
    </Booster>
    <Guidance>
        <Gain>0.25</Gain>               <!-- Guidance strength -->
        <ReleaseAngle>90</ReleaseAngle> <!-- Stop guiding when the angle between the nose and the target direction exceeds this [deg] -->
    </Guidance>
</Projectile>
```

**XML alone does not decide the target.** Put a target into `Weapon.Target` from C#. How to aim (the center of the camera view, etc.) is up to your mod.

```csharp
// Every frame on the shooter's peer
var view = myCamera.transform;
_weapon.Target = _weapon.FindTarget(view.position, view.forward, 6f);
```

Steps and cautions are in [Using EWL from C#](csharp.md#specifying-the-guidance-target).

## Target

| Item | Details |
|---|---|
| What can be a target | The core block of an enemy machine. Enemy = a machine of a player other than yourself who is not spectating and is in the global simulation. If the firing block has a team, the same team is excluded (with no team, everyone is an enemy) |
| How to find one | `Weapon.FindTarget` returns, among the enemies within the given angle of the aim direction, the one closest to the center of the aim (smallest angle). It does not choose by distance |
| Fixing | Fixed by `Weapon.Target` at the moment `TryFire` accepts the shot. Projectiles that come out later due to fire delay or burst chase the same target |
| After fixing | The target of a projectile in flight does not change. Changing `Target` afterward does not affect projectiles waiting to fire or in flight |
| Invalid target | If it is not a valid enemy core at the moment of fixing (ally, destroyed, respawned, an arbitrary object, etc.), the projectile is unguided |
| Multiplayer | Set it on the peer of the owner of the firing block. If the owner is a client, EWL sends it to the host. The host does not accept specifications from anyone other than the owner, or ones that are not enemy cores |
| SP and local simulation | There are no other players, so no target is found (use the test targets below for checking) |

## How it flies

| Phase | Behavior |
|---|---|
| First 0.2 s after firing | Keeps the firing direction (with or without a target) |
| Tracking | Turns the nose toward the intercept point (below) |
| Unguided | Keeps its heading and flies straight. A projectile fired without a target keeps the firing direction; one that lost its target keeps the heading at that moment. It never turns back to the firing direction |

In every phase, propulsion, stabilizing fins, drag, gravity, lifetime, and the time fuse work as configured.

### Steering while tracking

1. **Predict the intercept point.** It aims at where the target will have moved by the time the projectile arrives. The time to arrive is estimated as "distance ÷ average speed over that time" (the current speed plus what thrust adds, limited by `MaxSpeed`).
   The target is moved by the estimated time, the distance is measured again to that point, and this repeats `PredictionIterations` times (3 by default). More iterations are more accurate for moving targets. With 0, it aims at the target's current position.
2. **Add the gravity drop.** A projectile affected by gravity aims higher by the amount it will fall before arriving.
3. **Lead with the nose.** The projectile's velocity turns later than its nose. Pointing the nose at the intercept point alone makes it pass beside or over the target before the velocity catches up.
   So the nose is pointed beyond the direction to the intercept point by the difference between it and the velocity direction. Thrust then pulls the velocity toward the intercept point.
4. **Turn with torque.** The torque is proportional to the difference between the nose and the desired direction, with a limit proportional to `Gain`. How easily it actually turns depends on `Gain`, the projectile's mass and colliders (how hard it is to rotate), `AngularDrag`, the stabilizing fins `Stability`, and the thrust.
5. **Jitter.** While the target is more than about 70 m away, a short random wobble is added now and then.

A guided projectile with a small `Gain` cannot make large turns at short range. The farther away it is fired from, the more time it has to correct.

## Release

| Trigger | Details |
|---|---|
| Angle | When the angle between the nose and the direction from the projectile to the target exceeds `ReleaseAngle`. It is the half-angle of a cone around the nose; with 90 it tracks as long as the target is in the forward hemisphere. Checked from right after firing, so firing far away from the target makes the projectile unguided at once. 0 means no release by angle |
| Lost | The target was destroyed, respawned, or the simulation ended |

A released projectile does not chase another target. To keep a projectile that missed and passed its target from turning back, set `ReleaseAngle` to around 90.

## Speed

With thrust alone, the projectile keeps accelerating. Its speed is decided by the following combination.

| Element | Effect |
|---|---|
| `Booster/Thrust` | Pushing force [N]. Acceleration = thrust ÷ projectile mass |
| `Projectile/Drag` | Drag. Slows in proportion to speed, so speed levels off where it balances thrust. Acceleration also becomes gentler |
| `Booster/MaxSpeed` | Top speed [m/s]. No thrust while at or above this speed (gravity or blasts can exceed it) |
| `Booster/ThrustTime` | How long thrust is produced [s]. After fuel runs out, it flies on inertia |

Example: with thrust 1000 N, mass 5 kg, and `Drag` 0.9, speed levels off at about 220 m/s (Besiege's physics step is 0.01 s).

## Elements

The elements of `Projectile/Guidance` (in C#, `ProjectileSpec.Guidance` = `GuidanceSpec`).

| Element | Default | Meaning |
|---|---|---|
| `Gain` | `0` | Guidance strength (coefficient of the turning torque). 0 means no guidance |
| `ReleaseAngle` | `0` | Release angle [deg]. 0 means no release by angle |
| `PredictionIterations` | `3` | Number of iterations of the intercept prediction. 0 aims at the target's current position |

See the [XML reference](../reference/xml-modules.md#ewlweapon-projectile-guidance) for the exact description of every element.

## Checking it

There are no enemies in SP, so you can place test targets with a console command.

| Command | Details |
|---|---|
| `ewl_dummy add [distance]` | Places a 4 m box on the ground at the given distance (150 m by default) horizontally ahead of the camera |
| `ewl_dummy clear` | Removes all placed targets |

The targets are chosen as enemies only on the peer that placed them. They are not Besiege players or machines and are not synced in multiplayer. Projectiles hit them and explode.
