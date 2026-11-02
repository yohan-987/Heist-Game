# Heist Game (working title)

A solo-developed, physics-driven 3D horror-comedy loot game: grab valuables, carry them without breaking them, and extract before noise attracts something nasty. Offline-first, built to be co-op ready.

- **Engine:** Unity 6 LTS (URP), C#
- **Platform:** Windows PC (Steam later)
- **Plan:** see [`docs/ROADMAP.md`](docs/ROADMAP.md)
- **Design:** see [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md)

## Opening the project

This repo holds the folder structure, assembly definitions, and git config. The Unity-generated files (`ProjectSettings/`, `Packages/manifest.json`) are created by the editor, so do this once:

1. Install Unity Hub and Unity 6 LTS.
2. In Unity Hub, create a new project from the **3D (URP)** template in a temporary location.
3. Copy that project's `ProjectSettings/` and `Packages/` folders into this repo's root.
4. Open this repo's root folder in Unity Hub (Add project from disk).
5. In Project Settings, set Asset Serialization to **Force Text** and Version Control Mode to **Visible Meta Files**.
6. Install the packages listed in the roadmap (section 2.1).

Then commit `ProjectSettings/` and `Packages/` and carry on with Phase 0.

## Git LFS

Run `git lfs install` once per machine before cloning or committing assets.

## Layout

All first-party work lives in `Assets/_Project/`. Third-party packs go in `Assets/ThirdParty/` untouched. Full tree and rules are in the roadmap, section 3.

## Project status

See [STATUS.md](STATUS.md) for what is done, tested and still to do, and [docs/UNITY_SETUP.md](docs/UNITY_SETUP.md) for the editor setup guide.
