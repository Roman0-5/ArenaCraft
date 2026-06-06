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

- `Input/ArenaControls.inputactions` — **one** asset, **two** action maps `PlayerOne` / `PlayerTwo`,
  each with `Move` (Vector2), `Attack` (Button), `Interact` (Button).
  P1 = WASD / Space / F, P2 = Arrows / Enter / Right Shift (GDD 5.2).
- `Input/ControlsBootstrap.cs` — enables both maps at startup, loads rebind overrides from PlayerPrefs.
- `Player/PlayerController.cs` — `Rigidbody`-based X/Z movement, constant speed, facing via
  `Quaternion.LookRotation`/`Slerp`, drives Animator (`Speed` float).
- `Combat/Health.cs` — base 100 HP, armor raises max HP, `TakeDamage`, death/elimination, events.
- `Combat/MeleeAttack.cs` — on Attack: swing cooldown, brief hitbox window, `Attack` anim trigger, SFX.
- `Combat/AttackHitbox.cs` — child trigger collider; damages opposing `Health`; per-swing hit dedup.
- `Combat/Weapon.cs` — ScriptableObject (`damage`, `swingCooldown`); default "Fists"; stub for Paket 3 shop.
- `Combat/ArmorType.cs` — enum None/Light/Heavy (+0/+25/+50 max HP).
- `UI/RebindActionUI.cs` + `UI/KeybindingMenuController.cs` — full runtime rebind menu (PlayerPrefs).
- `Audio/CombatAudio.cs` — swing/hit SFX, triggered by combat scripts.

### Input architecture (2 players, one keyboard)
Two action maps in one asset, both enabled simultaneously (different keys → no conflict). This is
more robust than `PlayerInput`/control schemes for a single keyboard device. Each `PlayerController`
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
