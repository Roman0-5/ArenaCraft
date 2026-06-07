# Project Overview 
    - Game Title: ArenaCraft
    - High-Level Concept: 2.5D local multiplayer Battle Royale with Resource Harvesting and Shopping phases.
    - Players: 2 Players (Local)
    - Inspiration / Reference Games: Hades, League of Legends
    - Tone / Art Direction: Stylized Low-Poly, Warm Earth Tones
    - Target Platform: PC
    - Screen Orientation / Resolution: Landscape 1920x1080
    - Render Pipeline: URP
# Game Mechanics 
## Core Gameplay Loop
1. Resource Phase: Harvest nodes (Wood, Stone, Metal) to earn Gold.
2. Shopping Phase: Spend Gold on Weapons and Armor upgrades.
3. Battle Royale Phase: Melee combat until one survivor remains.
## Controls and Input Methods
- Player 1: WASD (Move), Space (Attack), F (Interact)
- Player 2: Arrow Keys (Move), Enter (Attack), Right Shift (Interact)
# UI
- Main Menu: High-impact entry screen.
- HUD: Player panels (HP, Resource, Gold, Icons) + Central Timer.
- Shop: Grid-based upgrade menu.
- Victory: Final winner announcement.
# Key Asset & Context
- Scripts: `GamePhaseManager`, `HUDController`, `ShopController`, `ResourceNode`.
- Icons: `Icon_BasicSword`, `Icon_AdvancedSword`, `Icon_LightArmor`, `Icon_HeavyArmor`.
# Implementation Steps
- **Step 1: Fix HUD and Shop UI layout and formatting (USS)**
  - Description: Rewrite `PlayerHUD.uss` and `Shop.uss` to remove invalid properties (`box-shadow`, `linear-gradient`) and improve spacing/flexibility.
  - Assigned role: developer
  - Dependencies: None
  - Parallelizable: Yes
- **Step 2: Assign all missing icons to HUD and Shop**
  - Description: Use `RunCommand` to wire generated icons to `HUDController` and `ShopController` properties.
  - Assigned role: developer
  - Dependencies: None
  - Parallelizable: Yes
- **Step 3: Add visual icons to Shop UXML**
  - Description: Update `Shop.uxml` to include icon containers for each item.
  - Assigned role: developer
  - Dependencies: Step 1
  - Parallelizable: No
- **Step 4: Final verification of gameplay loop**
  - Description: Playtest from MainMenu to victory.
  - Assigned role: explorer
  - Dependencies: All steps
  - Parallelizable: No
# Verification & Testing
- Run the game from MainMenu.
- Verify HUD doesn't overlap at different resolutions (using flex-grow/shrink).
- Verify icons appear in HUD after purchase.
- Verify Shop shows item icons.
