# Game Design

Working title: TBD. Theme: a haunted mental hospital. Solo-first, co-op ready.
Status: **version 2, design review of 2026-11-03.** Every number is a starting value that has been checked arithmetically (see section 4.3) but not yet playtested. Tune in playtests, not in theory.

---

## 0. What Changed in Version 2 and Why

Version 1 had a strong idea (loot becomes medicine, medicine keeps you alive) but several systems fought each other or could not be won. This review fixes them.

| Problem in version 1 | Why it hurts | Fix in version 2 |
|---|---|---|
| Delivered loot could still lose value, and carrying it back out lowered the dose count | Doses already taken became a hidden debt; exploitable and confusing | The compounder **consumes** accepted loot and commits its value permanently (4.2) |
| Quota was a share of whatever loot spawned; loot did not scale with player count | Four players could not survive a 10-minute run and still meet quota | Quota is set in doses first, and the loot budget is **derived** from quota, player count and run length (4.3) |
| Insanity rose at 0.4 per second | Constant tax; death in under 4 minutes without medicine | Slower baseline (0.25 per second), events add pressure, 5-second last-chance swell (4.1) |
| The radio gave about 3 relief per second to every player in range, with no limit | One radio replaced the whole medicine system; multiple radios stacked | Battery-limited, 0.6 per second, strongest source only (4.5) |
| "A ground doctor holding you while a chasing doctor closes in is deadly by design" | Stacked hard-control and lethal threats feel unfair, not scary | **Threat Director** rules: telegraphs, a two-hazard cap, no pile-ons (4.9) |
| Hallucinations were undefined | Fake threats can feel like cheating | Hallucinations are harmless and readable (4.8) |
| Anyone could drain the shared dose pool | One greedy or careless player ruins the team | Dose gate: minimum pressure and a cooldown (4.1) |
| Level ended at an undefined point; counter and exit undefined | No greed-versus-safety decision | Quota **unlocks** the exit; the player chooses when to leave (4.10) |
| Loot "in beds" | Stealing from sleeping patients clashes with the tone rule | Loot is equipment and staff belongings; patients are scenery (1, 4.2, 10) |
| Everything planned for the first playable | Too many interacting systems to debug at once | Three build layers with playtest gates (6) |

---

## 1. Premise

The hospital has been sealed by a contamination called **the Haze**. It turned the doctors into zombies and keeps the patients in a deep sleep in their beds. You and your team are scavengers. You slip through the wards, salvage valuable equipment and belongings, and bring them to the **pharmacy wing**, where the **compounder** turns them into **stabiliser doses**.

You need those doses yourself. The Haze builds **Pressure** inside every scavenger, and without doses it climbs until you swell up like a balloon and burst.

Tone: horror-comedy. Frightening, never cruel. The doctors are the monsters. The patients are sleeping bystanders: never a threat, never a target, never a source of loot.

Naming: the player-facing term is **Pressure** (a gauge that fits the balloon death). The internal code name stays `Insanity`. Alternative UI word: Stability.

## 2. Core Loop

1. Leave the **safe start wing**, move through the hospital and salvage valuables with the physics grab system. Carrying carefully is the core skill, and impacts cost value.
2. Bring valuables back to the **compounder** in the pharmacy wing. It swallows them and produces doses.
3. Doses do two jobs: they build the team's **quota** and they lower your Pressure. Every dose you take is a dose the team does not bank.
4. Survive the doctors, manage noise, keep Pressure below 100.
5. When the quota is met, the **exit unlocks**. Leave now with what you have, or stay for more loot and a bigger bonus.

## 3. Design Pillars

1. **Physics is the gameplay.** Everything is carried, thrown, dropped, broken.
2. **Value is fragile.** Impacts destroy money, but only before delivery.
3. **Noise draws danger.** Footsteps and impacts alert doctors.
4. **Every tool has a price.** Music, apples and the defibrillator each solve one problem and cause another.
5. **Fear, not frustration.** The player should be scared because they understand the threat, never because three unrelated effects landed at once.
6. **Every death is explainable.** The player can always say what they did wrong.
7. **Co-op ready, offline first.**

