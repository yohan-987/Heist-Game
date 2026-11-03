# Unity Setup Guide

Step by step, from an empty PC to a playable sandbox. Do the parts in order. Do not skip Part 3: a clean compile is worth more than anything else you can do first.

Everything in `Assets/_Project/Scripts` was written without the Unity editor, so expect some compile errors on the first open. Part 3 says how to handle them.

---

## Part 1. Install the tools

1. Install **Git** and **Git LFS**. Open a terminal and run `git lfs install` once.
2. Install **Unity Hub**, sign in, then install **Unity 6 LTS** (the latest 6000.x LTS). Under modules, tick **Windows Build Support**.
3. Install an editor for C#: Visual Studio Community (with the "Game development with Unity" workload), Rider, or VS Code. In Unity, set it under Edit > Settings > External Tools > External Script Editor.

## Part 2. Get the project open

The repo holds the scripts, folder tree and assembly definitions. Unity's own settings come from a template, so do this once.

1. Clone the repo: `git clone https://github.com/yohan-987/Heist-Game.git`
2. In Unity Hub, click New project. Pick the **Universal 3D** template (the URP one). Name it `TemplateTemp` and put it anywhere outside the repo. Create it, let it open, then close Unity.
3. Copy these from `TemplateTemp` into the root of the clone:
   - the `ProjectSettings` folder
   - the `Packages` folder
   - the `Assets/Settings` folder **and** `Assets/Settings.meta` (this holds the URP render pipeline asset that `ProjectSettings` points to; without it you get pink materials)
4. In Unity Hub, click Add > Add project from disk, and choose the clone's root folder. Open it. The first import takes several minutes.
5. If Unity says there are compile errors and offers Safe Mode, choose to enter it, or press Ignore. Go on to Part 3.
6. Open Window > Package Manager, set the list to "In Project", and check that these exist. Add any that are missing with the "Unity Registry" list:
   - **Input System**. If Unity asks to enable the new input backend and restart, say yes. If it does not ask, go to Edit > Project Settings > Player > Other Settings > Active Input Handling and pick **Input System Package (New)** or **Both**.
   - **AI Navigation** (needed for the enemy NavMesh)
   - **Test Framework**
   - **ProBuilder** (for greyboxing rooms)

## Part 3. Fix the compile and run the tests

1. Open the Console (Window > General > Console). Clear it, then wait for the script compile to finish.
2. For each red error, read the file name and line. The likely causes, in order:
   - **`CS0246` type or namespace not found.** An assembly is missing a reference. Click the `.asmdef` file in the same folder as the script, and in the Inspector add the assembly under Assembly Definition References. Click Apply.
   - **A Unity 6 API name that differs** from what the code assumes. The code uses `Rigidbody.linearVelocity`, `linearDamping`, `angularDamping` and `FindObjectsByType`.
   - **A genuine typo.** Fix it.
3. Copy the full text of errors you cannot fix and send it to Claude. One round of this usually clears the lot.
4. When the Console is clean, open Window > General > Test Runner. Pick the **EditMode** tab and click **Run All**.
5. Expected result: **126 passed, 0 failed.** If any fail, send the test names and messages.

Do not carry on until step 5 is green.

## Part 4. Layers and physics

1. Edit > Project Settings > Tags and Layers. Add these seven layers with exactly these names (any empty slot from 6 up):
   `Player`, `Grabbable`, `Held`, `Enemy`, `Level`, `Trigger`, `NoClip`
2. Edit > Project Settings > Physics > Layer Collision Matrix. Untick the box where **Held** meets **Player**, so a carried object cannot shove the player.
3. If the Console later says `Layer '...' is missing`, a name above has a typo.

## Part 5. Build the sandbox scene

Create `Assets/_Project/Scenes/Sandbox.unity` (File > New Scene > Basic, then save it there). Delete the default light only if the scene already has one from the template.

### 5.1 Floor

1. GameObject > 3D Object > Cube. Name it `Floor`. Position (0, -0.5, 0), scale (40, 1, 40).
2. Set its Layer to **Level**.

### 5.2 Player

