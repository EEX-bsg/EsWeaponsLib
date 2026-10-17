# E'sWeaponsLib Documentation

E'sWeaponsLib (EWL) is a prerequisite library for Besiege weapon mods. With XML custom modules and a C# API, it provides
firing, projectiles, impacts, explosions, magazines, block physics, and multiplayer synchronization.

## Players

- [Getting Started](getting-started.md) — Installation, multiplayer, and troubleshooting

## Mod developers

- [Core Concepts](guide/concepts.md) — The four levels of use, the shape of a definition, execution authority, lifecycle, and common mistakes
- [Building Your First Weapon (XML)](guide/xml-first-weapon.md) — Make a block that fires using XML alone
- [Using XML](guide/xml-recipes.md) — Magazines, bursts, sliders, projectiles, fuses and explosions, effects and sounds, rockets
- [Block Physics (EwlBlockPhysics)](guide/xml-block-physics.md) — Joint strength, physics material, rotation axes, additional colliders
- [Using C#](guide/csharp.md) — Observing weapons, hits and attackers, building weapons in your own module
- [XML Reference](reference/xml-modules.md) — All elements and default values (generated)
- [C# API Reference](reference/csharp-api.md) — Public types and members (generated)
- [Changelog](https://github.com/EEX-bsg/EsWeaponsLib/blob/main/CHANGELOG.md)

## Contributors

- [CONTRIBUTING.md](https://github.com/EEX-bsg/EsWeaponsLib/blob/main/CONTRIBUTING.md) — Environment, build, and pull requests
- [Design Principles](contributing/design-principles.md) — Purpose, principles, scope of responsibility, and usage patterns
- [Architecture](contributing/architecture.md) — Folders, dependency direction, weapon flow, execution authority and multiplayer
- [Public Surface and Compatibility](contributing/compatibility.md) — What must not be broken, and the procedure for changing the public surface
- [Coding Conventions](contributing/conventions.md)
- [Verification and Testing](contributing/testing.md)
- [Writing Documentation](contributing/documentation.md)
