using UnityEngine;
using UnityEngine.UIElements;

namespace ArenaCraft
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private SettingsUIController m_SettingsMenu;

        private Button m_StartButton;
        private Button m_SettingsButton;
        private Button m_ClassicButton;
        private Button m_QuickButton;
        private Button m_SharedButton;
        private Button m_SplitButton;
        private Label m_SelectionSummary;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            ResponsiveUILayout.Attach(root);
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

            this.m_ClassicButton = root.Q<Button>("mode-classic-button");
            this.m_QuickButton = root.Q<Button>("mode-quick-button");
            this.m_SharedButton = root.Q<Button>("camera-shared-button");
            this.m_SplitButton = root.Q<Button>("camera-split-button");
            this.m_SelectionSummary = root.Q<Label>("selection-summary");

            if (this.m_ClassicButton != null) this.m_ClassicButton.clicked += () => this.SelectRuleSet(MatchRuleSet.GddClassic);
            if (this.m_QuickButton != null) this.m_QuickButton.clicked += () => this.SelectRuleSet(MatchRuleSet.QuickMatch);
            if (this.m_SharedButton != null) this.m_SharedButton.clicked += () => this.SelectCamera(false);
            if (this.m_SplitButton != null) this.m_SplitButton.clicked += () => this.SelectCamera(true);
            this.RefreshSelection();
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
            SceneNavigation.LoadGame();
        }

        private void SelectRuleSet(MatchRuleSet ruleSet)
        {
            MatchRules.Select(ruleSet);
            this.RefreshSelection();
        }

        private void SelectCamera(bool splitScreen)
        {
            PlayerPrefs.SetInt(SplitScreenManager.PreferenceKey, splitScreen ? 1 : 0);
            this.RefreshSelection();
        }

        private void RefreshSelection()
        {
            MatchRuleSet rules = MatchRules.Current;
            bool split = PlayerPrefs.GetInt(SplitScreenManager.PreferenceKey, 0) == 1;
            this.m_ClassicButton?.EnableInClassList("choice-button--active", rules == MatchRuleSet.GddClassic);
            this.m_QuickButton?.EnableInClassList("choice-button--active", rules == MatchRuleSet.QuickMatch);
            this.m_SharedButton?.EnableInClassList("choice-button--active", !split);
            this.m_SplitButton?.EnableInClassList("choice-button--active", split);
            if (this.m_SelectionSummary != null)
                this.m_SelectionSummary.text = $"{(rules == MatchRuleSet.GddClassic ? "GDD CLASSIC" : "QUICK MATCH")}  |  {(split ? "SPLIT SCREEN" : "SHARED SCREEN")}";
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
