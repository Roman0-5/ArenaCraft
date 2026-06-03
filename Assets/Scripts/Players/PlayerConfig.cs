using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaCraft
{
    /// <summary>
    /// Per-player identity and key bindings. Defaults follow GDD 5.2:
    ///   P1: WASD move, Space attack, F interact/shop.
    ///   P2: Arrow keys move, Enter attack, Right Shift interact/shop.
    /// Uses the new Input System (the project's active input handler) via
    /// Keyboard.current. Bindings are mutable so a custom-keybindings option can
    /// be offered later (GDD 2.3 / 5.2).
    /// </summary>
    [System.Serializable]
    public class PlayerConfig
    {
        public int playerId;        // 1 or 2
        public string label;        // e.g. "P1 - Red"
        public Color accentColor;   // P1 red, P2 blue (GDD 3.2.1)

        public Key up, down, left, right;
        public Key attack;
        public Key interact;

        public static PlayerConfig Player1()
        {
            return new PlayerConfig
            {
                playerId = 1,
                label = "P1 - Red",
                accentColor = new Color(0.85f, 0.18f, 0.18f),
                up = Key.W, down = Key.S, left = Key.A, right = Key.D,
                attack = Key.Space, interact = Key.F
            };
        }

        public static PlayerConfig Player2()
        {
            return new PlayerConfig
            {
                playerId = 2,
                label = "P2 - Blue",
                accentColor = new Color(0.20f, 0.45f, 0.90f),
                up = Key.UpArrow, down = Key.DownArrow, left = Key.LeftArrow, right = Key.RightArrow,
                attack = Key.Enter, interact = Key.RightShift
            };
        }

        private static bool Pressed(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].isPressed;
        }

        private static bool Down(Key k)
        {
            var kb = Keyboard.current;
            return kb != null && kb[k].wasPressedThisFrame;
        }

        /// <summary>Movement input as a normalised X/Z direction (Y is always 0).</summary>
        public Vector3 ReadMove()
        {
            float x = (Pressed(right) ? 1f : 0f) - (Pressed(left) ? 1f : 0f);
            float z = (Pressed(up) ? 1f : 0f) - (Pressed(down) ? 1f : 0f);
            Vector3 v = new Vector3(x, 0f, z);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        // The attack key for P2 is Enter; accept the numpad enter as well for convenience.
        public bool AttackPressed()
        {
            if (Down(attack)) return true;
            if (attack == Key.Enter && Down(Key.NumpadEnter)) return true;
            return false;
        }

        public bool InteractPressed() => Down(interact);
        public bool UpPressed() => Down(up);
        public bool DownPressed() => Down(down);
    }
}
