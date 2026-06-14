using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaCraft
{
    public static class SceneNavigation
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "SampleScene";

        public static void LoadMainMenu()
        {
            Time.timeScale = 1f;
            Load(MainMenuScene);
        }

        public static void LoadGame()
        {
            Time.timeScale = 1f;
            Load(GameScene);
        }

        private static void Load(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"Scene '{sceneName}' is missing or disabled in Build Settings.");
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