---

## 4. Systems

### 4.1 Pressure and Medicine

| Value | Setting |
|---|---|
| Starting Pressure | 0 |
| Baseline rise | **0.25 per second** (0.3125 on high difficulty) |
| Untreated time from 0 to 100 | 400 seconds (320 on high) |
| One dose lowers Pressure by | 40 (so one dose buys 160 seconds) |
| Stages | 50 Uneasy, 75 Terrified, 100 Swelling |
| Swell warning | **5 seconds**, during which a dose still saves you |

- **Events add Pressure** on top of the baseline, so danger costs medicine: an apple throw +10, a ground doctor grab +8, a scripted scare +5. Over an average run these add about 33.
- **Stages are readable.** At 50: mild hallucinations begin and the screen edges tighten. At 75: the heartbeat becomes audible, the character visibly puffs up, scare frequency rises. At 100: a swelling animation and a loud warning, then the burst.
- **Dose gate (shared pool protection).** A dose is refused, and not consumed, if your Pressure is below 20. After a dose there is a 10-second cooldown. The cooldown does not apply during the swell warning. This stops hoarding, wasting and griefing.
- **Doses are drawn from the pool anywhere** through a wrist injector (key Q). No trip back to the counter is needed.
- **Make the tax visible.** The HUD always shows: doses banked, quota, doses you have taken, and your own Pressure. In co-op, each teammate's Pressure shows as a ring around their name tag.

### 4.2 Loot and the Compounder

- **Loot sources:** abandoned medical equipment, instruments, staff belongings, broken electronics, research gear, hidden contraband. Patients are never looted.
- **Placement:** value is weighted toward the **far wing** (the vault wing), so greed means distance. Roughly 40 percent of total value sits in the farthest third of the rooms.
- **Counter location:** the compounder is in the **pharmacy wing next to the start room**. The first haul is short, so the first dose arrives before the first hallucination at about 200 seconds.
- **Accepting loot is a transaction:**
  1. A valuable is fully inside the intake, not held, and settled for about 0.5 seconds.
  2. Its current value is read.
  3. That value is **committed permanently** and the valuable is consumed (sucked into the machine).
  4. Doses produced = floor(total committed value ÷ value per dose). Any remainder carries over and shows on a gauge.
- Damage **before** delivery costs value. After delivery, nothing can change it. Nothing can be pulled back out.
- **Compounder:** a physical machine (glass jars, gauges, a hopper, a dispenser). Personality through sound and animation, with no AI of its own. A friendly NPC is a later option.

### 4.3 Medicine Economy and Budgeting

The level must always be winnable by a competent team. The loot budget is **derived**, not guessed.

Let:
- Q = quota in doses, **4 + 2 per player** (6, 8, 10, 12 for 1 to 4 players)
- P = number of players
- D = doses each player is expected to need over a 10-minute run (**3 normal, 4 high**), which covers baseline rise plus about 33 pressure from events, with about one dose of slack
- E = expected collection efficiency, the share of spawned value that is actually delivered undamaged (**0.65 normal, 0.60 high**)
- v = value per dose (500 normal, 600 high)

**Loot budget = v × (Q + P × D) ÷ E**

| Players | Quota (doses) | Doses to survive | Normal budget | High budget |
|---|---|---|---|---|
| 1 | 6 | 3 | about 6,900 | about 10,000 |
| 2 | 8 | 6 | about 10,800 | about 16,000 |
| 3 | 10 | 9 | about 14,600 | about 22,000 |
| 4 | 12 | 12 | about 18,500 | about 28,000 |

At an average item value of about 450, a four-player normal level needs roughly 41 items. Check that the rooms can physically hold that much.

- A 12-minute normal run still needs only 3 doses per player, so the design has margin.
- A 14-minute run needs 4. Keep the target at 10 to 12 minutes.
- **Re-measure E in playtests** (average value actually delivered ÷ value spawned) and adjust the budget, not the dose rules.
- Three-player runs get their own playtest, since they expose scaling problems that solo and four-player runs hide.

