using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArenaCraft
{
    /// <summary>
    /// Coordinates a keybinding menu: loads overrides when opened, resets all bindings, and
    /// provides the shared save/load helpers used by <see cref="RebindActionUI"/>.
    /// Overrides are persisted to PlayerPrefs under <see cref="ControlsBootstrap.PlayerPrefsKey"/>.
    /// </summary>
    public class KeybindingMenuController : MonoBehaviour
    {
        #region Public Fields
        [Tooltip("Shared ArenaControls asset.")]
        public InputActionAsset controls;

        [Tooltip("All rebind rows in this menu; refreshed after a global reset.")]
        public RebindActionUI[] rows;

        [Tooltip("Optional 'Reset all' button.")]
        public Button resetAllButton;
        #endregion

        private void Awake()
        {
            if (this.resetAllButton != null) this.resetAllButton.onClick.AddListener(this.ResetAll);
        }

        private void OnEnable()
        {
            // Ensure overrides are applied even if the menu opens standalone (no ControlsBootstrap).
            LoadOverrides(this.controls);
            this.RefreshRows();
        }

        public void ResetAll()
        {
            if (this.controls == null) return;

            foreach (var map in this.controls.actionMaps)
            {
                map.RemoveAllBindingOverrides();
            }

            SaveOverrides(this.controls);
            this.RefreshRows();
        }

        private void RefreshRows()
        {
            if (this.rows == null) return;
            foreach (var row in this.rows)
            {
                if (row != null) row.UpdateDisplay();
            }
        }

        public static void SaveOverrides(InputActionAsset controls)
        {
            if (controls == null) return;

            string json = controls.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(ControlsBootstrap.PlayerPrefsKey, json);
            PlayerPrefs.Save();
        }

        public static void LoadOverrides(InputActionAsset controls)
        {
            if (controls == null) return;

            controls.RemoveAllBindingOverrides();

            string json = PlayerPrefs.GetString(ControlsBootstrap.PlayerPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                controls.LoadBindingOverridesFromJson(json);
            }
        }
    }
}
