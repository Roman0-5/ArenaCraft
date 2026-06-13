using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace ArenaCraft
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string m_GameSceneName = "SampleScene";
        [SerializeField] private SettingsUIController m_SettingsMenu;

        private Button m_StartButton;
        private Button m_SettingsButton;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            var vignette = root.Q<VisualElement>(className: "vignette");
            if (vignette != null) vignette.pickingMode = PickingMode.Ignore;

            this.m_StartButton = root.Q<Button>("start-button");
            if (this.m_StartButton != null)
            {
                this.m_StartButton.clicked += this.OnStartClicked;
            }

            this.m_SettingsButton = root.Q<Button>("settings-button");
            if (this.m_SettingsButton != null)
            {
                this.m_SettingsButton.clicked += this.OnSettingsClicked;
            }
        }

        private void OnDisable()
        {
            if (this.m_StartButton != null)
                this.m_StartButton.clicked -= this.OnStartClicked;

            if (this.m_SettingsButton != null)
                this.m_SettingsButton.clicked -= this.OnSettingsClicked;
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
