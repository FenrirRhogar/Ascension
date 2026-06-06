# Project Summary: "Ascension" (Co-op Action Roguelike)
**Core Concept:** A split-screen, class-based action roguelike. Players spawn on a procedurally generated mountain, fight through scaling waves of enemies using unique elemental/melee combos, and must find a Level Gate to ascend to the next, harder stage.

### 1. Systems Implemented

**A. Procedural Generation & Environment**
*   **Procedural Mountain Terrain**: `ProceduralTerrain.cs` generates high-resolution, infinitely random mountains using multi-octave Perlin noise.
*   **Procedural Populator**: `ProceduralGenerator.cs` randomly scatters Nature Props (trees, rocks), Enemies (NPCs), Consumables, and the all-important Level Gate across the terrain, using raycasts to ensure they rest accurately on uneven slopes.
*   **Universal Material Fixer**: `MaterialFixer.cs` automatically detects legacy materials or broken URP assets upon spawning. It intelligently separates "Solid" meshes from "Particle/VFX" meshes, preserving transparency, glow, and additive blending for magic spells.

**B. The Class System (`CharacterClassSO`)**
*   A highly modular ScriptableObject architecture where every class inherits basic stats (Health, Speed, Mana Regen) but executes unique abilities.
*   **Customizable VFX**: Every class has an exposed `VFX Height Offset` to perfectly align visual effects with the character model's chest/eye level.
*   **Classes Built:**
    *   **Fighter**: Uses basic melee swings, a heavy cleave, and a channeled "Beyblade" Whirlwind ability that spins the physical character model at 1500 deg/sec while dealing AoE damage.
    *   **Wizard**: Casts Fireball projectiles, channels a continuous high-precision Lightning Cone that perfectly tracks the player's aim, and calls down a massive Meteor Storm ultimate.
    *   **Assassin**: Features high-mobility dashes and a melee strike that dynamically pivots up and down to match the player's camera pitch for accurate fighting on mountain slopes.
    *   **Cleric**: Features AoE healing bursts and a massive "Holy Sanctuary" ultimate that damages enemies while fully healing players.
*   **Combat Range Scaling**: Factored in a new global melee range bonus (`meleeRangeBonus`) to Fighter, Cleric, and Assassin classes to dynamically scale their melee attack ranges.

**C. Combat & Controls**
*   **Channeled Abilities**: Robust logic in `CombatSystem.cs` and `ResourceSystem.cs` handles "Hold" abilities. It accurately tracks input press/releases, drains mana per frame, pauses passive mana regeneration while channeling, and instantly terminates abilities when mana depletes or the button is released.
*   **Aim Tracking**: Melee hitboxes and Magic VFX are un-parented from the base character rotation and dynamically use the Camera's forward vector. Visuals and damage hitboxes pivot flawlessly with the crosshair.
*   **Gamepad & KBM Support**: Fully configured Unity Input System. Includes a custom **"Hold to Look Behind"** tactical camera toggle (with an inverted Y-axis) for checking rear flanks.
*   **VFX Cleanup**: All transient visual effects (slashes, smoke, meteor impacts) have automatic destruction timers to prevent memory leaks and hierarchy clutter during long runs.

**D. UI & Game Loop**
*   **High-Visibility Health Bars**: Enemies feature world-space UI health bars with a pitch-black background, vibrant red fill, and a crisp white outline for maximum readability.
*   **Comic-Style Hit Text**: Enemies spawn randomized floating text prefabs (e.g., "POW!", "WOW!") when taking damage.
*   **The "Ascension" Loop**: `LevelManager.cs` tracks `CurrentLevel`. Touching the procedural Level Gate reloads the scene.
*   **Scaling Difficulty**: With every level advanced, the generator spawns +3 more enemies. Enemies gain +50% Max Health, +25% Damage, and +15% Speed per level.
*   **Dynamic Level Banner**: A code-generated Screen Space UI flashes a giant golden "LEVEL X" text on screen for 5 seconds at the start of every stage (resolved obsolete TMPro `enableWordWrapping` warning by switching to `textWrappingMode = TextWrappingModes.NoWrap`).

**E. Artifact & Potion Systems (Run Progression)**
*   **Permanent passive Artifacts**: `ArtifactSO.cs` (configured via Inspector) grants run-wide permanent buffs: Speed, Max Health, Damage Multiplier, Mana Regen, Stamina Regen, and Melee Range. Bypasses inventory slots, applies immediately on pickup, and dynamically lists on the HUD above hotbar slots with stacked quantity tracking (e.g., "x2"). Managed by `ArtifactManager.cs`.
*   **Customizable Potions**: Refactored the legacy subclass files into a single, generic `PotionSO.cs` allowing configuration of instant restores (Health, Mana, Ult Charge), temporary stat buffs (Movement Speed, Infinite Stamina, Infinite Mana), and temporary regeneration boosts (Health/sec, Mana/sec, Stamina/sec). Potions are placed into 1-2-3 hotbar slots and consumed via hotkeys.
*   **Rarity and Drops**: Implemented weighted random drop generation globally on `LevelManager.cs` supporting configurable ratios and rarity weights (Common: 100, Uncommon: 50, Rare: 15, Legendary: 3) for enemy drops (`Health.cs`) and map scattering (`ProceduralGenerator.cs`).

***

### 2. Roadmap: What Needs to be Implemented Next

To evolve this from a "combat sandbox" into a fully addictive Roguelike, the following systems need to be built:

**Phase 1: Elemental Combos (The "Spellbreak" Mechanics)**
*   **Objective**: Encourage active co-op teamwork.
*   **Implementation**: Add an Elemental Status system to the `Health` component (e.g., Burning, Wet, Shocked). Update class abilities to react to these statuses.
    *   *Example*: If the Cleric makes enemies "Wet", the Wizard's Lightning deals 3x damage and chains to nearby Wet targets. If the Fighter Whirlwinds through a Wizard's Fireball, the Whirlwind catches fire.

**Phase 2: The Boss Arena (The Climax)**
*   **Objective**: Give the "Run" a definitive ending.
*   **Implementation**: If `LevelManager.CurrentLevel == 5`, instead of spawning a procedural mountain, load a specific "Peak Arena" scene. Spawn a massive Boss monster with unique attack patterns. Defeating the boss triggers the Victory screen.

**Phase 3: Metaprogression (Persistent Unlocks)**
*   **Objective**: Give players a reason to play *another* run after winning or dying.
*   **Implementation**: Create a Save System. Beating bosses earns "Mountain Coins." Players can spend these in a Main Menu lobby to unlock new Character Classes, new Starting Weapons, or new potential Relics that can spawn in future runs.