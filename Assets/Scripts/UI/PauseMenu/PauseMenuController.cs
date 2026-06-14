using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace ArenaCraft
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset m_Controls;
        [SerializeField] private SettingsUIController m_SettingsMenu;

        private UIDocument m_UIDocument;
        private VisualElement m_PauseRoot;
        private bool m_IsPaused;
        private InputAction m_MenuAction;
        private float m_AcceptPauseInputAt;

        private void Awake()
        {
            Time.timeScale = 1f;
            this.m_IsPaused = false;
            this.m_AcceptPauseInputAt = Time.unscaledTime + 0.35f;
            this.m_UIDocument = GetComponent<UIDocument>();
            if (this.m_UIDocument == null) return;

            this.m_UIDocument.sortingOrder = 100;
            ResponsiveUILayout.Attach(this.m_UIDocument.rootVisualElement);
            this.m_PauseRoot = this.m_UIDocument.rootVisualElement.Q<VisualElement>("pause-root");
            if (this.m_PauseRoot != null)
            {
                this.m_PauseRoot.style.display = DisplayStyle.None;
            }

            var resumeBtn = this.m_UIDocument.rootVisualElement.Q<Button>("resume-button");
            if (resumeBtn != null) resumeBtn.clicked += this.ResumeGame;

            var settingsBtn = this.m_UIDocument.rootVisualElement.Q<Button>("settings-button");
            if (settingsBtn != null) settingsBtn.clicked += this.OpenSettings;

            var mainMenuBtn = this.m_UIDocument.rootVisualElement.Q<Button>("main-menu-button");
            if (mainMenuBtn != null) mainMenuBtn.clicked += this.LoadMainMenu;

            var quitBtn = this.m_UIDocument.rootVisualElement.Q<Button>("quit-button");
            if (quitBtn != null) quitBtn.clicked += this.QuitGame;
        }

        private void OnEnable()
        {
            if (this.m_Controls != null)
            {
                var uiMap = this.m_Controls.FindActionMap("UI");
                if (uiMap != null)
                {
                    uiMap.Enable();
                    this.m_MenuAction = uiMap.FindAction("Menu");
                    if (this.m_MenuAction != null)
                    {
                        this.m_MenuAction.performed += this.OnMenuPressed;
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (this.m_MenuAction != null)
            {
                this.m_MenuAction.performed -= this.OnMenuPressed;
            }
        }

        private void OnMenuPressed(InputAction.CallbackContext context)
        {
            if (Time.unscaledTime < this.m_AcceptPauseInputAt)
                return;

            if (this.m_SettingsMenu != null && this.m_SettingsMenu.gameObject.activeInHierarchy && this.m_SettingsMenu.IsMenuOpen())
            {
                this.m_SettingsMenu.CloseMenu();
                return;
            }

            this.TogglePause();
        }

        public void TogglePause()
        {
            this.m_IsPaused = !this.m_IsPaused;
            
            if (this.m_IsPaused)
            {
                this.PauseGame();
            }
            else
            {
                this.ResumeGame();
            }
        }

        public void PauseGame()
        {
            this.m_IsPaused = true;
            Time.timeScale = 0f;
            if (this.m_PauseRoot != null) this.m_PauseRoot.style.display = DisplayStyle.Flex;
        }

        public void ResumeGame()
        {
            this.m_IsPaused = false;
            Time.timeScale = 1f;
            if (this.m_PauseRoot != null) this.m_PauseRoot.style.display = DisplayStyle.None;
            if (this.m_SettingsMenu != null) this.m_SettingsMenu.CloseMenu();
        }

        private void OpenSettings()
        {
            if (this.m_SettingsMenu != null)
            {
                this.m_SettingsMenu.OpenMenu();
            }
        }

        private void LoadMainMenu()
        {
            SceneNavigation.LoadMainMenu();
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
