# Decision Log

| Date | Decision | Status | Notes |
|---|---|---|---|
| 2026-10-29 | Engine = Unity 6 LTS, C# | Decided | |
| 2026-10-29 | Platform = Windows PC, Steam later | Decided | |
| 2026-10-29 | Scope = solo-first, multiplayer later | Decided | |
| 2026-10-29 | Game = 3D physics loot game (not 2D Metroidvania) | Decided | |
| | Render pipeline = URP | Proposed | Confirm in Phase 0 |
| | Networking stack | Open | Decide after Phase 4.5 spike |
| | Voice chat solution | Open | Follows networking choice |
| 2026-10-31 | Theme = haunted mental hospital; loot goes to a medicine counter and compounder | Decided | See GAME_DESIGN.md |
| | Title | Open | Decide before Phase 2 art pass |
| | Delivered medicine = team quota and shared dose pool | Proposed | Confirm in playtests |
| | Art style = stylised low-poly | Proposed | |
| 2026-11-03 | Editor version = Unity 6.6 (6000.6.7f1) for now, not an LTS | Decided | Chosen to avoid a large download. Revisit before real content exists |
| 2026-11-03 | Renamed `EntityId` to `GameEntityId` | Decided | Unity 6.6 added `UnityEngine.EntityId` |
| 2026-11-03 | Design v2: loot is consumed and its value committed on delivery | Decided | Fixes the retroactive-value hole. See GAME_DESIGN.md 4.2 |
| 2026-11-03 | Quota set in doses first; loot budget derived from quota, player count and run length | Decided | GAME_DESIGN.md 4.3 |
| 2026-11-03 | Baseline pressure 0.25 per second, 5 second last-chance swell, dose gate | Decided | To validate in the Layer 1 playtest gate |
| 2026-11-03 | Radio is battery-limited, 0.6 per second, strongest source only | Decided | Previous 3 per second bypassed the medicine economy |
| 2026-11-03 | Quota unlocks the exit and stays unlocked; counter and exit sit in the start wing | Decided | Greed versus safety decision |
| 2026-11-03 | Build in three layers with a playtest gate after Layer 1 | Decided | Scope control |
| 2026-11-04 | Nearest two thirds of rooms hold at least 1.2 times the minimum winnable value; vault capped at 20 percent (normal) and 25 percent (high), from 1 − 1.2 × E | Decided | Replaces the 40 percent weighting, which could make the quota depend on the vault wing |
| 2026-11-04 | Radio only plays when set down; radios per level = max(1, ceil(P / 2)) | Decided | Calm should cost standing still |
| 2026-11-04 | First delivery targeted by 3:00; the introductory scare adds no Pressure | Decided | With the +5 scare, Pressure 50 would arrive at 3:00 |
| 2026-11-04 | Doctor hit: 60 damage, 2 s protection, wind-up of at least 0.75 s, regeneration only in safe rooms (5 per second) | Proposed | Tune in the Layer 1 gate |
| 2026-11-04 | Hazard classes with cosmetic effects uncounted; failing must never pay more than succeeding | Decided | GAME_DESIGN.md 4.9 |
| 2026-11-04 | Doses tracked per player; the dose gate limits waste but does not stop griefing | Decided | GAME_DESIGN.md 4.1 |
| 2026-11-04 | Freeze new design and code until Layer 1 runs in Unity | Decided, then overridden | Nine verified-in-theory systems are waiting on the editor |
| 2026-11-05 | Wrote the design 2.1 code anyway, at the owner's request. No further new features until Layer 1 runs in Unity | Decided | Review items 9 to 16 are in code; the hallucination rules (Phase 7) stay unwritten |
| | UI name for insanity = Pressure | Proposed | Code name stays `Insanity` |
