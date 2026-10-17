# Public Surface and Compatibility

When EWL is updated, the external mods on users' machines are swept up in that update. The Workshop updates automatically,
so an external mod can break even if its author has done nothing. This page sets out what counts as the "public surface" and how it may be changed.

## Public surface

- The names and elements of XML custom modules (`EwlWeapon`, `EwlBlockPhysics`, and the element names, types, default values, and meanings within them)
- Public C# types and members (the public ones in `EWL.Api` and `EWL.Modules`)
- Behavior that is described in the documentation

The list is `api-surface.txt` at the repository root, generated from the built DLL with `tools/api-surface.ps1`.

## Why things break

- XML: If even one of a block's modules cannot be loaded (a required element is missing, a type does not match, or there is an unknown module),
  that block **loses all of its modules**. Unknown elements are ignored.
- C#: If you change a public method's signature, compiled external DLLs fail at runtime with `MissingMethodException` or the like.

## Rules

| Surface | From 1.0 |
|---|---|
| XML schema | **Additions only**. New elements must always have a default and be optional. When renaming, keep the old name as an alias. Deletion is done by turning the element into one that is "read but ignored" |
| Public C# API | Breaking changes only in major versions. If possible, announce them with `[Obsolete]` from the previous minor version |
| Internals | May be changed freely (they are `internal`, so invisible from outside) |
| Assembly version | Fix `AssemblyVersion` at `1.0.0.0`. The library's version is expressed by `Version` in `Mod.xml` |

Before 1.0 (alpha and beta), breaking changes are allowed, but always write an "old → new" example in the CHANGELOG.
While unreleased, if you fix the consumer at the same time, an alias for the old name is not needed either.

There is one question to ask when judging: **"Does this change break existing block XML or compiled external DLLs?"**

## Procedure for changing the public surface

1. Build, run `tools/api-surface.ps1`, and check the diff of `api-surface.txt`.
2. Judge with the question above.
   - Not breaking: Add one line under `[Unreleased]` in `CHANGELOG.md`.
   - Breaking: Write `[Breaking]` and add a concrete "old → new" example (both XML and C#, as applicable). From 1.0, bump the major version and get the maintainer's confirmation.
3. Run `tools/gen-reference.ps1` to regenerate `docs/reference/`.
4. Commit `api-surface.txt` and `docs/reference/` together with the change.

## When adding a new element or member

- Confirm that a real consumer needs it.
- Add it in the order internals → `Api/` → (if needed) `Modules/`.
- Write a description with `///` on types and members in `Api/` (units, the meaning of the default, "disabled when 0", and so on). The reference is generated from this.
- Give an XML element the same name as the corresponding C# field.
- Express an optional feature with a default of "disabled when 0", not with null.
