using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaCraft
{
    /// <summary>
    /// Loads saved rebinding overrides onto the shared ArenaControls asset at startup.
    /// Place one of these in the scene and assign the same asset the players use.
    /// </summary>
    public class ControlsBootstrap : MonoBehaviour
    {
        /// <summary>PlayerPrefs key under which rebind overrides are stored as JSON.</summary>
        public const string PlayerPrefsKey = "ArenaControls.rebinds";

        #region Public Fields
        [Tooltip("Shared ArenaControls asset used by the players and the keybinding menu.")]
        public InputActionAsset controls;
        #endregion

        private void Awake()
        {
            if (this.controls == null) return;

            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                this.controls.LoadBindingOverridesFromJson(json);
            }
        }
    }
}