### 4.4 Doctors (lethal)

- Zombified doctors patrol, investigate noise, chase and attack. Reuses the existing enemy brain.
- They hear footsteps, impacts and drops. They **ignore music** so that the radio is a real option and not suicide.
- **Flee** when an apple bursts nearby (4.7). After fleeing a doctor goes back to patrol, not straight to chase.
- **Hit model:** a doctor hit deals heavy damage (about 60 of 100 health), knocks the player back and gives 2 seconds of protection. One mistake is a scare, two is death.
- **Safe rooms:** the start wing and the pharmacy wing. Doctors and ground doctors never enter. Standing in a safe room restores health slowly (about 20 seconds to full).
- Doctors cannot be killed in version 1. Players manage them with sound, doors, apples and hiding.

### 4.5 Radio (music)

The radio is an alternative survival strategy, not a replacement for medicine.

- A radio is a physical prop. Hold it and press the use key (E) to toggle it. Place it down to make a calm zone.
- **Relief:** 0.6 Pressure per second at full strength, falling with distance. Net of the baseline that is +0.35 per second while it plays.
- **Battery:** 40 seconds of playback per radio, not rechargeable in version 1. One radio is worth about 0.6 of a dose per player in range, and about 2.4 doses for a full team.
- **No stacking:** only the strongest radio in range counts.
- **Placement:** about one radio per two players per level, in the wards, not in safe rooms.
- It emits `Music` noise, which doctors ignore. **Nuisance enemies** (4.6) react to it, and the longer it plays, the more they come.
- Choosing to stand still for 40 seconds in the open is the price.

### 4.6 Nuisance Enemies (non-lethal, drawn by music)

- **Spiders:** crawl onto the camera and partly block vision. Swat them away (key F). Hard limits: never more than about 40 percent of the screen, never for more than about 8 seconds.
- **Ground doctor:** a dead, frozen doctor on the floor. Music near it wakes it after about 1.5 seconds. A **rattle sound** plays during that time. It crawls to the player and grabs them by the waist, slowing movement to about 40 percent (the player can still move). Ways out: mash jump (about 3 seconds of effort), a thrown apple (instant), a teammate's pull (co-op), or a timeout. Then it goes dormant for a cooldown.
- **Struggling is noisy** (small radius). A grab near a patrol route is a gamble, but never an instant death.

### 4.7 Apples

- A limited emergency tool, not an infinite panic button.
- **Supply:** about one per player per level, in kitchens and fruit bowls. They are physical props (grab and throw). No inventory in version 1.
- **Single use.** It shatters on its first impact within a few seconds of the throw.
- **Effect:** doctors within about 9 metres flee for about 6 seconds, even if the apple misses. Then they return to patrol, not to chasing you.
- **Cost:** +10 Pressure to the thrower, and an impact noise (about 12 metres, just beyond the 9 metre flee radius, so a doctor at the edge investigates instead of fleeing), so it is never free.
- Thrown at a ground doctor it breaks the hold at once.
- **High difficulty only:** apples rot over about 90 seconds and lose their effect.

### 4.8 Scares and Hallucinations

- A fixed set of scripted scares per level, placed deterministically from the seed. Each fires at most once per run.
- Global minimum gap of about 45 seconds between scares.
- No scripted scares in safe rooms, and none during the swell warning.
- Pressure-driven hallucinations start at 50 and ramp up to 100.
- **Hallucinations are harmless and readable:**
  - Fake doctors have no collision and cannot hurt the player.
  - They vanish if looked at for about 2 seconds or approached within 3 metres.
  - Fake sounds are cosmetic and never create real noise events, so they cannot attract real doctors.
  - A subtle tell (audio filtering, a faint colour fringe) marks them. Never explained in a tutorial. Players learn it by playing.

### 4.9 Threat Director (fairness rules)

A small system that keeps fear from turning into frustration.

