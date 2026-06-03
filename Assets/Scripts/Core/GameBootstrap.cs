using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Single entry point for the whole game. Drop one of these into a scene
    /// (already wired into SampleScene) and it builds every system at runtime:
    /// audio, UI, the arena camera and the GameManager state machine. No other
    /// scene setup is required, which keeps the project free of fragile authored
    /// prefab/scene data while still being fully playable on Play.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Overall match configuration. Tweak in the Inspector to rebalance.")]
        public GameSettings settings = new GameSettings();

        private void Awake()
        {
            // Safety: if the component was added to a scene without serialized
            // settings (so Unity zeroed the values), fall back to sane defaults.
            if (settings == null || settings.resourcePhaseDuration <= 0f || settings.moveSpeed <= 0f)
                settings = new GameSettings();

            // Audio manager.
            var audioGo = new GameObject("AudioManager");
            audioGo.transform.SetParent(transform, false);
            var audio = audioGo.AddComponent<AudioManager>();
            audio.Initialize();

            // UI manager (builds the canvas + all screens).
            var uiGo = new GameObject("UIManager");
            uiGo.transform.SetParent(transform, false);
            var ui = uiGo.AddComponent<UIManager>();

            // Camera: reuse the scene's Main Camera if present, otherwise create one.
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            var arenaCam = cam.gameObject.GetComponent<ArenaCamera>();
            if (arenaCam == null) arenaCam = cam.gameObject.AddComponent<ArenaCamera>();

            // Make sure there's an audio listener somewhere.
            if (Object.FindFirstObjectByType<AudioListener>() == null)
                cam.gameObject.AddComponent<AudioListener>();

            // Game manager / state machine.
            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(transform, false);
            var gm = gmGo.AddComponent<GameManager>();
            gm.Initialize(settings, ui, cam, arenaCam);
        }
    }
}
