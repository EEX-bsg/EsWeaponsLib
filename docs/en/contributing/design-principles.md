# Design Principles

Please read this before making changes to EWL. It describes "what to protect" and "why we do it this way".
For the code structure, see [architecture.md](architecture.md); for how compatibility is handled, see [compatibility.md](compatibility.md).

## What EWL is

E'sWeaponsLib (EWL) is **a general-purpose weapon library for Besiege, used by external mod developers (people and their AI agents)**.
With XML custom modules and a C# API, it provides firing, projectiles, impacts, explosions, magazines, block physics, and multiplayer synchronization.

## Unchanging goals

1. External mods use EWL **through the public API** and do not need to concern themselves with its internals (projectile management, synchronization, the interface to the Besiege base game).
2. External mods can use EWL's **XML custom modules as they are**.
3. External mods can **add their own features through the API and build their own custom modules**.

These three do not change. The principles, structure, and API shapes below are means to satisfy them. They may be changed as long as the goals are satisfied.

## Principles

| Principle | Content and reason |
|---|---|
| The public API is the front door, and the modules are its users too | EWL's own XML modules are also written using only the public API (`EWL.Api`). By using it ourselves, the gap of "a module can do it but an external mod cannot" is closed naturally. In exchange, each feature requires writing three places: "API data, XML element, conversion" |
| New features go internals → API → module | Do not build a feature that exists only in an XML module |
| Decide the scope of implementation by real need, and present it in general vocabulary | What to build is decided by the needs of real consumers (for now, the E-cannon mod). Names, the XML schema, and the shape of the API are designed in the language of general weapons, not skewed toward a particular mod |
| Extend only through composition and events | The consumer holds and drives EWL's parts (`Weapon`, etc.) and reacts to events. We do not create base classes for inheritance, chains of hooks, registries, or DI. Keeping the public surface to "data + parts + events" makes it harder for AI to misuse and lighter to maintain |
| Few concepts | Types are split only for definitions that are passed or shared independently (weapon, projectile, explosion) and for optional features attached to a projectile (propulsion, guidance). We do not make a box for each topic. Optional features are expressed with a default of "disabled when 0", not with null |
| MP is the baseline behavior | Weapons built with EWL's modules or `Weapon` are synchronized in MP without the user doing anything |
| Compatibility: "loose, but never break without notice" | The shape of the API is not fixed until 1.0. Even from 1.0 we do not freeze strictly, and when breaking we bump the version and write the migration steps |
| Base-game dependencies in traceable places | We do not create an abstraction layer. Only Besiege base-game operations that have pitfalls, or that are used in several places, are gathered into `Besiege/` |

## Scope of responsibility

| Category | Content |
|---|---|
| What EWL does | Fire control (keys, auto-fire, burst, delay, spread, recoil), projectiles (spawning, physics, colliders, pools), impacts (damage, sticking to what is hit, explosions, fuses), effects and sounds, magazines and reloading, propulsion, guidance, the block physics module, MP synchronization, the C# API, attacker tags (whose projectile it was, who detonated it), and documentation and samples for users |
| What the consumer mod does | Block XML and assets, UI such as a HUD, determining kills and deaths, and features specific to that mod |
| What it does not do | Compatibility with ACM (AddCustomModule) (element names, schema, configuration files), world extension (breaking through the 4 km boundary, origin shift), water, buoyancy, environment, skyboxes, and general-purpose infrastructure unrelated to weapons (such as a communication channel API for other mods) |

If you want to add a feature that looks outside this scope, please discuss it in an issue before implementing it.

## Usage patterns

| Pattern | What the user writes | What EWL handles |
|---|---|---|
| A. Use the XML modules as they are | Write `<EwlWeapon modid="...">` and the like in the block XML | All of input, firing, projectiles, impacts, explosions, effects, and MP synchronization |
| B. A + observation | In addition to A, read the weapon's state and events from C# (such as an ammo HUD) | Same as A |
| C. Build a custom module with the API | Create and drive a `Weapon` from your own `BlockModule` or block script. The user decides when and how to fire and where the values come from | Projectiles, impacts, explosions, effects, magazines, MP synchronization |
| D. Add custom features | In addition to C, write behavior EWL lacks using lower-level parts and events | The inside of each part |

The pattern A modules are written using only the same public API as pattern C. In other words, "what EWL's modules can do = what an external mod can do with the API".
The lower-level parts for pattern D (such as parts that handle projectiles directly) will be published step by step when they are actually needed.

## When in doubt

Think in this order.

1. Does it harm any of the three goals?
2. Does it break existing block XML or external DLLs ([compatibility.md](compatibility.md))?
3. Does a real consumer need it (do not build for imagined demand)?
4. Can it be written without adding concepts?
