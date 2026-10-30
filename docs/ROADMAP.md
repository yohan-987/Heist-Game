# Project Roadmap & Architecture

A solo-developed, physics-driven, co-op-ready 3D horror-comedy loot game, inspired by the *extract valuables without breaking them* genre (R.E.P.O. is the reference point for mechanics, **not** for names, art, or assets).

> Working title: `TBD` (decide in Phase 0; keep it out of code namespaces so renaming is cheap).
> Developer: solo. Pace assumption: roughly 10 hours/week alongside college. Adjust all timelines to your real pace.

---

## 1. Vision and Scope

### 1.1 Design Pillars
1. **Physics is the gameplay.** Every object is a Rigidbody the player can drag, swing, drop, and break.
2. **Value is fragile.** Items lose money when they take impacts. Carrying carefully is the core skill.
3. **Noise attracts danger.** Impacts and sprinting generate noise that enemies react to.
4. **Short, replayable runs.** Procedural levels from modular rooms, so content scales without handcrafting everything.
5. **Co-op ready from line one**, but offline-first in development.

### 1.2 Out of Scope (for v1)
- Story, cutscenes, voice acting
- More than 2-3 enemy types at first release
- Console ports, mobile, VR
- Dedicated servers (use host-client with relay or Steam)
- User mod support

### 1.3 Originality Rule
Mechanics are fair game to be inspired by. Names, character designs, art, audio, UI layouts, and level layouts from the reference game must **not** be copied. Pick your own theme (setting, enemy designs, tone) early; it also makes the game easier to market later.

---

## 2. Technology Decisions

| Area | Choice | Why | Alternatives |
|---|---|---|---|
| Engine | **Unity 6 LTS (6000.0.x)** | Strong physics, huge asset ecosystem, best docs for solo devs | Godot (weaker 3D physics/networking for this genre), Unreal (heavy) |
| Language | **C#** | Native to Unity | n/a |
| Render pipeline | **URP** (Universal RP) | Good performance on mid PCs, stylised looks, Shader Graph | Built-in (legacy), HDRP (too heavy) |
| Input | **Input System package** | Rebinding, gamepad, per-player maps | Legacy Input Manager (simple but obsolete) |
| UI | **UI Toolkit** for menus, **uGUI + TextMeshPro** for HUD | Pick one per layer; uGUI is more familiar for HUD | Pure uGUI everywhere (fine, simpler) |
| AI navigation | **AI Navigation package** (NavMeshSurface, runtime baking) | Required for procedural levels | A* Pathfinding Project (paid) |
| Level blockout | **ProBuilder** | Greybox rooms fast | Blender |
| Camera | Plain camera + custom script | First-person only needs little | Cinemachine (optional later for cutscenes/death cam) |
| Audio | **Unity AudioMixer** initially | Zero setup | FMOD (later, if audio gets complex) |
| Networking | **Decide at Phase 4.5 spike** (see 2.2) | Physics sync is the hardest problem; test before committing | see below |
| Voice chat | Matches networking pick (see 2.2) | Proximity voice needs 3D audio sources | n/a |
| Platform | **Windows PC**, Steam later | Matches the genre's audience | Linux/Mac builds are cheap add-ons later |
| Steam | **Steamworks.NET** | Lobbies, invites, achievements | Facepunch.Steamworks |
| Version control | **Git + Git LFS**, GitHub (private repo) | Unity binaries are large | Plastic SCM / Unity Version Control |
| IDE | Rider (free for non-commercial) or Visual Studio Community | Best Unity debugging | VS Code |

### 2.1 Packages to Install (Package Manager)
- Input System
- AI Navigation
- ProBuilder
- TextMeshPro (usually bundled)
- Cinemachine (optional, later)
- Test Framework (for unit tests)
- Unity Recorder (optional, for trailers/devlogs)

### 2.2 Networking Options (decision gate)