1. GameObject > Create Empty. Name it `Player`. Position (0, 1, 0). Layer **Player**. (Pick "Yes, change children" if asked.)
2. Add a **Character Controller**. Set Height 1.8, Radius 0.35, Center (0, 0.9, 0).
3. Move the **Main Camera** inside `Player` and rename it `Head`. Local position (0, 1.6, 0), rotation (0, 0, 0). Keep its Audio Listener; there must be exactly one in the scene.
4. On `Player`, add these components. Order does not matter:
   - `PlayerStats`
   - `PlayerInputReader`
   - `PlayerLook`
   - `PlayerMotor`
   - `PlayerGrabber`
   - `PlayerInsanity`
   - `NoiseEmitter`
5. Drag `Head` into the **Head** field of `PlayerStats`, `PlayerLook` and `PlayerMotor` (all three have one).
6. On `PlayerStats`: Player Id 0, Is Local ticked.
7. Press Play. You should be able to look with the mouse, walk with WASD, sprint with Shift, crouch with Ctrl and jump with Space. If the mouse does nothing, recheck Part 2 step 6 (Input System backend).

### 5.3 A grabbable valuable

1. Create a Cube. Name it `Vase`. Scale (0.4, 0.5, 0.4). Layer **Grabbable**.
2. Add a Rigidbody (mass 3). Add `Grabbable` and `Valuable`.
3. Create the data asset: right-click in the Project window > Create > Game > Valuable. Name it `Valuable_Vase`. Set Display Name, Base Value 500, Mass 3.
4. Drag `Vase` from the Hierarchy into `Assets/_Project/Prefabs/Valuables` to make a prefab.
5. Drag that prefab into the asset's **Prefab** field, and drag the asset into the `Valuable` component's **Definition** field on the `Vase` in the scene. Apply the override to the prefab if Unity offers it. Duplicate the vase and put three or four around the player.
6. Press Play. Look at a vase and hold the left mouse button. It should float in front of you. Release to drop, right mouse to throw, scroll to change the distance, R to rotate. Damage messages only appear once the run starter from 5.5 is in the scene.

### 5.4 The medicine counter

1. GameObject > Create Empty. Name it `MedicineCounter`. Position (10, 1, 0). Layer **Trigger**.
2. Add `ExtractionZone`. It adds a Box Collider by itself. Set the box size to (4, 2, 4).
3. Optional but handy: add a flat cube on the floor under it with a bright material so you can see where it is.
4. Carry a vase in and let go, making sure it lies fully inside the box. After it has been still for about half a second, the machine **swallows** it: the vase disappears and its value is committed for good. Nothing can change that value afterwards, and nothing can be pulled back out.
5. Add an **ExitZone** later (Part 6, step 7) for leaving the level.

### 5.5 Start a run

Nothing starts a run in a bare scene, so add the helper:

1. GameObject > Create Empty. Name it `Run`.
2. Add `SandboxRun`. It adds `RunTracker` for you. Quota Doses 3, Value Per Dose 500.
3. Press Play. The Console should print a line like `Run: delivered 0, lost 0, banked doses 0 ...`. Deliver a vase and it should update. Two vases of 500 value mean 2 banked doses.
4. Without this object, a vase dropped in the counter is still swallowed, but nothing records it.

**If your Player was set up before the design update:** the `PlayerInsanity` component in the scene still holds the old saved numbers (rate 0.4, swell 3). Click the component's three-dot menu and choose **Reset**, then check the Rate shows 0.25 and Swell Seconds 5. Scripts only apply new defaults to components that are added fresh.

Do **not** use the `Bootstrap` component in this scene. It loads a scene called `MainMenu`, which does not exist yet.

### 5.6 Optional: debug overlay

Create an empty object and add `DebugOverlay`. It shows live numbers on screen.

## Part 6. Playtest checklist, in this order

Tick these off before touching anything else. If one fails, fix it before moving on.

1. Walk (WASD), look, sprint (Shift), crouch (Ctrl), jump (Space). Stamina drains while sprinting and recovers.
2. Grab, hold, rotate, throw a vase. Heavier objects slow you down.
3. Drop a vase hard. It loses value. Rest it gently. It loses none.
4. Deliver vases to the counter. Each one is swallowed and the Console shows banked doses rising once 500 value has been committed. Drop a vase hard before delivering: it should be worth less. After delivery nothing can change its value.
5. Wait about 80 seconds. Pressure (insanity) climbs by 0.25 per second. Below 20 a dose is refused and not used up. Above that, press **Q** to take a dose when you have one banked: pressure falls by 40. Pressing Q again within 10 seconds is refused too.
6. Let pressure hit 100 without a dose. You get a **5 second** warning, during which a dose still saves you. After that the player bursts (the balloon death).
7. Exit: create an empty object named `Exit` on a spot near the start, layer **Trigger**, add `ExitZone` (it adds a Box Collider; make it about 4 by 2 by 4 metres). Deliver enough value to reach the quota, then stand in it. The run should finish as a success once every living player is inside.

