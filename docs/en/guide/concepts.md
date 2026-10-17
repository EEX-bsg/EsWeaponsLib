# Core Concepts (for Modders)

This is what you should know before using EWL, whether you use XML or C#.

## Four levels of use

| Level | What you do | Page |
|---|---|---|
| A. XML only | Write `EwlWeapon` and the like in the block XML | [Building Your First Weapon](xml-first-weapon.md), [Using XML](xml-recipes.md) |
| B. Reference | Read a weapon's remaining ammo and events from C# (for a HUD, etc.) | [Using C#](csharp.md#observing-weapons) |
| C. Custom module | Create and drive a `Weapon` from your own C# module | [Using C#](csharp.md#building-a-weapon-in-your-own-module) |
| D. Custom features | Add behavior EWL does not have, using lower-level parts and events | To be published as needed |

EWL's own `EwlWeapon` is written using only the same public API as level C. Anything you can do in XML you can also do in C#.

## Depending on EWL

- A mod that uses EWL writes `<LoadOrder>1</LoadOrder>` (1 or higher) in its `Mod.xml`. EWL uses the default of 0, so this makes your mod load after EWL.
- Do not add `<LoadInTitleScreen />` to a mod that uses EWL. It would be loaded before EWL.
- In block XML, add `modid="0d219960-b202-4906-b29d-8d90a61a89b8"` (EWL's Mod ID) to EWL's modules.
- When using EWL from C#, take into account environments where EWL is not installed, and check with `Mods.IsModLoaded` before using it ([Using C#](csharp.md)).
- Users need to subscribe to EWL. It is considerate to specify EWL as a required item on your mod's Workshop page.

## Weapon definition

Currently, an EWL weapon is defined by a launcher and a projectile.

|  |  |
|---|---|
| **Launcher** (`EwlWeapon` / `WeaponSpec`) | Values of the launcher itself: muzzle position, muzzle velocity, interval, ammo count, magazine, firing effects, and so on. |
| **Projectile** (`Projectile` / `ProjectileSpec`) | The projectile's physics, colliders, appearance, damage, and fuse, plus `Explosion`, `Booster` (propulsion), and `Guidance`. |

- XML element names and C# field names are the same
  - (directly under `EwlWeapon` = `WeaponSpec`, `EwlWeapon/Projectile` = `ProjectileSpec`).
- A feature you do not use is disabled if you do not write it.
  - An explosion does not work when `Radius` is 0, propulsion when `Thrust` is 0, and guidance when `Gain` is 0.

## Execution authority

In Besiege multiplayer there are three states: host, client, and local simulation.
EWL splits its processing by whether "this peer is responsible for the physics of that simulation" (execution authority).

| Situation | Firing, hits, damage | Visuals |
|---|---|---|
| SP | You | You |
| MP | Host | Clients spawn visual-only projectiles that are synchronized |
| MP local simulation | Individual | Not visible to other players |

- Requests such as `Weapon.TryFire()` are accepted only on the side that has execution authority. Elsewhere they do nothing and return `false`.
- Hit events (`Weapon.Hit` / `Weapon.AnyHit`) also fire only on the side that has execution authority.
- You do not need to write any code for synchronization.

## Lifecycle

- A weapon is created when the simulation starts and removed when it stops.
- If the weapon that fired is destroyed or detached, projectiles already in flight keep flying to the end.
- When the simulation stops, all projectiles and effects are reclaimed.

## Reloading XML

When the `Mod.xml` of the mod that defines the block has `<Debug>true</Debug>`, saving the block XML while in the game applies
most elements from the next simulation start. References to keys, toggles, and sliders, and `EwlBlockPhysics/AdditionalColliders`, require
a Besiege restart (which ones apply is shown in the "Reload" column of the [XML Reference](../reference/xml-modules.md)).

## Common mistakes

| Symptom | Cause and fix |
|---|---|
| None of the block's functions work (including non-EWL ones) | If even one of a block's modules cannot be read, Besiege discards all of that block's modules. Check the warnings in `output_log.txt`, and verify the required element (`FireKey` of `EwlWeapon`), the spelling of element names, and the value types. |
| `EwlWeapon` is not recognized | `modid` is missing or wrong. Is `LoadOrder` in `Mod.xml` 1 or higher? Is EWL enabled? |
| The projectile hits your own machine and explodes | Move the muzzle position outside the block, or lengthen `Projectile/ColliderEnableDelay` (especially for slow projectiles). |
| It fires but the projectile is not visible | When `Projectile/Mesh` is absent, the projectile is a small sphere. For mesh names, use the names written in `Resources` of the `Mod.xml` of the mod that defines the block. |
| Effects do not appear | Check the `AssetBundle` setting and the spelling of the prefab name. Also, effects are applied after the bundle finishes loading, so they may not be visible right after deployment. |

## Versions and compatibility

Until the official release, EWL may change its public surface (element names and C# API) with advance notice. Changes and migration examples are in [CHANGELOG.md](https://github.com/EEX-bsg/EsWeaponsLib/blob/main/CHANGELOG.md).
After the official release, breaking changes to existing block XML and external DLLs will be made carefully, and in principle only in major updates.
