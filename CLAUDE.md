# CLAUDE.md — ArenaCraft

Guidance for Claude Code when working in the **ArenaCraft** Unity project.
(Inherits the conventions in `C:\Users\mi301\CLAUDE.md`; this file adds project-specific context.)

## Overview

ArenaCraft is a **local 2-player Battle Royale** (shared keyboard, shared screen) built in
**Unity 6 (6000.3.10f1), URP 17.3.0, Input System 1.18.0, UGUI 2.0.0**. A match has three
phases (Resource → Shopping → Battle Royale). The full design lives in `../ArenaCraft_GDD.pdf`.

### IMPORTANT: 3D instead of 2.5D
The GDD describes 2.5D (3D assets + isometric/orthographic camera, movement locked to X/Z).
**This implementation uses full 3D** (free perspective + free facing). Movement still happens on
the **X/Z plane** (no player-controlled vertical movement), but visuals and camera are full 3D.

## Current scope: Paket 2 only

We build **only Paket 2 — Player, Movement, Combat & Animation**. Packages 1 (framework/arena/
flow) and 3 (resources/economy/shop/HUD) are out of scope and will be built later by the team.

Paket 2 deliverables:
- PlayerController: full 3D ground movement on X/Z, facing = movement direction
- 2-player input via the new Input System + custom keybindings (full rebind menu)
- Melee: trigger hitbox in front of the character, damage, swing cooldown
- HP & Armor: base 100 HP, Light/Heavy armor raise max HP, death/elimination
- Apply weapon damage (weapon stat is owned by Paket 3 later; we keep a stub interface)
- 2 gladiator variants (red/blue accent) + Idle/Walk/Attack animations (Mixamo)
- Combat/hit SFX

## Working division (agreed with the user)
- **Claude** writes all C# scripts + the input asset, and gives exact Editor step-by-step guides.
- **The user** imports FBX/animations, builds prefabs, places objects in the scene, and wires
  Inspector references. Scenes (`.unity`) and prefabs (`.prefab`) are **not** edited as YAML
  (GUID/fileID corruption risk).

## Architecture (Paket 2)

All gameplay code lives under `Assets/Scripts/`:

- `Input/ArenaControls.inputactions` — **one** asset, **two** maps `PlayerOne` / `PlayerTwo`,
  each with `Move` (Vector2), `Attack`, `Interact`, `Dash`, `Block` (Buttons).
- `Input/PlayerInputProvider.cs` — resolves a player's map (asset + `PlayerSlot` enum) and exposes
  Move/Attack/Interact/Dash/Block. Movement **and** combat read input through this (one wiring point).
