using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace ArenaCraft
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string m_GameSceneName = "SampleScene";
        [SerializeField] private SettingsUIController m_SettingsMenu;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            
            var startButton = root.Q<Button>("start-button");
            if (startButton != null)
            {
                startButton.clicked += this.OnStartClicked;
            }

            var settingsButton = root.Q<Button>("settings-button");
            if (settingsButton != null)
            {
                settingsButton.clicked += this.OnSettingsClicked;
            }
        }

        private void OnStartClicked()
        {
            SceneManager.LoadScene(this.m_GameSceneName);
        }

        private void OnSettingsClicked()
        {
            if (this.m_SettingsMenu != null)
            {
                this.m_SettingsMenu.OpenMenu();
            }
        }
    }
}