| Option | Pros | Cons |
|---|---|---|
| **Photon Fusion** (or PUN 2) + Photon Voice | Fast to start, relay included, voice bundled | Account + CCU limits, vendor lock-in, PUN 2 is maintenance-mode |
| **Mirror** + Steam transport (e.g. FizzySteamworks) | Free, open source, large community | More setup, voice needs a separate solution (e.g. Dissonance, paid) |
| **Netcode for GameObjects** (Unity official) | First-party, relay/lobby via Unity Gaming Services | Less physics-sync tooling out of the box |

**Gate:** In Phase 4.5, build a throwaway two-player prototype where both players can grab one shared Rigidbody. Pick whichever feels least painful. Do not decide on theory.

---

## 3. Repository Layout

```
ProjectRoot/                         <- git root
|-- .gitignore                       <- Unity template
|-- .gitattributes                   <- LFS rules (see 11.2)
|-- README.md                        <- short: what it is, how to open it
|-- docs/
|   |-- ROADMAP.md                   <- this file
|   |-- ARCHITECTURE.md              <- living copy of section 4-6
|   |-- DECISIONS.md                 <- decision log (see 13)
|   |-- design/                      <- notes, sketches, balance sheets
|-- Assets/
|   |-- _Project/                    <- ALL your own work lives here
|   |   |-- Art/
|   |   |   |-- Models/
|   |   |   |-- Materials/
|   |   |   |-- Textures/
|   |   |   |-- Animations/
|   |   |   |-- VFX/
|   |   |   |-- UI/                  <- sprites, icons, fonts
|   |   |-- Audio/
|   |   |   |-- Music/
|   |   |   |-- SFX/
|   |   |   |-- Mixers/
|   |   |-- Data/                    <- ScriptableObject assets
|   |   |   |-- Valuables/
|   |   |   |-- Enemies/
|   |   |   |-- Upgrades/
|   |   |   |-- Levels/
|   |   |   |-- RoomPools/
|   |   |-- Prefabs/
|   |   |   |-- Player/
|   |   |   |-- Valuables/
|   |   |   |-- Enemies/
|   |   |   |-- Rooms/
|   |   |   |-- UI/
|   |   |   |-- Systems/
|   |   |-- Scenes/
|   |   |   |-- Boot.unity           <- always first, loads services
|   |   |   |-- MainMenu.unity
|   |   |   |-- Lobby.unity
|   |   |   |-- Level.unity          <- generated content spawns here
|   |   |   |-- Sandbox.unity        <- dev playground, never shipped
|   |   |-- Scripts/
|   |   |   |-- Core/                <- bootstrap, state machine, events, utils
|   |   |   |-- Player/
|   |   |   |-- Interaction/         <- grab system, interactables
|   |   |   |-- Items/               <- valuables, tools, carts
|   |   |   |-- Enemies/
|   |   |   |-- LevelGen/
|   |   |   |-- Economy/             <- money, upgrades, shop
|   |   |   |-- UI/
|   |   |   |-- Audio/
|   |   |   |-- Save/
|   |   |   |-- Net/                 <- empty until Phase 8
|   |   |   |-- DevTools/            <- console, spawn menu, gizmos
|   |   |-- Settings/                <- URP assets, InputActions, mixer presets
|   |   |-- Tests/
|   |       |-- EditMode/
|   |       |-- PlayMode/
|   |-- ThirdParty/                  <- imported asset packs, never edit in place
|   |-- Plugins/                     <- Steamworks.NET, networking SDK
|-- Packages/
|-- ProjectSettings/
```

**Rules**
- Only `_Project/` contains first-party work. `_` sorts it to the top.
- Third-party packs go in `ThirdParty/` untouched; copy and modify into `_Project/` if needed.
- One folder per system under `Scripts/`; each gets its own assembly definition (see 4.2).
- Prefabs mirror the script folder names so things are easy to find.

---

