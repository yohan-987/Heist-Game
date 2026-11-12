# Known Issues

Log bugs here instead of carrying them in your head.

All entries below come from a static code review on 2026-11-13 (two outside reviews, each claim re-checked against the code at commit 3b0a1f3). **None has been reproduced in Unity yet.** "Fixed on branch" means the change is written on `fix/review-p0` but not compiled or tested in Unity.

| Date | Area | Issue | Status |
|---|---|---|---|
| 2026-11-13 | Sandbox scene | `Head` was saved at local position `(0, 1.6, -10)`, putting the camera, grab origin and enemy sight target 10 m behind the player. `PlayerMotor.ApplyHeight` only rewrote Y | Fixed on branch, not verified in Unity |
| 2026-11-13 | RunTracker | `OnPlayerDied` called `Finish()`, which reported `Success = ExitUnlocked`. A team wipe after the exit unlocked counted as a win (GAME_DESIGN 4.10 says all dead is failure) | Fixed on branch (`Finish(bool extracted)`), not verified in Unity |
| 2026-11-13 | ThreatDirector | `Services.Threats.Tick` ran only inside `JumpscareDirector.Update`. Without a director, timed hazards never expired and filled `MaxActiveHazards` | Fixed on branch (ticked from `RunTracker.Update`), not verified in Unity |
| 2026-11-13 | SandboxRun / Apple | `Apple.Awake` read `Services.Difficulty` before `SandboxRun.Start` set it, so scene-placed apples ignored High difficulty rot | Fixed on branch (difficulty set in `Awake`, execution order -100), not verified in Unity |
| 2026-11-13 | Packages | `com.unity.ai.navigation` is not in `Packages/manifest.json`. `NavMeshSurface` is needed to bake a NavMesh for doctors. Install through the Package Manager so Unity picks the version that matches 6.6 | Open |
| 2026-11-13 | Apple | Rot timer starts at level spawn (High difficulty only), so every apple is rotten after about 76 s. Proposed: start the timer on first grab. This is a design decision | Open |
| 2026-11-13 | Apple | An apple caught in mid-air stays armed and splats if set down within the armed window | Open |
| 2026-11-13 | PlayerGrabber | The hold target is not clamped against walls. A held item is driven into geometry and may auto-release | Open |
| 2026-11-13 | PlayerGrabber | Hold gain and max speed ignore mass, so heavy items swing as fast as light ones | Open, tune after playtest |
| 2026-11-13 | PlayerMotor | Carry stamina drain applies while standing still and blocks recovery | Open |
| 2026-11-13 | GroundDoctor | Struggle uses the Jump input while `PlayerMotor` still jumps (8 stamina each). No struggle noise and no hold timeout (GAME_DESIGN 4.6) | Open |
| 2026-11-13 | GroundDoctor | `AttachVictim` parents an `Enemy`-layer collider into the player capsule. `OnDisable` does not end the hazard in `Services.Threats` | Open, check in Play Mode |
| 2026-11-13 | InsanityModel | `PlayerInsanity.Update` calls `Tick` then `Relieve`. At 100 pressure any radio relief resets `SwellTimer` every frame, so a weak radio signal stops the burst | Open |
| 2026-11-13 | ThreatDirector | `DoctorNear` counts every doctor within 15 m in any state and ignores walls | Open, rule is intentional, scope needs review |
| 2026-11-13 | JumpscareScheduler | Scripted scares skip the safe-room and swell checks, and share one cooldown with hallucinations (GAME_DESIGN 4.8) | Open |
| 2026-11-13 | EnemyBrain | The attack windup stops the doctor for 0.75 s, then 1.4 s of cooldown. A player walking away at 3.5 m/s should dodge every hit | Open, tune in Play Mode |
| 2026-11-13 | DoseGate / EconomyPlanner | Gate allows a dose at 20 pressure, but the survival budget assumes none is wasted | Open, design decision |
| 2026-11-13 | GameClock | `Now` returns raw `Time.time`, and several scripts read `Time` directly, so `SetPaused` does not pause them | Open |
| 2026-11-13 | PlayerInputReader | Cursor stays locked after run end or death. Gamepad has no scroll binding for hold distance and rotation | Open |
