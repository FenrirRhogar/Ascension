# Project Summary: "Ascension" (Co-op Action Roguelike)
**Core Concept:** A split-screen, class-based action roguelike. Players spawn on a procedurally generated mountain (via MapMagic 2), fight through scaling waves of enemies using unique elemental/melee combos, and must find a Level Gate to ascend to the next, harder stage.

### 1. Systems Implemented

**A. MapMagic 2 Integration & Environment**
*   **Infinite Terrain**: The game exclusively uses MapMagic 2 for high-quality, infinite procedural terrain.
*   **Stable Population**: `LevelManager.cs` utilizes a robust `OnEnable`/`OnDisable` lifecycle to catch MapMagic tile generation events without memory leaks.
*   **Dynamic Biomes**: The mountain environment evolves as you level up:
    *   **Levels 1-2 (Forest)**: Lush green valleys and stone cliffs.
    *   **Levels 3-4 (Volcanic)**: Black basalt and dark scorched rocks.
    *   **Levels 5+ (Frozen)**: Thick snow and icy peaks.
*   **High-Altitude Spawning**: Spawning raycasts shoot from 500m up, ensuring players and objects land safely on the highest peaks without clipping.

**B. The Class System (`CharacterClassSO`)**
*   **Classes Built:**
    *   **Fighter**: Features basic swings, heavy cleave, and a channeled "Beyblade" Whirlwind ability.
    *   **Wizard**: Casts Fireball projectiles, channels a precise Lightning Cone (aim-tracking), and calls down a Meteor Storm ultimate.
    *   **Assassin**: Agile melee strikes that pivot vertically to match camera pitch, ensuring accurate combat on slopes.
    *   **Cleric**: AoE healing bursts and a "Holy Sanctuary" damage/heal ultimate.

**C. AI & Combat**
*   **WakeUp AI**: Enemies physically fall to the terrain via gravity when spawned. They "Wake Up" and activate their AI logic the moment they touch the floor.
*   **Diverse Archetypes**: Support for both Melee (Grunt) and Ranged (Artillery) AI scripts, allowing for tactical co-op gameplay.
*   **Scaling Difficulty**: Enemy HP (+50%), Damage (+25%), and Speed (+15%) scale with each level.
*   **Hit Feedback**: High-visibility health bars and randomized "WOW/POW" hit text popups.

**D. UI & Game Loop**
*   **The Ascension Loop**: Finding and touching the procedural Level Gate advances the run and reloads the mountain with a new biome.
*   **Dynamic Level Banner**: Giant golden "LEVEL X" text appears on screen for 5 seconds at the start of every stage.
*   **Optimized HUD**: Tracks health, mana, stamina, and current threat levels.

***

### 2. Roadmap: What Needs to be Implemented Next

**Phase 1: The Relic System (Run Progression)**
*   Create a `RelicManager` for passive, stackable items (e.g., "+10% Move Speed", "Extra Meteor").
*   Add a UI inventory screen for collected run-based upgrades.

**Phase 2: Elemental Combos (Team Synergy)**
*   Add a Status Effect system (Burning, Wet, Shocked) to `Health.cs`.
*   Make abilities interact (e.g., Lightning arcing through Wet targets).

**Phase 3: The Boss Arena (Run Climax)**
*   Load a custom "Peak Arena" at Level 5 with a massive Boss monster.

**Phase 4: Persistence (Persistent Unlocks)**
*   Create a save system and a "Mountain Camp" lobby for unlocking permanent upgrades between runs.
