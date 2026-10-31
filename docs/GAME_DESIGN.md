# Game Design

Working title: TBD. Theme: a haunted mental hospital. Solo-first, co-op ready.
Status: first design pass, 2026-10-31. Numbers are starting values to tune in playtests, not final.

---

## 1. Premise

Patients lie in their beds while the hospital's doctors have turned into zombies. You and your team slip through the wards stealing valuables and bring them to the medicine counter, where the compounder turns them into medicine. You need that medicine yourself: without regular doses your insanity climbs until you swell and burst like a balloon.

Tone: horror-comedy. Frightening, never cruel. The doctors are the monsters; the patients are passive victims and are never the threat.

## 2. Core Loop

1. Move through the hospital, carrying valuables with the physics grab system (carrying carefully is still the core skill; impacts still cost value).
2. Deliver valuables to the **medicine counter**. The **compounder** converts delivered value into **medicine doses**.
3. Doses do two jobs: they meet the team's quota and they lower your insanity. Every dose you take is a dose the team does not bank.
4. Survive the doctors, manage noise, keep insanity below 100, and leave once the quota is met.

Assumption to confirm: delivered medicine is both the team quota and the shared pool players draw doses from. This creates the central tension (heal yourself or hit the target).

## 3. Design Pillars

1. **Physics is the gameplay.** Everything is carried, thrown, dropped, broken.
2. **Value is fragile.** Impacts destroy money.
3. **Noise draws danger.** Footsteps and impacts alert doctors.
4. **Every tool has a price.** Music, apples, and the defibrillator each solve one problem and cause another.
5. **Co-op ready, offline first.**

## 4. Systems

### 4.1 Insanity and Medicine
- Insanity runs 0 to 100 and rises passively (start: about 0.4 per second, so roughly four minutes from empty to full with no medicine).
- One dose lowers insanity by about 40.
- Thresholds: 50 and above brings mild hallucinations (false sounds, fake shadows); 75 and above raises jumpscare frequency; at 100 a short swelling warning plays, then the player bursts like a balloon and dies.
- Music (4.4) lowers insanity gradually; apples (4.6) raise it.
- Player-facing name: use "sanity" or a similar neutral word in UI; "insanity" is the internal term.

### 4.2 Loot and the Compounder
- Valuables are placed through the wards, beds and rooms (existing valuable and value-loss systems).
- The **medicine counter** is an extraction zone. Only valuables fully inside and not held count (existing rule).
- The **compounder** converts delivered value into doses at a fixed rate (start: 500 value per dose). Open question: NPC, machine, or a hostile-looking friendly NPC.
- Value lost to damage after delivery still drains the total, as in the base design.

### 4.3 Doctors (lethal)
- Zombified doctors patrol, investigate noise, chase and attack. Reuses the existing enemy brain.
- They hear footsteps, impacts, and drops. They **ignore music** (see 4.4) so that music is a real option and not suicide.
- New behaviour: **Flee** when hit by or near a thrown apple (4.6).
- Chasing doctors cause the multiple jumpscares in the original sketch; scripted scares are listed in 4.8.

### 4.4 Music
- Playing music (a radio or similar prop) lowers insanity gradually while it plays.
- It produces a separate noise type, `Music`, which doctors ignore but **nuisance enemies** (4.5) react to.
- Loudness and duration scale both the benefit and the risk, so the player decides how much to gamble.

### 4.5 Nuisance Enemies (non-lethal, drawn by music)
- **Spiders:** crawl onto the camera and partly block vision for a few seconds. Swat them away. Hard cap: never block more than about 40 percent of the screen, and never for more than about 8 seconds.
- **Ground doctor:** a dead, frozen doctor lying on the floor. Music wakes it. It crawls to the player and holds them by the waist, slowing movement to about 40 percent. Ways to break free: mash a button, a teammate pulls it off, or a thrown apple. It returns to dormant after a cooldown.
- **Random scares:** see 4.8.
- Danger by combination: a ground doctor holding you while a chasing doctor closes in is intended to be deadly without any extra scripting.

### 4.6 Apples
- Thrown apples make nearby doctors run away for a few seconds ("an apple a day keeps the doctor away").
- Each throw raises insanity (start: about 10).
- Apples are physical objects in the world in version 1 (grab and throw like any prop). A pocket or hotbar is a later decision, since it needs inventory UI.
- On **high difficulty only**, apples visibly rot over time and lose their effect.

### 4.7 Co-op (Phase 8)
- **Downed state:** a player at zero health is downed, not dead, in co-op.
- **Defibrillator:** revives a downed teammate. Used on a standing player it deals damage (friendly fire), so it must be aimed with care.
- Dying alone ends the run in solo.

### 4.8 Jumpscares and Hallucinations
- A fixed set of scripted scares per level, placed deterministically from the level seed.
- Insanity-driven hallucinations on top (fake doctors, wrong sounds, flickering lights).
- Global minimum gap of about 45 seconds between scares; each unique scare fires at most once per run.

### 4.9 Difficulty
- Higher difficulty: faster insanity rise, more doctors, apples rot, fewer doses per value.

## 5. Mapping to the Roadmap

| Roadmap phase | Added by this design |
|---|---|
| 3 Extraction loop | Medicine counter and compounder (extension of the extraction zone); quota expressed in doses |
| 4 Enemy and noise | Insanity model and dosing, balloon death, `Music` noise type, doctors ignoring music, Flee state, apples |
| 6 Meta progression | Nuisance enemies: ground doctor and spiders; second valuable family |
| 7 Polish | Jumpscare set, hallucinations, screen crawl effects, audio |
| 8 Multiplayer | Downed state, defibrillator, friendly fire |

## 6. Code Impact

Mostly reuses what exists (noise events, enemy brain, extraction zone, run ledger, state machine). New or changed:

- `InsanityModel` (pure class, unit tested, same style as `StaminaModel`) and a `PlayerInsanity` component.
- `NoiseSource.Music` plus a per-source hearing multiplier on `EnemyDefinition`.
- `EnemyBrain`: add a Flee state; a separate dormant-grabber enemy (Dormant, Waking, Grabbing, Cooldown).
- A movement-slow hook on the player motor (an `IMovementModifier` list; carry slowdown becomes one entry).
- `MedicineCounter` extending the extraction zone, and a dose pool on the run ledger.
- Screen-space crawler effect in the UI layer.

## 7. Open Questions

1. Is delivered medicine both the team quota and the shared dose pool? (Assumed yes.)
2. What is the compounder: NPC, machine, or something stranger?
3. Can lethal doctors be hurt or killed, or only avoided?
4. Does a level end at a fixed exit or when the quota is met?
5. Final title and visual style (stylised low-poly is the current plan).

## 8. Tone Guidelines

- Doctors are the threat; patients are never monsters and are not mocked.
- Keep comedy in the mechanics and animation (balloon death, apple logic), not in the patients' condition.
- Prefer neutral UI wording for the insanity stat.
