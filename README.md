# ArenaCraft

A 2.5D **local two-player Battle Royale** built in Unity, implementing the
[ArenaCraft Game Design Document v2.0](#). Two gladiators share one keyboard and
fight across three phases — **Resource → Shopping → Battle Royale** — until one
survivor remains.

> **Status:** Full MVP. The entire game is generated **at runtime from code**, so
> the project runs the moment you press Play — no manual scene wiring, prefab
> hookups or asset imports required. Real low-poly art (GDD §7) can be dropped in
> later by swapping the meshes/materials on the generated objects.

---

## Running the game

1. Open the project in **Unity 6 (6000.3.9f1)** or newer.
2. Open `Assets/Scenes/SampleScene.unity` (it contains the `__ArenaCraft`
   bootstrap object; `MainScene.unity` has it too).
3. Press **Play**. You start on the Main Menu → *Start Match*.

Everything (arena, players, resource nodes, camera, HUD, menus, audio) is built
by the single `GameBootstrap` component on the `__ArenaCraft` object.

## Controls (GDD §5.2)

| Action            | Player 1 (Red) | Player 2 (Blue) |
|-------------------|----------------|-----------------|
| Move              | `W A S D`      | Arrow Keys      |
| Attack / Harvest  | `Space`        | `Enter`         |
| Interact / Shop   | `F`            | `Right Shift`   |

Menus are navigable with `W/S` or `↑/↓` and confirmed with `Space`/`Enter`
(mouse works too). `Esc` returns to the Main Menu.

### In the shop
Use **up/down** to highlight an item, **attack** to buy it, **interact** to mark
yourself *Ready*. The phase ends when both players are ready or the timer runs
out. A *"You didn't buy anything!"* warning appears near the end if you haven't
bought anything.

---

## How the GDD maps to the code

| GDD area | Implementation |
|----------|----------------|
| Three-phase flow (§2.1.1) | `Core/GameManager.cs` state machine |
| Physics / X-Z movement (§2.2.1–2) | `Players/PlayerController.cs` (Rigidbody, frozen Y) |
| Resource nodes (§2.2.3, §4.1) | `World/ResourceNode.cs`, `World/ResourceNodeSpawner.cs` |
| Melee combat (§2.2.5) | `PlayerController.TryAttack` + `Combat/IDamageable.cs` |
| Economy / auto-convert to gold (§2.2.6) | `Players/PlayerStats.cs`, `World/ResourceType.cs` |
| Item shop (§2.2.6) | `Shop/ShopCatalog.cs`, `Shop/ShopItem.cs`, `UI/ShopPanel.cs` |
| Screen flow (§2.2.7) | `UI/UIManager.cs` (Menu/Tutorial/Shop/Victory overlays) |
| Game options — adjustable timer, controls (§2.3) | Options screen in `UI/UIManager.cs` |
| Rematch + win tracker (§2.4) | `GameManager.Rematch`, victory score line |
| Arena / colosseum (§3.2, §4.1) | `World/ArenaBuilder.cs` |
| Characters (§3.3) | `Players/PlayerFactory.cs` |
| Tutorial overlay (§4.2) | Tutorial screen in `UI/UIManager.cs` |
| HUD: HP/resource/gold/items + timer (§5.1) | `UI/PlayerHUD.cs`, `UI/UIManager.cs` |
| 2.5D orthographic camera (§1.5, §5) | `Core/ArenaCamera.cs` |
| Audio: SFX + 2 music tracks (§5.3) | `Audio/AudioManager.cs` (fully procedural) |

### Requirements coverage

**Must (FMR1–10, NFR1):** all implemented.
**Should (FSR1–4):** 3 resource types + rare relic, shared phase timer,
attack/collect/transition SFX, HP bar turns red below 30%.
**Could:** `FCR2` extra weapons (dagger, spear), `FCR4` cosmetic hook
(`PlayerFactory.AddCosmetic`), `FCR5` session win-count tracker, `FCR6` hidden
rare relic node. `FCR1` (gamepad/4P) and `FCR3` (split-screen) are left as
documented post-MVP extension points.

---

## Resolved open issues (GDD Appendix C)

- **C.1 Gold values:** Wood 1g, Stone 2g, Metal 5g (relic 15g). See
  `ResourceTypeDef.DefaultSet()`.
- **C.2 Respawn:** nodes do **not** respawn (`GameSettings.nodesRespawn = false`),
  giving a finite, contested resource economy. Timer-based respawn is supported
  by flipping the flag.
- **C.3 Attack cadence:** fists 0.50s, dagger 0.30s, basic sword 0.55s,
  advanced sword 0.60s, spear 0.70s. See `ShopCatalog`/`GameSettings`.
- **C.4 Animations:** placeholder primitive "swing" scaling is used; Mixamo clips
  can be added on the generated player object's Animator later.

See `docs/DESIGN.md` for the Appendix B analysis diagrams (state machine,
economy flow, component overview).