1. **Telegraph first.** Every lethal or hard-control effect gives at least 1.5 seconds of sound and visual warning.
2. **Two-hazard cap.** At most two hazards are active on one player at once (chasing doctor, grab, spider swarm, scripted scare). Others wait.
3. **No pile-ons.** Ground doctors do not wake and spiders do not arrive while a doctor is chasing that player or is within about 15 metres.
4. **Last chance.** The swell warning always allows a dose.
5. **Explainable deaths.** The results screen states the cause (burst, doctor, and so on).
6. **A loss still pays something** (rate to be tuned; the starting idea is 25 percent of committed value as money).

### 4.10 Extraction and Exit

- The exit is the front door of the start wing.
- **Quota unlocks the exit.** The first time banked doses reach the quota, the exit opens (an alarm bell rings). It **stays unlocked** even if doses are taken afterwards. Before unlocking, taking a dose delays the team. After unlocking, it only reduces the bonus.
- **Greed decision:** leave now, or go back out for more value. Each surplus dose banked at exit pays a bonus (rate tuned in the meta-progression phase).
- **Success:** quota unlocked and all living players inside the exit zone.
- **Failure:** all players dead. In solo, dying ends the run.

### 4.11 Co-op (Phase 8)

- **Downed state:** a player at zero health is downed, not dead, in co-op.
- **Defibrillator:** revives a downed teammate. On a standing player it deals damage (friendly fire), so it needs careful aim.
- **Rescue dose:** a teammate can inject a swelling player.
- Physics-heavy co-op is not bolted on later. Grab ownership, object sync, enemy authority and the medicine ledger need a networking model, which the Phase 4.5 spike decides. The request and grant events already keep dose logic host-friendly.

### 4.12 Difficulty

| Setting | Normal | High |
|---|---|---|
| Baseline Pressure rise | 0.25 per second | 0.3125 per second |
| Value per dose | 500 | 600 |
| Doctors | base | about 1.4 times |
| Apples | do not rot | rot |
| Loot budget | by the formula in 4.3 | by the formula, with higher D, v and lower E |

High is harder mainly because there is more to haul and less margin, not because of a new rule. The formula keeps every difficulty winnable.

---

## 5. Run Arc (target 10 to 12 minutes)

| Time | What happens |
|---|---|
| 0:00 to 1:30 | Calm. Leave the safe wing, find the first valuables, learn the carry. No pressure events |
| about 1:30 | First scripted scare, in a ward |
| 2:00 to 3:30 | First doctor encounter. First delivery to the compounder |
| about 3:20 | Pressure reaches 50. The first dose is due and already banked |
| 4:00 to 7:00 | Wider exploration. Radios, apples, nuisance enemies. Heavier loot in the far wing |
| 7:00 to 9:00 | Quota reached, exit unlocks |
| 9:00 to 12:00 | Greed decision, then the exit |

---

## 6. Build Layers (scope control)

The design is large. Build it in layers, and do not move on until the previous layer is fun.

**Layer 1: first playable**
- Physics grab, carry and throw
- Loot value loss
- Compounder, committed value, doses and quota
- Pressure with dosing and the burst
- One lethal doctor (patrol, investigate, chase) with the hit model
- Apples
- One map, the safe start wing and the exit

**Layer 2: expansion** (after the Layer 1 gate)
- Radio, ground doctor, spiders
- Hallucinations, scripted scares, the Threat Director
- High difficulty

**Layer 3: multiplayer and polish**
- Networked physics, downed state, defibrillator, friendly fire
- More maps and enemies, audio and art polish

The code for Layer 2 already exists. It stays out of the scenes until Layer 1 passes its gate.

### Layer 1 playtest gate

Test in a real scene, with people who are not the designer.

