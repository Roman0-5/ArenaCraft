using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Per-player identity and key bindings. Defaults follow GDD 5.2:
    ///   P1: WASD move, Space attack, F interact/shop.
    ///   P2: Arrow keys move, Enter attack, Right Shift interact/shop.
    /// Bindings are mutable so the Options menu can offer custom keybindings (GDD 2.3 / 5.2).
    /// </summary>
    [System.Serializable]
    public class PlayerConfig
    {
        public int playerId;        // 1 or 2
        public string label;        // e.g. "P1 - Red"
        public Color accentColor;   // P1 red, P2 blue (GDD 3.2.1)

        public KeyCode up, down, left, right;
        public KeyCode attack;
        public KeyCode interact;

        public static PlayerConfig Player1()
        {
            return new PlayerConfig
            {
                playerId = 1,
                label = "P1 - Red",
                accentColor = new Color(0.85f, 0.18f, 0.18f),
                up = KeyCode.W, down = KeyCode.S, left = KeyCode.A, right = KeyCode.D,
                attack = KeyCode.Space, interact = KeyCode.F
            };
        }

        public static PlayerConfig Player2()
        {
            return new PlayerConfig
            {
                playerId = 2,
                label = "P2 - Blue",
                accentColor = new Color(0.20f, 0.45f, 0.90f),
                up = KeyCode.UpArrow, down = KeyCode.DownArrow, left = KeyCode.LeftArrow, right = KeyCode.RightArrow,
                attack = KeyCode.Return, interact = KeyCode.RightShift
            };
        }

        /// <summary>Movement input as a normalised X/Z direction (Y is always 0).</summary>
        public Vector3 ReadMove()
        {
            float x = (Input.GetKey(right) ? 1f : 0f) - (Input.GetKey(left) ? 1f : 0f);
            float z = (Input.GetKey(up) ? 1f : 0f) - (Input.GetKey(down) ? 1f : 0f);
            Vector3 v = new Vector3(x, 0f, z);
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        // The attack key for P2 is Enter; accept the numpad enter as well for convenience.
        public bool AttackPressed()
        {
            if (Input.GetKeyDown(attack)) return true;
            if (attack == KeyCode.Return && Input.GetKeyDown(KeyCode.KeypadEnter)) return true;
            return false;
        }

        public bool InteractPressed() => Input.GetKeyDown(interact);
    }
}
