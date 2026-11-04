# Project Status

Last updated: 2026-11-04. Working title: TBD. Unity project on **6000.6.7f1** (Unity 6.6), URP, Windows.

## Summary

The code skeleton for Phases 0 to 4 and the hospital design is written. It compiles in Unity, and all 126 EditMode tests pass inside the Unity Test Runner. The player moves and looks in a hand-built Sandbox scene. Grab, throw, delivery, insanity and enemies have not been played yet.

## Design review (2026-11-03)

`docs/GAME_DESIGN.md` was revised to version 2 after two outside reviews and a check against the code. Main fixes: committed loot, quota-first budgeting by player count, slower pressure, a limited radio, fairness rules, a quota-unlocked exit. The economy changes (committed loot, exit latch, dose gate, 0.25 baseline, radio battery, quota-first budgeting, apple noise, exit zone) are now in code with tests. The doctor hit model, safe rooms, Threat Director and hallucination rules are not yet. See section 8 of the design.

A second review (applied 2026-11-04, design version 2.1) changed the design further: vault weighting cut from 40 to 20 to 25 percent with a guaranteed-value rule for the nearest rooms, a radio that only plays when set down, a first delivery target of 3:00, a specified doctor hit model, hazard classes, and clearer exit and death rules. These are **docs only**. The matching code (items 12 to 15 in section 8 of the design) is paused until Layer 1 runs in Unity.

## Timeline

| Date | What happened |
|---|---|
| 2026-10-29 | Chose engine (Unity 6, C#), platform (Windows, Steam later), scope (solo first, multiplayer later), and a 3D physics loot game |
| 2026-10-29 | Wrote the roadmap, architecture and folder layout; created the repo skeleton and pushed it |
| 2026-10-30 | Wrote the core game code (events, services, player, grab, valuables, enemies, level generator, economy, save, dev tools) with the first 77 tests |
| 2026-10-31 | Chose the theme: a haunted mental hospital. Wrote `docs/GAME_DESIGN.md` |
| 2026-10-31 | Implemented the design in code: insanity and dosing, medicine economy, radio, apples, flee state, ground doctor, spider nest, jumpscares, spider overlay. 126 tests |
| 2026-11-03 | First time in the Unity editor. Fixed two compile errors. All 126 tests pass in Unity. Layers created, Sandbox scene started, player walking |

## Code written

All under `Assets/_Project/Scripts/`. See `docs/CODE_STATUS.md` for the per-assembly list.

- Core: event bus, services, seeded random numbers, state machines, noise, speed modifiers, difficulty profile, compounder, jumpscare scheduler
- Player: movement, look, input, stamina, health, insanity
- Interaction: physics grab, carry, throw
- Items: valuables with impact damage, extraction zone (the medicine counter), apples with rot, radio
- Enemies: lethal doctor with senses, fleeing and a state machine; ground doctor; spider nest; scare triggers
- LevelGen: seeded layout planner, spawn budgeting, level builder with nuisance spawns
- Economy: value ledger, medicine ledger, run tracker, upgrades, profile
- Save: atomic write with backup and corrupt-file fallback
- UI: spider crawl overlay logic
- DevTools: command registry, debug overlay, sandbox run starter

## Testing

| Check | Result |
|---|---|
| 159 EditMode tests, run outside Unity (Mono, stand-ins for Unity types), after the design v2 code changes | All pass |
| 126 EditMode tests, Unity Test Runner (6000.6.7f1), 2026-11-03 | **All pass.** The 33 newer tests have not been run in Unity yet |
| Console warnings during the run | 2, both expected: the save tests deliberately write a corrupt file |
| MonoBehaviour code at runtime | Only player walking and looking confirmed so far |

Tests cover: event bus, seeded random numbers, state machines, noise, stamina and health, grab maths, value loss, run and medicine ledgers, upgrades, enemy senses and flee maths, layout planning (200 seeds), spawn budgeting, the command parser, save round trip, insanity and swell, compounder, speed modifiers, difficulty, apple rot, per-source hearing, the ground doctor state machine, the spider swarm cap and the jumpscare scheduler.

## Problems found and fixed

| Problem | Cause | Fix |
|---|---|---|
| `EntityId` is an ambiguous reference | Unity 6.6 added `UnityEngine.EntityId`, clashing with ours | Renamed ours to `GameEntityId` (commit 175532e) |
| `Game.UI` does not exist (test file) | The test assembly did not reference `Game.UI` | Added the reference (commit cd95a82) |
| Pull showed no layers in the matrix | Layers had not been created yet, and the name field was off screen in a narrow window | Widened the Project Settings window and named the layers |

## Editor setup done

- Project opened on 6000.6.7f1. The installed 6.6 was used instead of downloading 6.3 LTS, which means the project is **not on an LTS version**. See risks.
- Unity created its own URP global settings and default volume profile in `Assets/`.
- Layers created: `Player`, `Grabbable`, `Held`, `Enemy`, `Level`, `Trigger`, `NoClip`.
- Unity meta files committed.
- Sandbox scene: floor, player with all components wired, walking and looking works.

## Roadmap progress

| Phase | Code | Proven in Unity |
|---|---|---|
| 0 Foundations | Done | Nearly: opens clean, tests pass |
| 1 Player and Grab | Done | In progress |
| 2 Valuables and Damage | Done | No |
| 3 Extraction Loop | Mostly (no menu or results UI) | No |
| 4 Enemy and Noise | Mostly | No |
| 4.5 Networking spike | No | No |
| 5 Procedural levels | Generator logic only | No |
| 6 Meta progression | Logic only | No |
| 7 to 9 | No | No |

Rough read: about 5 to 10 percent of the whole plan by exit criteria, about 30 to 35 percent by code written.

## Not done yet

- Confirm the Held/Player box is unticked in the Layer Collision Matrix
- Vase, grab and throw test; medicine counter; run starter; the six-point playtest in `docs/UNITY_SETUP.md`
- Prefabs and scenes for the radio, apples, enemies, nuisances and rooms
- HUD, menus, results screen, insanity meter
- Audio and animation
- Defibrillator, downed state and friendly fire (planned for Phase 8)
- Networking spike and multiplayer

## Known risks

- **Not on an LTS Unity version.** 6.6 is newer than the LTS releases. APIs may shift between releases, as the `EntityId` clash showed. Consider moving to 6.3 LTS before real content exists.
- The Unity-side scripts (physics, grab, NavMesh, UI) have never been run. Expect bugs in Phase 1 and 2 testing.
- The Phase 1 exit criterion (carry, swing, drop and throw for five minutes with no jitter) has not been tested.

## Next steps

1. Finish the vase grab and throw test.
2. Add the medicine counter and the run starter, then do the six-point playtest.
3. Tag `phase-0-done`. Fix whatever the playtest finds before adding enemies.
4. Add the radio and apples, then enemies and nuisances, one at a time.
5. Greybox rooms, then swap in free art packs.
