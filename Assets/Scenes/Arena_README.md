# Arena.unity — playable Paket 2 scene

A real, self-contained 2-player test arena, hand-built from Unity primitives and wired to the
existing gameplay scripts. It opens and plays without importing anything new. Full 3D (free
perspective + free facing); movement stays on the X/Z plane, per the project's 3D decision.

## What's in the scene
- **Arena_Ground** — 40×40 plane with a MeshCollider (the floor).
- **Main Camera** + **Directional Light** (shadows on).
- **PlayerOne** (slot One) and **PlayerTwo** (slot Two) — capsule bodies, each fully wired:
  `Rigidbody` + `CapsuleCollider`, `PlayerInputProvider`, `ArenaPlayerController`, `Health` (100 HP),
  `ShieldBlock`, `Equipment`, `MeleeAttack`, and a child **AttackHitbox** (box trigger in front).
  All cross-references (MeleeAttack→hitbox, hitbox→owner Health, MeleeAttack→owner) are already set.
- **ControlsBootstrap** — loads saved rebinds at startup.
- **KeybindMenu** — `RebindMenuBuilder` (gear button top-left / Esc opens the rebind UI at runtime).

## The only manual wiring (4 drag-drops)
The shared input asset can't be referenced safely from a hand-authored scene, so assign it once:

Drag **`Assets/Scripts/Input/ArenaControls.inputactions`** into the **`Controls`** field on:
1. PlayerOne → PlayerInputProvider
2. PlayerTwo → PlayerInputProvider
3. ControlsBootstrap
4. KeybindMenu → RebindMenuBuilder

Press Play. P1 = WASD / Space (attack) / LShift (dash) / LCtrl (block); P2 = Arrows / Enter / RCtrl / `.`

## Swapping in the real character (optional, in-Editor)
The capsules are stand-ins so the scene is engine-only and can't corrupt LFS art. To use the
Mixamo gladiator: drop **`Assets/RPGHero/Prefabs/RPGHeroHP`** as a child of PlayerOne/PlayerTwo
(or replace the MeshFilter/MeshRenderer), assign its red/blue material (`HandPainted 1` /
`HandPainted_Blue`), and leave each script's `Animator` field empty — they auto-find the Animator
in children. Hide the capsule's MeshRenderer once the model is in.
