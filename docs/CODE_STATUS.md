# Code Status

Last updated: 2026-10-31. Written without the Unity editor, so everything below says what has and has not been verified.

## What exists (`Assets/_Project/Scripts/`)

| Assembly | Contents |
|---|---|
| `Core` | `EventBus`, game events, `EntityId`, `DeterministicRandom`, `StateMachine`, `GameStateMachine`, `GameClock`, `PlayerRegistry`, `Services`, `Spawner`, `Bootstrap`, noise maths and `NoiseEmitter`, layer names, input state struct |
| `Player` | `StaminaModel`, `HealthModel`, `PlayerStats`, `PlayerInputReader` (Input System), `PlayerLook`, `PlayerMotor` |
| `Interaction` | `GrabMath`, `Grabbable`, `PlayerGrabber` |
| `Items` | `ValuableDefinition`, `ValueLoss`, `Valuable`, `ExtractionZone` |
| `Enemies` | `EnemyDefinition`, `EnemySenses`, `EnemyBrain` (NavMesh + state machine) |
| `LevelGen` | `LayoutPlanner` (seeded, deterministic), `SpawnPlanner`, `RoomAuthoring`, `LevelDefinition`, `LevelBuilder` |
| `Economy` | `RunLedger`, `QuotaMath`, `RunTracker`, `UpgradeDefinition`, `PlayerProfile`, `UpgradeShop` |
| `Save` | `SaveService` (atomic write, backup, corrupt-file fallback) |
| `DevTools` | `CommandRegistry`, `DebugOverlay` |

## Verified

77 EditMode tests in `Assets/_Project/Tests/EditMode/` were compiled and run outside Unity against small stand-ins for the Unity types they touch. All pass. They cover the event bus, seeded RNG, state machines, noise maths, stamina and health, grab maths, value loss, run ledger and quotas, upgrade shop, enemy senses, layout planning (200 seeds: connected, no overlaps, deterministic, exit room is farthest), spawn budgeting, the command parser and the save round trip.

## Not verified

These need the Unity editor and have never been compiled or run:

- Anything that is a `MonoBehaviour` or uses physics, the Input System, or the NavMesh: `PlayerMotor`, `PlayerLook`, `PlayerInputReader`, `PlayerGrabber`, `Grabbable`, `Valuable`, `ExtractionZone`, `EnemyBrain`, `LevelBuilder`, `RunTracker`, `NoiseEmitter`, `Bootstrap`, `DebugOverlay`.
- The test suite inside the Unity Test Runner (it should behave the same, but this is the first thing to run).
- Assembly definition references: if Unity reports a missing reference, add the assembly to that `.asmdef`.
- API names target **Unity 6**: `Rigidbody.linearVelocity`, `linearDamping`, `angularDamping`, `FindObjectsByType`.

## Setup still needed in the editor

1. Copy `ProjectSettings/` and `Packages/` from a fresh 3D (URP) project (see README).
2. Create the layers with these exact names: `Player`, `Grabbable`, `Held`, `Enemy`, `Level`, `Trigger`, `NoClip`. Then set the collision matrix: `Held` must not collide with `Player`.
3. Install packages: Input System, AI Navigation, ProBuilder, Test Framework.
4. Build the Sandbox scene: player prefab (CharacterController, `PlayerStats`, `PlayerInputReader`, `PlayerLook`, `PlayerMotor`, `PlayerGrabber`, `NoiseEmitter`, camera on a child "Head" assigned in `PlayerLook`), a floor on the `Level` layer, a few Rigidbody cubes with `Grabbable` on the `Grabbable` layer, one `ValuableDefinition` asset and a `Valuable` prefab, an `ExtractionZone`.
5. Run the EditMode tests, fix whatever the compiler reports, then work through Phase 1 exit criteria in the roadmap.

## Known gaps

- No UI yet (HUD, menus, results screen).
- `LevelBuilder` expects room prefabs with `RoomAuthoring` set up by hand, and an optional `INavMeshBaker` component to bake the NavMesh. That wrapper is not written.
- No audio, no networking (`Net/` is intentionally empty until Phase 8).
- `EnemyBrain` has no animation hooks and picks wander points with `UnityEngine.Random`, which is not deterministic. That is fine offline and must move to the host in multiplayer.