| Question | Pass condition |
|---|---|
| Is carrying fragile loot fun? | Testers keep playing past 5 minutes without prompting; handling loses roughly 10 to 30 percent of value (less means no tension, more means tedium) |
| Is medicine pressure fair? | Solo normal runs by non-experts end with 0 to 2 spare doses in at least 70 percent of attempts |
| Are doctor encounters tense but fair? | In at least 80 percent of deaths, the player can say what they did wrong |
| Is dosing a decision? | At least half of testers hesitate before a dose at least once |
| Is the exit tempting? | At least 40 percent of runs go back for more after the quota unlocks |

---

## 7. Mapping to the Roadmap

| Roadmap phase | Covered by this design |
|---|---|
| 3 Extraction loop | Compounder, committed value, dose ledger, quota in doses, exit unlock |
| 4 Enemy and noise | Pressure and dosing, doctors, hit model, Music noise, Flee, apples, safe rooms |
| 5 Procedural levels | Budget formula, start-wing counter, vault-weighted loot |
| 6 Meta progression | Nuisance enemies, bonus for surplus doses, failure payout |
| 7 Polish | Scares, hallucinations, Threat Director, screen effects, audio |
| 8 Multiplayer | Downed state, defibrillator, friendly fire, rescue dose |

## 8. Code Impact

Already in code (Unity-side parts still partly untested, see `STATUS.md` and `CODE_STATUS.md`): `InsanityModel`, `PlayerInsanity`, `MedicineLedger`, `Compounder`, `RunTracker`, `Music` noise and per-source hearing, Flee state, `Apple`, `MusicPlayer`, `GroundDoctor`, `SpiderNest`, `JumpscareScheduler`, `CrawlerSwarmModel`, `SpeedModifiers`, `DifficultyProfile`.

**Version 2 changes:**

| # | Change | State |
|---|---|---|
| 1 | Commit accepted loot: `ExtractionZone` consumes valuables, `MedicineLedger.Commit` only ever adds | **Done** |
| 2 | Latch the exit unlock (`ExitUnlocked`), plus `ExitZone` to finish the run | **Done** |
| 3 | Dose gate: minimum pressure and cooldown, swell exception (`DoseGate`, `DoseRefused` event) | **Done** |
| 4 | Defaults: baseline 0.25 per second, swell 5 seconds, event pressure from grabs and scares | **Done** |
| 5 | Radio: battery (`BatteryModel`), 0.6 per second, strongest source only (`MusicReliefMixer`) | **Done** |
| 6 | Quota-first budgeting by player count (`EconomyPlanner`, `LevelDefinition`, `LevelBuilder.Build(seed, players)`) | **Done** |
| 7 | Counter and exit in the start wing (`CounterInStartRoom`, on by default) | **Partly done.** Loot is not yet weighted toward the far wing |
| 8 | Apples: impact noise; flee ends in idle, then patrol | **Done** |
| 9 | Doctor hit model, safe rooms, health regeneration | Not started |
| 10 | Threat Director (hazard counting, no pile-ons) | Not started |
| 11 | Hallucination rules | Not started (Phase 7) |

The pure logic for items 1 to 6 and 8 is unit tested (159 tests, run outside Unity). The Unity-side scripts have not been compiled or played yet.

## 9. Decisions and Open Questions

| Question | Decision |
|---|---|
| Delivered medicine is both team quota and shared dose pool? | Yes, with the gates and visibility in 4.1 |
| What is the compounder? | A physical machine |
| Can lethal doctors be killed? | No in version 1 |
| How does a level end? | Quota unlocks the exit; the player chooses when to leave |
| Visual style | Stylised low-poly (proposed) |
| UI name for insanity | Pressure (proposed) |

Still open:
1. Failure payout and the surplus-dose bonus rates.
2. Final title.
3. How many radios and apples per level once rooms exist.
4. Whether doses should ever need a trip back to the counter on high difficulty.

## 10. Tone Guidelines

- Doctors are the threat. Patients are never monsters, never loot and never mocked.
- Keep comedy in the mechanics and animation (balloon death, apple logic, the compounder's personality), not in anyone's condition.
- The Haze is a supernatural contamination, not a metaphor for mental illness. Doses are a stabiliser, not psychiatric treatment.
- Prefer neutral UI wording: Pressure and doses.
