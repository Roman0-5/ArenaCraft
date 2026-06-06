using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArenaCraft
{
    /// <summary>
    /// One rebindable row in the keybinding menu: shows an action's current key and lets the
    /// player rebind or reset it. Operates directly on a binding in the shared ArenaControls asset.
    /// </summary>
    public class RebindActionUI : MonoBehaviour
    {
        #region Public Fields
        [Header("Target binding")]
        [Tooltip("Shared ArenaControls asset.")]
        public InputActionAsset controls;

        [Tooltip("Action map name: 'PlayerOne' or 'PlayerTwo'.")]
        public string actionMap = "PlayerOne";

        [Tooltip("Action name: 'Move', 'Attack' or 'Interact'.")]
        public string actionName = "Attack";

        [Tooltip("Binding index within the action. Single-key actions = 0. " +
                 "For the Move composite: 1=up, 2=down, 3=left, 4=right.")]
        public int bindingIndex = 0;

        [Header("UI")]
        [Tooltip("Optional static label, e.g. 'P1 Attack'.")]
        public Text labelText;

        [Tooltip("Shows the current key for this binding.")]
        public Text bindingText;

        public Button rebindButton;
        public Button resetButton;

        [Tooltip("Optional 'Press a key...' panel shown while listening.")]
        public GameObject waitingOverlay;
        #endregion

        #region Private Fields
        private InputAction action;
        private InputActionRebindingExtensions.RebindingOperation operation;
        #endregion

        private void Awake()
        {
            if (this.controls != null)
            {
                var map = this.controls.FindActionMap(this.actionMap, throwIfNotFound: true);
                this.action = map.FindAction(this.actionName, throwIfNotFound: true);
            }

            if (this.rebindButton != null) this.rebindButton.onClick.AddListener(this.StartRebind);
            if (this.resetButton != null) this.resetButton.onClick.AddListener(this.ResetBinding);
        }

        private void OnEnable()
        {
            this.UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (this.bindingText != null && this.action != null)
            {
                this.bindingText.text = this.action.GetBindingDisplayString(this.bindingIndex);
            }
        }

        public void StartRebind()
        {
            if (this.action == null || this.operation != null) return;

            if (this.waitingOverlay != null) this.waitingOverlay.SetActive(true);

            bool wasEnabled = this.action.enabled;
            this.action.Disable(); // required before interactive rebinding

            this.operation = this.action.PerformInteractiveRebinding(this.bindingIndex)
                .WithControlsExcluding("<Mouse>")
                .WithControlsExcluding("<Pointer>")
                .OnComplete(op => this.FinishRebind(wasEnabled))
                .OnCancel(op => this.FinishRebind(wasEnabled))
                .Start();
        }

        private void FinishRebind(bool reEnable)
        {
            this.operation?.Dispose();
            this.operation = null;

            if (reEnable) this.action.Enable();
            if (this.waitingOverlay != null) this.waitingOverlay.SetActive(false);

            this.UpdateDisplay();
            KeybindingMenuController.SaveOverrides(this.controls);
        }

        public void ResetBinding()
        {
            if (this.action == null) return;

            this.action.RemoveBindingOverride(this.bindingIndex);
            this.UpdateDisplay();
            KeybindingMenuController.SaveOverrides(this.controls);
        }

        private void OnDestroy()
        {
            this.operation?.Dispose();
        }
    }
}