## 4. Code Architecture

### 4.1 Principles
1. **Composition over inheritance.** Small components (`Health`, `Grabbable`, `NoiseEmitter`) instead of deep class trees.
2. **Data in ScriptableObjects, behavior in components.** Tuning a valuable's fragility should never need a code change.
3. **Events decouple systems.** UI listens to events; gameplay never references UI.
4. **No hidden global state.** Avoid static mutable fields. Use a small number of explicit services (see 4.3).
5. **Input is intent, not polling.** Gameplay consumes an input struct, never `Input.GetKey` directly (this is what makes networking and replays possible).
6. **Simulation separate from presentation.** Logic decides; visuals and audio react.
7. **Fail loudly in development.** Use assertions and clear logs; remove noise before release.

### 4.2 Assembly Definitions
One `.asmdef` per major folder to keep compile times fast and dependencies honest.

```
Core            <- depends on nothing
Player          -> Core
Interaction     -> Core
Items           -> Core, Interaction
Enemies         -> Core, Items
LevelGen        -> Core
Economy         -> Core, Items
UI              -> Core, Economy, Items (read-only)
Audio           -> Core
Save            -> Core, Economy
Net             -> Core (+ everything it replicates, added in Phase 8)
DevTools        -> everything (editor/dev builds only)
Tests           -> whatever is under test
```
Rule: **lower layers never reference higher ones.** If `Core` needs to call into `Enemies`, use an event or interface instead.

### 4.3 Core Services
Created once in `Boot.unity`, held by a single `Services` registry (a simple static class with typed properties, no magic reflection).

| Service | Responsibility |
|---|---|
| `GameStateMachine` | Menu / Lobby / Loading / InLevel / Extraction / Results / GameOver |
| `EventBus` | Typed, lightweight publish/subscribe (e.g. `ValuableDamaged`, `NoiseEmitted`, `LevelCompleted`) |
| `PlayerRegistry` | Tracks all players (even in offline mode there is exactly one); everything that needs "the player" asks here, never `FindObjectOfType` |
| `SaveService` | Reads/writes profile JSON |
| `AudioService` | Plays pooled one-shots, music transitions |
| `SettingsService` | Graphics, audio, controls, sensitivity |
| `GameClock` | Wrapper over `Time` so pausing and netcode time stay consistent |

