# Coding Conventions

The rules for writing EWL's code. Both people and AI agents follow this page.

## Runtime constraints

- Assume C# 6, a subset of .NET 3.5, and Unity 5.4. Do not use `Task`, `async`/`await`, or C# 7 and later syntax.
- When Besiege loads a mod's DLL, it inspects the APIs used, and it does not load the DLL if a forbidden API is present.
  Do not use `System.Reflection` or the forbidden APIs of `System.IO` / `System.Net` / `System.Xml`. Use `Modding.ModIO` for file I/O.
  After building, verify with `tools/ilscan.ps1` (it passes when it prints `IL scan OK`).
- Call Unity APIs only from the main thread.
- Do not make unnecessary memory allocations in `Update` / `FixedUpdate`. Do not log every frame.

## Style

- Indent `.cs` and `.xml` with four spaces. Do not use tabs (`.editorconfig`).
- Always pair event subscription with unsubscription, creation with destruction, and start with stop.
- Whenever you `catch` an exception, always log it. Do not swallow it. EWL's logs start with `[EWL]`.
- Distinguish SP, MP host, MP client, and MP local simulation. Decide whether to process weapons and physics by
  execution authority (whether this peer is responsible for the simulation's physics), and do not substitute `IsHost` ([architecture.md](architecture.md)).

## Structure

- Make folders and namespaces match (`Weapons/` → `EWL.Weapons`).
- Dependency direction and visibility are as in [architecture.md](architecture.md). In particular, of EWL's namespaces, `Modules/` uses only `EWL.Api`.
- Extend only through composition and events. Do not create base classes for inheritance, chains of hooks, or registries.
- Create an interface only when there are two or more implementations. Do not create DI or Null implementations.
- Gather Besiege base-game operations that have pitfalls, or that are used in several places, into helpers in `Besiege/`. Others may be used directly.
- Give new elements in the XML schema a default and make them optional ([compatibility.md](compatibility.md)).

## Naming

- Follow general C# conventions and use names whose role is clear on reading. Types and methods use PascalCase with complete English words.
- Do not abbreviate. Do not use abbreviations like `Fx`, `Sim`, `Hits`, or one-letter aliases that refer to a spec or part (`var f = spec.Fire`).
  Standard abbreviations such as loop indices and `rb` (Rigidbody) or `go` (GameObject) are fine. There is no need to make names needlessly long.
- Do not put names derived from other mods (such as ACM's `Ad*Prop`) or proper names specific to a particular consumer mod into EWL.

## Others' rights

- Do not bring in other mods' code or assets without confirming the terms of use.
- Do not include in the repository the decompiled output of the Besiege base game or anything copied from it. When writing about Besiege's behavior in comments or documents,
  write it in your own words as observable behavior.

## Comments

- Write a description with `///` on public types and members (`Api/` and `Modules/`). Include units (`[m/s]`, `[s]`, `[deg]`), the meaning of defaults, and
  conventions such as "disabled when 0". The reference (`docs/reference/`) is generated from this.
- Internal comments explain "why it is done this way". Do not write what can be understood by reading the code.
