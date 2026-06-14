using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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

        public bool WasAttackPressedThisFrame()
        {
            if (!this.isActiveAndEnabled)
                return false;

            if (Keyboard.current != null)
            {
                KeyControl attackKey = this.slot == PlayerSlot.One
                    ? Keyboard.current.spaceKey
                    : Keyboard.current.enterKey;
                if (attackKey.wasPressedThisFrame)
                    return true;
            }

            return this.Attack != null &&
                   this.Attack.enabled &&
                   this.Attack.WasPerformedThisFrame();
        }

        public Vector2 ReadMove()
        {
            if (!this.isActiveAndEnabled)
                return Vector2.zero;

            if (Keyboard.current != null)
            {
                Vector2 keyboardValue = this.slot == PlayerSlot.One
                    ? new Vector2(
                        ReadAxis(Keyboard.current.aKey, Keyboard.current.dKey),
                        ReadAxis(Keyboard.current.sKey, Keyboard.current.wKey))
                    : new Vector2(
                        ReadAxis(Keyboard.current.leftArrowKey, Keyboard.current.rightArrowKey),
                        ReadAxis(Keyboard.current.downArrowKey, Keyboard.current.upArrowKey));

                if (keyboardValue.sqrMagnitude > 0.001f)
                    return keyboardValue;
            }

            return this.Move != null && this.Move.enabled
                ? this.Move.ReadValue<Vector2>()
                : Vector2.zero;
        }

        private static float ReadAxis(KeyControl negative, KeyControl positive)
        {
            return (positive.isPressed ? 1f : 0f) - (negative.isPressed ? 1f : 0f);
        }

        private void Awake()
        {
            if (this.controls == null)
            {
                Debug.LogError($"{nameof(PlayerInputProvider)} on '{name}' has no ArenaControls asset assigned.", this);
                return;
            }

            ResolveActions();
        }

        private void OnEnable()
        {
            if (this.map == null && this.controls != null)
                ResolveActions();

            this.map?.Enable();
        }

        private void OnDisable()
        {
            this.map?.Disable();
        }

        private void ResolveActions()
        {
            string mapName = this.slot == PlayerSlot.One ? "PlayerOne" : "PlayerTwo";
            this.map = this.controls.FindActionMap(mapName, throwIfNotFound: true);
            this.Move = this.map.FindAction("Move", throwIfNotFound: true);
            this.Attack = this.map.FindAction("Attack", throwIfNotFound: true);
            this.Interact = this.map.FindAction("Interact", throwIfNotFound: true);
            this.Dash = this.map.FindAction("Dash", throwIfNotFound: true);
            this.Block = this.map.FindAction("Block", throwIfNotFound: true);
        }
    }
}
