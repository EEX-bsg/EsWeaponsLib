# Verification and Testing

EWL has no automated tests. Check mechanically what can be checked outside the game, and check the rest inside the game.

## Outside the game (on every change)

| Check | Method | Passing condition |
|---|---|---|
| Build | The build command in [CONTRIBUTING.md](https://github.com/EEX-bsg/EsWeaponsLib/blob/main/CONTRIBUTING.md) | No errors |
| Forbidden APIs | `powershell -File tools/ilscan.ps1` | `IL scan OK` |
| Public surface | `powershell -File tools/api-surface.ps1`, then `git diff api-surface.txt` | Only the intended differences. Breaking changes are recorded in the CHANGELOG |
| Reference | `powershell -File tools/gen-reference.ps1`, then `git diff docs/reference` | Only the intended differences |
| Information that must not be public | `powershell -File tools/check-public.ps1` | `check-public OK` |
| Documentation site (when you changed docs/) | `npm run docs:build` | Succeeds (fails if there is a broken link) |
| Conventions | Read [conventions.md](conventions.md) against the diff | No violations |

## In the game (SP)

For changes that affect behavior, check the following with a test block.

- No `[EWL]` errors or exceptions appear in `output_log.txt`. No XML loading warnings appear.
- Firing: Projectiles come out and the block moves from recoil (judging by the recoil and the effect on the target is more reliable than by the number of projectiles).
- The behavior of the changed feature (magazines, reloading, explosions, propulsion, etc.).
- It works the same way when you stop and restart the simulation. No projectiles or effects remain when it is stopped.

## In the game (MP)

For changes that affect synchronization, authority, or input, check with two players, a host and a client.

- Whether the host or the client fires, projectiles, explosions, and sounds appear on both screens. Damage is applied only once, on the host.
- On the client's screen, the projectile trajectory roughly matches the host's.
- The state of remaining ammo and reloading is reflected on the client as well.
- Projectiles from a local simulation are not visible to other players.

If you cannot verify MP, say so in the PR. The maintainer will verify it.
