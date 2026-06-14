using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace ArenaCraft.Editor
{
    [InitializeOnLoad]
    internal static class PlayModeStartScene
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

        static PlayModeStartScene()
        {
            Configure();
            EditorApplication.delayCall += Configure;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        internal static void Configure()
        {
            SceneAsset mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
            if (mainMenu != null && EditorSceneManager.playModeStartScene != mainMenu)
                EditorSceneManager.playModeStartScene = mainMenu;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode) Configure();
            if (change == PlayModeStateChange.EnteredPlayMode &&
                SceneManager.GetActiveScene().name != SceneNavigation.MainMenuScene)
            {
                SceneManager.LoadScene(SceneNavigation.MainMenuScene);
            }
        }
    }
}
