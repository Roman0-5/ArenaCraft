using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaCraft
{
    /// <summary>Which of the two local players this object represents.</summary>
    public enum PlayerSlot { One, Two }

    /// <summary>
    /// Resolves and owns the per-player <see cref="InputActionMap"/> from the shared
    /// ArenaControls asset, and exposes its actions. Both <see cref="PlayerController"/> and
    /// <see cref="MeleeAttack"/> read input through this single component, so the asset and slot
    /// only need to be wired once per player.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputProvider : MonoBehaviour
    {
        #region Public Fields
        [Tooltip("Shared ArenaControls asset (maps: PlayerOne / PlayerTwo).")]
        public InputActionAsset controls;

        [Tooltip("Which player this object is. Picks the matching action map.")]
        public PlayerSlot slot = PlayerSlot.One;
        #endregion

        #region Private Fields
        private InputActionMap map;
        #endregion

        public PlayerSlot Slot => this.slot;
        public InputAction Move { get; private set; }
        public InputAction Attack { get; private set; }
        public InputAction Interact { get; private set; }
        public InputAction Dash { get; private set; }
        public InputAction Block { get; private set; }

        private void Awake()
        {
            if (this.controls == null)
            {
                Debug.LogError($"{nameof(PlayerInputProvider)} on '{name}' has no ArenaControls asset assigned.", this);
                return;
            }

            string mapName = this.slot == PlayerSlot.One ? "PlayerOne" : "PlayerTwo";
            this.map = this.controls.FindActionMap(mapName, throwIfNotFound: true);
            this.Move = this.map.FindAction("Move", throwIfNotFound: true);
            this.Attack = this.map.FindAction("Attack", throwIfNotFound: true);
            this.Interact = this.map.FindAction("Interact", throwIfNotFound: true);
            this.Dash = this.map.FindAction("Dash", throwIfNotFound: true);
            this.Block = this.map.FindAction("Block", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            this.map?.Enable();
        }

        private void OnDisable()
        {
            this.map?.Disable();
        }
    }
}