If all seven work, the core loop works and you are ready for the enemy parts.

## Part 7. Add the enemy and hospital pieces, one at a time

Only start after Part 6 passes.

### 7.1 The radio (music)

1. Create a cube named `Radio`, layer **Grabbable**, Rigidbody, `Grabbable`.
2. Add `MusicPlayer`. Optionally assign an AudioSource with any looping clip.
3. Hold it with the left mouse button and press **E** to toggle it. While it plays, nearby players lose insanity over time, and it emits Music noise.

### 7.2 Apples

1. Create a sphere named `Apple`, red material, layer **Grabbable**, Rigidbody, `Grabbable`, `Apple`. Assign its Renderer in the Apple component so rot can tint it.
2. Throw it. On impact it sends nearby doctors fleeing and adds 10 insanity to the thrower. Then it disappears.
3. To test rot, set `SandboxRun`'s Difficulty to **High** and wait 90 seconds before throwing.

### 7.3 NavMesh and the lethal doctor

1. Select `Floor`. Add a **NavMesh Surface** component (AI Navigation package) and click **Bake**.
2. Make an **EnemyDefinition** asset (Create > Game > Enemy), build a capsule prefab with a NavMeshAgent, `EnemyBrain` and the layer **Enemy**. Assign the prefab in the definition and assign the definition on the brain.
3. Place one in the scene. It should wander, hear loud footsteps and thrown objects, and chase. It should ignore the radio.

### 7.4 Ground doctor and spider nest

1. Ground doctor: a capsule lying flat, NavMeshAgent, `GroundDoctor`, layer **Enemy**. Turn the radio on within its hearing range. It should wake, crawl to you, and grab you by the waist. Mash **Space** to break free. You are slowed while held.
2. Spider nest: any object with `SpiderNest`. With the radio playing and you within 8 m, spiders appear on your screen.
3. Spiders need the overlay: a Canvas with a full-screen panel and `CrawlerOverlay`. The Crawler Prefab is a UI Image (any spider sprite or a black circle). Press **F** to swat.

### 7.5 Jumpscares

1. Add `JumpscareDirector` to an empty object. Add `ScareTrigger` volumes (box colliders set to Trigger) where you want scripted scares. Each scripted scare fires once per run, with a 45 second minimum gap. Above 50 insanity, random hallucinations begin.

## Part 8. Generated levels (later)

After the sandbox is solid:

1. Greybox 3 to 5 rooms with ProBuilder: a start room, a corridor, a ward, an operating room, and a medicine counter room.
2. On each room root add `RoomAuthoring`. Set its size in grid cells (one cell is 8 m by 8 m), add door sockets on cell edges, and drag in child transforms for valuable spawns, enemy spawns and nuisance spawns. Start rooms need a player spawn. The extraction room needs an inactive child holding the `ExtractionZone`, assigned as the Extraction Zone Root.
3. Create a **LevelDefinition** asset, fill the room pools and valuable pool, and set Room Count.
4. Add `LevelBuilder` to a scene object, assign the definition, and call `Build(seed)` from a test script. Pass the player count as the second argument (`Build(seed, players)`); it sizes the quota and loot budget. Feed `QuotaDoses` and `ValuePerDose` from the result into `RunTracker.Begin`. If the Console warns that the level cannot be won, add valuable spawn points.

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| Everything is pink | `Assets/Settings` was not copied from the template, or the render pipeline is not assigned in Project Settings > Graphics |
| `Layer '...' is missing` in the Console | Layer name typo in Part 4 |
| Mouse and keys do nothing | Active Input Handling is still the old Input Manager only |
| Cannot grab anything | The object is not on the **Grabbable** layer, or has no `Grabbable` |
| Carried object pushes the player | The Held/Player collision is still ticked in the matrix |
| Vases never count at the counter | They are not fully inside the box, or the zone's layer or collider is wrong. They also need a Rigidbody |
| Dose key does nothing | No run is active (add `SandboxRun`), or no dose is banked |
| Enemy stands still | No NavMesh baked, or the enemy is not on the baked surface |
| `Bootstrap` errors about a missing scene | Do not use it in the sandbox |
