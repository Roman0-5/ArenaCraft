# ArenaCraft – Paket 2: Editor-Setup-Anleitung

Alle **Skripte** liegen unter `Assets/Scripts/` und sind fertig. Diese Anleitung beschreibt die
**Editor-Arbeit**, die du in Unity machst (Import, Prefabs, Szene, Inspector-Referenzen).
Reihenfolge wie geplant: erst Charakter + Animation, dann verdrahten.

> Hinweis: Beim ersten Öffnen von Unity nach dem Hinzufügen der Skripte kompiliert es automatisch.
> Prüfe die Console (Window > General > Console) – es sollten **keine roten Fehler** kommen.

---

## A) Charakter-Assets importieren (Schritt 1)

1. Lade 2 Low-Poly-Gladiator-Modelle (oder **1** Modell, das wir 2× mit roter/blauer Akzentfarbe
   verwenden). Quelle: Unity Asset Store oder Mixamo.
2. FBX in den Projekt-Ordner ziehen, z. B. `Assets/Characters/`.
3. Modell anklicken → Inspector → Tab **Rig**:
   - **Animation Type = Humanoid**, **Avatar Definition = Create From This Model** → **Apply**.
   - „Configure…" prüfen, ob das Skelett korrekt gemappt ist (grün).

### Animationen (Mixamo, GDD-Issue C.4)
Falls das Asset-Store-Modell schon Idle/Walk/Attack mitbringt, kannst du diese direkt nutzen –
sonst über Mixamo:
1. [mixamo.com](https://www.mixamo.com) (kostenlos mit Adobe-Account). Modell hochladen **oder**
   ein Mixamo-Charaktermodell wählen.
2. Animationen suchen: **Idle**, **Walk** (oder Run), **Attack** (z. B. „Sword And Shield Slash").
3. Download als **FBX**, „Without Skin" für die reinen Animationen (das Modell einmal „With Skin").
4. Import in Unity, je FBX im Tab **Rig**: **Humanoid**, Avatar = **Copy From Other Avatar**
   (das Avatar deines Modells wählen).
5. Tab **Animation**: bei **Idle** und **Walk** den Clip auf **Loop Time = an** stellen.

---

## B) Animator Controller „GladiatorAC" (Schritt 1)

1. `Assets/Characters/` → Rechtsklick → **Create > Animator Controller**, Name `GladiatorAC`.
2. Doppelklick → Animator-Fenster. Tab **Parameters** (links):
   - `Speed` → **Float**
   - `Attack` → **Trigger**
   - *(optional)* `Die` → **Trigger**
3. Clips reinziehen → States `Idle`, `Walk`, `Attack` (Idle als Default = orange).
4. **Transitions** (Rechtsklick auf State → Make Transition):
   - `Idle → Walk`: Condition `Speed` **Greater** `0.1`. Has Exit Time = **aus**.
   - `Walk → Idle`: Condition `Speed` **Less** `0.1`. Has Exit Time = **aus**.
   - `Any State → Attack`: Condition `Attack` (Trigger). Has Exit Time = **aus**.
   - `Attack → Idle`: **Has Exit Time = an** (z. B. 0.9), keine Condition (geht nach dem Swing zurück).
   - *(optional)* `Any State → Die`: Condition `Die`.
5. Bei Idle/Walk/Attack jeweils sinnvolle Übergangsdauer (~0.1 s) einstellen.

> Wichtig: Im **Animator-Component** des Prefabs (siehe D) muss **Apply Root Motion = AUS** sein,
> damit das Skript die Position steuert, nicht die Animation.

---

## C) Default-Waffe „Fists" (optional, sonst Fallback-Werte)

1. `Assets/` → Rechtsklick → **Create > ArenaCraft > Weapon**, Name `Fists`.
2. Im Inspector: `damage = 10`, `swingCooldown = 0.5`, `displayName = Fists`.
   (Ohne Waffe nutzt `MeleeAttack` automatisch `fallbackDamage`/`fallbackCooldown`.)

---

## D) Spieler-Prefab bauen (Schritt 8)

Baue **Player1**; **Player2** ist eine Kopie mit anderem Slot + blauer Farbe.

1. Leeres GameObject `Player1` erstellen. Komponenten hinzufügen:
   - **Rigidbody**: Use Gravity = **an**, Is Kinematic = **aus**. (Constraints setzt das Skript.)
   - **Capsule Collider** (Body, **kein** Trigger) – als physischer Körper.
   - **PlayerInputProvider**: `Controls` = `ArenaControls` (Asset aus `Assets/Scripts/Input/`),
     `Slot` = **One**.
   - **PlayerController**: `Move Speed` ~6.
   - **Health**: `Base Max HP` = 100, `Armor` = None.
   - **MeleeAttack**: `Weapon` = `Fists` (optional), `Hitbox` = (siehe 3.), `Combat Audio` = (siehe 4.).
   - **AudioSource** + **CombatAudio**: `Swing Clip` / `Hit Clip` zuweisen (SFX importieren).
2. **Modell als Kind**: das FBX-Modell als Child unter `Player1` ziehen, Position (0,0,0).
   - Auf dem Modell den **Animator**: `Controller` = `GladiatorAC`, `Avatar` = dein Avatar,
     **Apply Root Motion = aus**.
3. **Hitbox als Kind**: leeres GameObject `Hitbox` unter `Player1`.
   - Position **vor** dem Charakter (z. B. Z = +1), passend zur Reichweite.
   - **Box Collider** (oder Capsule) hinzufügen, **Is Trigger = an**.
     (Das Skript deaktiviert den Collider automatisch, bis ein Swing aktiv ist.)
   - **AttackHitbox**-Component drauf (`owner` füllt `MeleeAttack` automatisch).
   - In `MeleeAttack` das Feld **Hitbox** auf dieses `Hitbox`-Objekt ziehen.
4. `Player1` als Prefab speichern (in `Assets/Prefabs/` ziehen).
5. **Player2** = Prefab duplizieren/Variante:
   - `PlayerInputProvider.Slot` = **Two**.
   - Akzentfarbe Blau: Material des Modells duplizieren, Farbe ändern, zuweisen
     (Rot bleibt bei Player1).

---

## E) Szene + Controls-Bootstrap (Schritt 2)

1. Szene `Assets/Scenes/SampleScene.unity` öffnen (oder neue Arena-Szene).
2. Boden: ein großes **Plane**/3D-Objekt mit Collider (provisorische Arena reicht für Paket 2).
3. Leeres GameObject `Controls` → **ControlsBootstrap**-Component → `Controls` = `ArenaControls`.
4. `Player1` und `Player2` in die Szene ziehen, etwas auseinander platzieren (über dem Boden).
5. Eine Kamera so positionieren, dass beide sichtbar sind (Kamera-System ist Paket 1 – für den
   Test reicht eine schräg von oben gerichtete Kamera).

---

## F) Keybinding-Menü (Schritt 6, volles Rebind)

1. **Canvas** erstellen (UI > Canvas) + **EventSystem** (kommt automatisch mit).
2. Pro Aktion eine Zeile bauen (Panel mit: Label-`Text`, aktuelle-Taste-`Text`, `Rebind`-Button,
   `Reset`-Button). Empfehlung – diese Zeilen anlegen:
   - **P1**: Attack (bindingIndex 0), Interact (0), und Move-Teile up/down/left/right (Index 1/2/3/4).
   - **P2**: dieselben Aktionen, `actionMap = PlayerTwo`.
3. Auf jede Zeile ein **RebindActionUI**:
   - `Controls` = `ArenaControls`, `Action Map` = `PlayerOne`/`PlayerTwo`,
     `Action Name` = `Attack`/`Interact`/`Move`, `Binding Index` wie oben.
   - `Label Text`, `Binding Text`, `Rebind Button`, `Reset Button` zuweisen.
   - *(optional)* `Waiting Overlay` = ein „Drücke eine Taste…"-Panel.
4. Auf das Menü-Root ein **KeybindingMenuController**:
   - `Controls` = `ArenaControls`, `Rows` = alle RebindActionUI-Zeilen, `Reset All Button` (optional).

> Tipp: Du kannst das Menü auch als eigene Szene/Overlay bauen. Die Overrides werden in PlayerPrefs
> gespeichert und beim Spielstart von `ControlsBootstrap` geladen.

---

## G) Verifikation (Play Mode)

1. **Bewegung/Facing**: P1 (WASD) & P2 (Pfeile) bewegen sich gleichzeitig auf X/Z; Modell dreht
   sich in Laufrichtung; Idle/Walk schaltet (Speed-Parameter).
2. **Kampf**: Space (P1) / Enter (P2) → Attack-Animation + kurzes Hitbox-Fenster; Cooldown stoppt
   Spam; Gegner im Fenster verliert HP; kein Selbsttreffer, kein Doppelschaden pro Swing.
3. **HP/Armor**: Schaden senkt HP; bei 0 → Tod (Bewegung/Angriff aus). Setze `Health.armor` auf
   Light/Heavy → MaxHP 125/150 (im Inspector zur Laufzeit prüfbar).
4. **Rebinding**: im Menü eine Taste neu binden → wirkt sofort; Editor stoppen/starten → bleibt
   erhalten (PlayerPrefs); „Reset all" stellt Defaults wieder her.
5. **Audio**: Swing- und Treffer-SFX spielen.

---

## Offene Punkte (laut GDD)
- **C.3 Swing-Cooldown**: Default 0.5 s – im Inspector (`Weapon` oder `MeleeAttack`) tunen.
- **C.4 Animation**: Mixamo, Humanoid, Root Motion aus – Lizenz über Adobe-Account ok.