- `Input/ControlsBootstrap.cs` — loads rebind overrides from PlayerPrefs at startup.
- `Player/ArenaPlayerController.cs` — `Rigidbody` X/Z movement, facing via `LookRotation`/`Slerp`,
  drives Animator `Speed`; **Dash** (forward burst + cooldown). Y position + X/Z tilt locked, spin
  zeroed (clean collisions). *(Renamed from `PlayerController` to avoid a clash with the Input
  System sample's `PlayerController`, which hid it from Add Component.)*
- `Combat/Health.cs` — 100 base HP, armor raises max HP (set by Paket 3), `TakeDamage`/`Die` + events;
  consults `ShieldBlock` to negate blocked hits. *(Temporary `Debug.Log` for HP/death until HUD.)*
- `Combat/MeleeAttack.cs` — Attack input (polled live), swing cooldown, hitbox window, anim trigger,
  SFX; **2-hit combo** (alternates `Attack`/`Attack2`, 2nd hit ×1.5 dmg + longer finisher cooldown);
  `EquipWeapon()` shop hook; drives weapon-model visibility via `Equipment`.
- `Combat/AttackHitbox.cs` — child trigger collider; damages opposing `Health`; per-swing dedup.
- `Combat/Weapon.cs` — ScriptableObject: `damage`, `swingCooldown`, `displayName`, `showsWeaponModel`.
- `Combat/ArmorType.cs` — enum None/Light/Heavy (+0/+25/+50 max HP).
- `Combat/Equipment.cs` — shows/hides the weapon (`Sword`) & shield (`Shield`) models; hidden at start
  (begins unarmed). Controlled by `MeleeAttack` (weapon) and `ShieldBlock` (shield).
- `Combat/ShieldBlock.cs` — hold **Block** to negate incoming hits; durability (`maxBlocks`=10), shield
  **breaks** at zero; `EquipShield()` shop hook; drives Animator `Blocking` bool + shield visibility.
- `UI/RebindActionUI.cs` + `UI/KeybindingMenuController.cs` — rebind-row logic + save/load/reset.
- `UI/RebindMenuBuilder.cs` — builds the whole rebind-menu UI at runtime; **gear button (top-left)**
  toggles it (also Esc). Add to one empty GameObject + assign `ArenaControls`.
- `Audio/CombatAudio.cs` — swing/hit SFX (needs clips).
- `Assets/Weapons/` — `Fists` (10 dmg), `BasicSword` (20), `AdvancedSword` (35) Weapon assets.

### Input architecture (2 players, one keyboard)
Two action maps in one asset, both enabled simultaneously (different keys → no conflict). This is
more robust than `PlayerInput`/control schemes for a single keyboard device. Each `PlayerInputProvider`
gets the asset + a `PlayerSlot` enum in the Inspector and resolves its map via `FindActionMap`.
This deliberately deviates from the inherited `InputSystem.actions.FindAction(...)` convention,
which is single-player only. Rebinding uses `SaveBindingOverridesAsJson` /
`LoadBindingOverridesFromJson` persisted to PlayerPrefs.

## Key GDD values
- Base HP 100; Light Armor +25 (→125), Heavy Armor +50 (→150).
- Combat: brief trigger collider in front of player; damage = weapon Damage stat.
- Bindings: P1 WASD/Space/F, P2 Arrows/Enter/RightShift.

## Open GDD issues resolved here
- **C.3 swing cooldown** — not specified in GDD; default ~0.5 s, serialized/tunable in Inspector.
- **C.4 animation source** — Mixamo (Humanoid rig, **Apply Root Motion OFF** so the script drives
  position). Free with an Adobe account.

## Notes
- Unity 6 API: use `Rigidbody.linearVelocity` (not the obsolete `velocity`).
- The default `Assets/InputSystem_Actions.inputactions` is the Unity template — left as reference,
  not used for gameplay. ArenaCraft uses `Assets/Scripts/Input/ArenaControls.inputactions`.
- Scenes are edited in the Editor. Tests via Window > General > Test Runner.
- Claude CAN hand-author `.inputactions`, `.controller` and `.mat`/`.asset` files (self-contained,
  GUID-referenced). Scenes/prefabs stay Editor-only.

---

# Session-Stand (zuletzt bearbeitet) — wo wir aufgehört haben

## Charakter
- Asset **RPGHero** (`Assets/RPGHero/`), Charakter-Prefab **`RPGHeroHP`** (Sword in `hand_r`,
  Shield in `hand_l`). Animator-Controller: **`Assets/RPGHero/Animator/ArenaGladiatorAC.controller`**
  (von Claude gebaut), **nicht** `SwordAndShield`.
- Materialien sind URP-konvertiert (Farbe kommt aus **Emission**). `HandPainted 1` = **rot** (P1),
  `HandPainted_Blue` = **blau** (P2). `HandPainted` (Original) unverändert.

## Animator `ArenaGladiatorAC` — States & Parameter
- Parameter: `Speed` (Float), `Attack` (Trigger), `Attack2` (Trigger), `Die` (Trigger),
  `Blocking` (Bool).
- States: `Idle` (Default), `Walk`, `Attack` (NormalAttack01), `Attack2` (NormalAttack02),
  `Die` (Die_SwordShield), **`Block`** (Motion noch LEER — siehe „Offen").
- Clips loopen: Idle/Walk an, Attack/Die aus.

## Steuerung (beide im Rebind-Menü änderbar)
| Aktion | Player 1 | Player 2 |
|---|---|---|
| Bewegen | WASD | Pfeiltasten |
| Angriff | Space | Enter |
| Interact | F | Rechte Umschalt |
| **Dash** | Linke Umschalt | Rechte Strg |
| **Block** | Linke Strg | Punkt `.` |

## Was heute fertig wurde ✅
- Bewegung + Facing (Rigidbody, X/Z), saubere Kollision (Y-Pos gesperrt, kein Drehen/Schleudern).
- Animationen: Idle/Walk/Attack laufen; **Death-Animation** (Die-State, `Health.deathTrigger="Die"`).
- **2-Hit-Combo**: abwechselnd Attack/Attack2, 2. Hit ×1.5 Schaden + längere Erholung (`comboFinisherCooldown`).
- **Dash** nach vorn (Speed/Duration/Cooldown im `ArenaPlayerController`).
- **HP/Armor**: 100 Basis; Armor-HP-Logik vorhanden (Werte setzt Paket 3 per `ApplyArmor`).
- **Aktiver Schild-Block**: halten = Treffer negiert; 10 Blocks → Schild zerbricht; `EquipShield()`-Hook.
  Schild ist EIGENES Item (kein HP) — Rüstung gibt HP (Paket 3).
- **Equipment**: Schwert/Schild am Start unsichtbar (Fäuste); erscheinen bei Ausrüstung/Kauf.
- **Waffen-System**: `Weapon`-Assets (Fists/Basic/Advanced), `EquipWeapon()`-Hook; Schaden skaliert.
- **Rebind-Menü**: baut sich per `RebindMenuBuilder` selbst; Zahnrad oben links toggelt (auch Esc);
  live-Speichern in PlayerPrefs; „Reset all".
- Git: zwei zu große SRP-Zips aus History entfernt + `Assets/RPGHero/SRP/` in `.gitignore`; gepusht.

## OFFEN / als Nächstes ⬜
1. **Block-Animation zuweisen** (NOCH NICHT GEMACHT): FBX `Assets/RPGHero/Animations/Sword And
   Shield Block Idle.fbx` → Rig **Humanoid**, Avatar **Copy From Other Avatar → NewFreeRPGHeroAvatar**,
   Apply; Animation-Tab **Loop Time an**. Dann im Project die FBX aufklappen (►), den Clip auf den
   **`Block`-State** in `ArenaGladiatorAC` ziehen (Feld `Motion`). Block funktioniert schon, nur die
   Pose fehlt bis dahin.
2. **Editor-Wiring sicherstellen** (beide Spieler): Komponenten `ShieldBlock`, `Equipment`
   (Sword→Weapon Object, Shield→Shield Object), `Health.deathTrigger = Die`, Material rot/blau,
   `KeybindMenu`-GameObject mit `RebindMenuBuilder` + ArenaControls.
3. **Kampf-SFX**: Clips importieren (z. B. kenney.nl RPG Audio, CC0) → `AudioSource` + `CombatAudio`
   pro Spieler, `MeleeAttack.combatAudio` zuweisen.

## Dev-Hinweise
- Temporäre `Debug.Log` in `Health` (Schaden/Tod/Block) bis HUD da ist (Paket 3) — können später raus.
- `OnValidate`-Hooks in `MeleeAttack`/`ShieldBlock`: erlauben Live-Test von Waffe/Schild im Inspector.
- Aktuelle Arbeitsszene war zwischenzeitlich `Assets/_Recovery/0.unity` (nach Crash) — als echte
  Szene speichern (`Assets/Scenes/SampleScene.unity`).