### 4.4 Key Patterns
- **State machines** (plain C# classes) for game flow and enemy AI.
- **Object pooling** for impact particles, sound sources, and debris.
- **ScriptableObject definitions + runtime instances**: a `ValuableDefinition` asset describes the item; a `Valuable` component holds its current value.
- **Interfaces** for cross-system contracts: `IDamageable`, `IGrabbable`, `IInteractable`, `INoiseListener`.

### 4.5 Conventions
- Namespace: `Game.<System>` (e.g. `Game.Player`). Keep the product name out.
- Private fields with `[SerializeField] private`, `_camelCase` naming; public API as properties.
- No `GetComponent`, `Find*`, or allocations inside `Update`.
- Physics work in `FixedUpdate`; input sampled in `Update`.
- No magic numbers: expose as serialized fields or ScriptableObject data.
- One MonoBehaviour per file, filename equals class name.

---

## 5. Collision Layers and Tags

| Layer | Used by |
|---|---|
| `Default` | unused, avoid |
| `Player` | player capsule |
| `Grabbable` | valuables, props, held items |
| `Held` | an object currently carried (ignores the player to avoid launching them) |
| `Enemy` | enemies |
| `Level` | static room geometry |
| `Trigger` | extraction zones, noise areas, room triggers |
| `NoClip` | debug |

Set the **Layer Collision Matrix** deliberately in Phase 1; wrong matrices cause the most confusing physics bugs later.

---

## 6. Systems Breakdown

### 6.1 Player
- First-person controller: walk, sprint, crouch, jump.
- Stamina with exhaustion threshold (cannot sprint until partly recovered).
- Health, damage, death, spectate (later: revive).
- Split into three parts: `PlayerInputReader` (produces an input struct), `PlayerMotor` (consumes it), `PlayerStats` (health, stamina).
- Head bob, footstep noise (feeds the noise system).

### 6.2 Physics Grab and Carry (the heart of the game)
- Raycast/spherecast from the camera to find a `Grabbable`.
- While held, drive the Rigidbody toward a hold point using velocity or force (not parenting), so held objects still collide with the world.
- Scroll to adjust hold distance; optional rotate modifier.
- Heavy objects move slower (mass-based speed cap) and drain stamina.
- Auto-release if the object is too far from the hold point (stuck behind geometry).
- Throwing: release with the current velocity (optional modifier for a stronger throw).
- Impact response is delegated to the object (`IDamageable`), not the grabber.

### 6.3 Valuables
- `ValuableDefinition` (ScriptableObject): name, base value, mass, fragility, break threshold, audio set, prefab.
- `Valuable` component: current value, `OnCollisionEnter` converts impact impulse above a threshold into value loss; fires `ValuableDamaged` events.
- Visual feedback: crack decals or material swap at value thresholds; "ping" popups showing money lost.
- Variants: fragile glass, heavy furniture, awkward-shaped items, "bomb" items (risk/reward).

### 6.4 Noise System
- `NoiseEmitter` (anywhere): sprint steps, impacts, dropped items, shouting (proximity voice later).
- Emits `NoiseEmitted(position, loudness)` through the event bus.
- Enemies implement `INoiseListener` and decide how to react based on distance and loudness.
- Debug gizmo shows noise radius.

### 6.5 Enemies
- Built on `NavMeshAgent` + a state machine: `Idle -> Patrol -> Investigate(noise) -> Chase -> Attack -> Cooldown`.
- Senses: hearing (noise events), sight (cone + line-of-sight raycast), optional proximity.
- Start with **one** enemy (a basic chaser). Add a second with a different behavior only after the first is fun.
- Enemy variety ideas, original to your theme: a blind-but-loud-sensitive type, a fast short-range type, a stationary hazard type.
- Data-driven via `EnemyDefinition` assets (speed, sight range, hearing sensitivity, damage).

### 6.6 Level Generation
- **Modular room prefabs** with connection sockets (doorways) on a fixed grid.
- A `LevelGenerator` takes a **seed** and a `LevelDefinition` (room count, pools, difficulty), then:
  1. Places a start room.
  2. Grows the layout by attaching compatible rooms to open sockets.
  3. Seals unused doorways.
  4. Places the extraction point far from the start.
  5. Spawns valuables on spawn points according to value budget.
  6. Spawns enemies according to difficulty budget.
  7. Bakes the NavMesh at runtime (`NavMeshSurface.BuildNavMesh`).
- **Deterministic from a seed**: required so multiplayer clients can rebuild the identical level.
- Start with 5 hand-built rooms; the generator only needs to prove the idea.

### 6.7 Extraction and Round Loop
- Goal: reach a **quota** of value, delivered into an **extraction zone** (or cart/truck).
- Zone counts value only for objects fully inside; value drains if objects are damaged afterward.
- Win: quota met and players exit. Lose: all players dead.
- Level progression: each completed level raises the quota and difficulty.

### 6.8 Economy and Meta Progression
- Money earned per run feeds a **shop** in the hub/lobby.
- Upgrades via `UpgradeDefinition` assets: stamina, health, grab strength, sprint speed, etc.
- Persistent profile saved as JSON (versioned).
- Keep numbers in a spreadsheet in `docs/design/` and mirror them into assets.

### 6.9 UI
- HUD: health, stamina, held-object value, run total vs quota, noise indicator (optional).
- Menus: main menu, pause, settings (graphics, audio, controls, rebind), lobby, results screen.
- Rule: UI only subscribes to events; it never drives gameplay.

### 6.10 Audio
- Pooled `AudioSource`s with 3D spatial settings.
- Impact sounds chosen by material and force.
- Ambient and music layers controlled by tension (enemy nearby raises intensity).
- AudioMixer groups: Master, Music, SFX, Voice, Ambient.

### 6.11 Save System
- JSON profile at `Application.persistentDataPath`.
- Contains version number, money, upgrades, settings, unlocked items.
- Atomic write (write temp file, then replace) and a backup copy.
- Save data is separate from runtime state so multiplayer clients can load only what they need.

### 6.12 Dev Tools (build these early, they pay back constantly)
- In-game console with commands (`spawn`, `god`, `noclip`, `seed`, `timescale`).
- Spawn menu for any valuable or enemy.
- Debug overlays: noise radii, enemy state labels, physics contacts, FPS and physics-body count.
- A **Sandbox scene** that loads instantly with all tools enabled.

### 6.13 Multiplayer (Phase 8, designed-for from Phase 1)
- Host-client model; host runs the authoritative physics.
- Clients send **requests** (grab, release, throw); the host validates and applies.
- Replicate: player transforms, held-object state, Rigidbody transforms for nearby/active objects, valuable value, enemy state, level seed.
- Proximity voice: audio sources on each player with 3D settings; mute/volume per player.
- Lobby via Steam (invites, friend join) once networking is chosen.

---

## 7. Networking-Ready Checklist
Follow these from the first script, even offline, to avoid a painful retrofit:

1. Input becomes an **intent struct**; gameplay never calls `Input.*` directly.
2. Every shared-state change goes through a **single method** that could become an RPC (e.g. `Grabbable.RequestGrab(player)`).
3. No singletons storing per-player data; use `PlayerRegistry`.
4. **All spawning goes through one spawner/factory**, never scattered `Instantiate` calls.
5. Level generation takes a **seed** and is deterministic.
6. Physics changes in `FixedUpdate` only.
7. Time comes from `GameClock`, not `Time` directly.
8. Visuals and audio react to events; they do not carry gameplay state.
9. Objects that matter get a stable **ID** (assigned at spawn) for future replication.
10. Save data is separate from live state.

---

## 8. Roadmap (phased)

Estimates assume about 10 hours/week. Exams and hackathons will stretch these, which is normal; plan buffer weeks.

### Phase 0: Foundations (about 1 week)
- Install Unity Hub, Unity 6 LTS, IDE.
- Create the project (3D URP template), repo, `.gitignore`, `.gitattributes` with LFS.
- Install packages (section 2.1), create the folder structure (section 3), set up asmdefs.
- Configure Input Actions asset, layers and collision matrix.
- Create `Boot` scene, `Services` registry, event bus skeleton, `Sandbox` scene.
- **Exit criteria:** project opens clean on a fresh clone; capsule can be dropped in Sandbox without errors.

### Phase 1: Player and Grab (about 2 weeks)
- First-person controller with stamina.
- Grab system with distance control, mass-based speed, auto-release.
- Greybox test room with a handful of boxes and spheres of varying mass.
- **Exit criteria:** carrying, swinging, dropping, and throwing feels good for 5 straight minutes with no jitter or launching.

### Phase 2: Valuables and Damage (about 2 weeks)
- `ValuableDefinition` assets, `Valuable` component, impact-to-value conversion.
- HUD showing held value and total.
- Impact audio and basic particle feedback.
- **Exit criteria:** dropping a vase from head height visibly and audibly costs money; resting it gently costs nothing.

### Phase 3: Extraction Loop (about 2 weeks)
- Extraction zone, quota, win/lose states, results screen.
- Game state machine wired end to end (Menu -> Level -> Results -> Menu).
- Simple hand-built level (one real, polished map).
- **Exit criteria:** a complete, repeatable run from menu to results works in under 10 minutes of play.

### Phase 4: Enemy and Noise (about 3 weeks)
- Noise events from impacts and sprinting.
- First enemy with patrol, investigate, chase, attack.
- Player damage, death, and spectator mode.
- **Exit criteria:** you get genuinely nervous carrying a vase past a patrolling enemy.

### Phase 4.5: Networking Spike (about 1-2 weeks, throwaway)
- Separate scratch project or branch.
- Two players, one shared Rigidbody, both can grab it.
- Evaluate the options in section 2.2, pick one, record in `DECISIONS.md`.
- **Exit criteria:** a decision with a written reason, and a known list of pain points.

### Phase 5: Procedural Levels (about 4 weeks)
- Modular room kit (start with 5, grow to 15+), socket system.
- `LevelGenerator` with seed, runtime NavMesh bake, spawn budgeting.
- Difficulty scaling across successive levels.
- **Exit criteria:** 20 different seeds all produce playable, fully connected, bakeable levels.

### Phase 6: Meta Progression (about 2 weeks)
- Shop, upgrades, persistent profile and save system.
- Second valuable family and (optionally) second enemy.
- **Exit criteria:** quitting and reopening the game keeps your money and upgrades.

### Phase 7: Menus, Settings, Audio, Polish (about 3 weeks)
- Main menu, pause menu, settings with rebinding.
- Mixer groups, music layers, ambient sound.
- Lighting pass, post-processing, horror atmosphere.
- **Exit criteria:** a friend can play a full session without asking you how anything works.

### Phase 8: Multiplayer (about 6-8 weeks)
- Integrate the chosen networking stack.
- Replicate players, grabs, physics objects, valuables, enemies, level seed.
- Lobby, invites, proximity voice.
- **Exit criteria:** two people on different machines complete a full run together.

### Phase 9: Steam and Release Prep (about 4+ weeks)
- Steamworks integration (lobbies, achievements, cloud saves).
- Playtesting rounds, balance, performance passes, bug bash.
- Store page, trailer, build pipeline.
- **Exit criteria:** a stranger can install it, play with a friend, and finish a run.

**Realistic total:** roughly 6-9 months to a multiplayer-complete build at part-time pace. A fun **offline vertical slice (end of Phase 4)** should exist after about 3 months. That is your first real milestone.

---

## 9. Feature List

### MVP (Phases 1-3)
- [ ] First-person movement and stamina
- [ ] Physics grab, carry, throw
- [ ] Valuables with impact-based value loss
- [ ] Extraction zone and quota
- [ ] One handcrafted level
- [ ] HUD and results screen

### Version 1 (Phases 4-7)
- [ ] Noise system
- [ ] 2 enemy types
- [ ] Procedural levels from a room kit
- [ ] Shop and persistent upgrades
- [ ] Settings and rebinding
- [ ] Music and ambience
- [ ] Death, spectate

### Version 1.x (Phases 8-9)
- [ ] 2-4 player online co-op
- [ ] Proximity voice
- [ ] Steam lobbies and invites
- [ ] Achievements

### Stretch
- [ ] Carts or vehicles for hauling
- [ ] Revive mechanic
- [ ] Tool items (flashlight, grabber upgrade, noise-maker)
- [ ] Cosmetics
- [ ] Gamepad support polish
- [ ] Daily seed challenge
- [ ] Localisation

---

## 10. Art and Audio Strategy (solo-friendly)

- **Style:** stylised low-poly. It hides weak art skills and runs well.
- **Greybox first.** Build levels with ProBuilder and flat colours; only art-pass after the gameplay is fun.
- **Free/cheap asset sources (check each license):** Kenney (CC0), Quaternius (CC0), Unity Asset Store sales, Mixamo for humanoid animation.
- **Blender** for custom props and edits.
- **Audio:** freesound.org (check license per sound), Sonniss GDC bundles, record your own impact sounds (kitchenware thuds are great).
- Keep an `ASSET_CREDITS.md` from day one so attribution is never a scramble.

---

## 11. Version Control

### 11.1 Workflow
- Branches: `main` (always playable), `dev` (integration), `feature/<name>`.
- Small, frequent commits; message format: `system: what changed` (e.g. `grab: clamp hold speed by mass`).
- Tag each phase exit: `phase-1-done`, etc.
- Never commit the `Library/`, `Temp/`, `Logs/`, or `UserSettings/` folders.

### 11.2 Git LFS Patterns (`.gitattributes`)
Track at least: `*.png *.jpg *.psd *.tga *.fbx *.blend *.wav *.mp3 *.ogg *.mp4 *.unity` (scenes are optional; text YAML merges poorly anyway, so keep scenes small and avoid parallel scene edits).

### 11.3 Unity Project Settings for Git
- Asset Serialization: **Force Text**
- Version Control Mode: **Visible Meta Files**

---

## 12. Testing and Debugging

- **EditMode tests** for pure logic: value-loss formula, state machines, level-generator connectivity, save serialization.
- **PlayMode tests** for integration: grab-then-drop loses value, extraction counts correctly.
- Level generator **soak test:** generate 100 seeds in a loop and assert connectivity and NavMesh validity.
- Profile regularly (Profiler, Physics Debugger). Budget early: count active Rigidbodies and make sleeping work for you.
- Keep a `KNOWN_ISSUES.md` rather than carrying bugs in your head.

---

## 13. Decision Log (start in Phase 0)

| Decision | Status | Notes |
|---|---|---|
| Engine = Unity 6 LTS, C# | Decided | |
| Platform = Windows PC, Steam later | Decided | |
| Scope = solo-first, multiplayer later | Decided | |
| Game = 3D R.E.P.O.-style (not 2D Metroidvania) | Decided | |
| Render pipeline = URP | Proposed | Confirm in Phase 0 |
| Networking stack | **Open** | Decide after Phase 4.5 spike |
| Voice chat solution | **Open** | Follows networking choice |
| Theme/setting and title | **Open** | Decide before Phase 2 art pass |
| Art style | Proposed: stylised low-poly | |

---

## 14. Risks and Mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Physics over network is much harder than expected | High | Phase 4.5 spike; host-authoritative design; request-based interactions |
| Scope creep (the classic solo killer) | High | Strict MVP list; new ideas go to the Stretch list, not into the build |
| Art workload overwhelms | Medium | Greybox first, asset packs, low-poly style |
| Burnout / semester conflicts | Medium | Buffer weeks; small weekly goals; keep a playable build at all times |
| Procedural levels produce broken layouts | Medium | Seeded generation, soak tests, validation step before spawning |
| Physics jitter or tunneling with held objects | Medium | Velocity-based hold, continuous collision on fast objects, tuned layer matrix |
| Performance (many Rigidbodies) | Medium | Sleeping, object caps, simple colliders, profile every phase |
| Netcode lock-in | Low-Medium | Keep `Net/` behind interfaces; gameplay never imports the networking SDK directly |

---

## 15. Definition of Done: Offline Vertical Slice

The first real milestone (end of Phase 4). It is done when:
- A new player can start the game, finish a run, and see results without help.
- One level, one enemy type, at least 5 valuable types, and noise all work together.
- Carrying a fragile item past a patrolling enemy is genuinely tense.
- It runs at a stable 60 FPS on a mid-range PC.
- It has been played by at least 3 other people, and their feedback is written down.

---

## 16. Immediate Next Steps

1. Install Unity Hub and Unity 6 LTS; confirm the editor opens.
2. Create the GitHub repo, `.gitignore`, and LFS config.
3. Create the Unity project and the folder structure in section 3.
4. Pick a working title and theme (a rough one is fine).
5. Start Phase 0, then Phase 1.
