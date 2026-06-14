using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

namespace ArenaCraft
{
    /// <summary>
    /// Controller for the UI Toolkit based settings menu.
    /// Manages tabs, graphics, audio, and keybindings.
    /// </summary>
    public class SettingsUIController : MonoBehaviour
    {
        public InputActionAsset controls;
        
        private UIDocument m_UIDocument;
        private VisualElement m_Root;
        private VisualElement m_P1Container;
        private VisualElement m_P2Container;
        private VisualElement m_WaitingOverlay;

        // Tabs
        private Button m_TabGeneralBtn;
        private Button m_TabAudioBtn;
        private Button m_TabControlsBtn;
        private VisualElement m_ContentGeneral;
        private VisualElement m_ContentAudio;
        private VisualElement m_ContentControls;
        
        // Rebinding
        private InputActionRebindingExtensions.RebindingOperation m_RebindOperation;
        private List<(InputAction action, int index, Label label)> m_RowRefs = new List<(InputAction, int, Label)>();

        private void OnEnable()
        {
            this.m_UIDocument = GetComponent<UIDocument>();
            if (this.m_UIDocument == null) return;

            this.m_UIDocument.sortingOrder = 200;

            var documentRoot = this.m_UIDocument.rootVisualElement;
            ResponsiveUILayout.Attach(documentRoot);
            documentRoot.style.position = Position.Absolute;
            documentRoot.style.left = 0;
            documentRoot.style.top = 0;
            documentRoot.style.right = 0;
            documentRoot.style.bottom = 0;

            this.m_Root = documentRoot.Q<VisualElement>("settings-root");
            if (this.m_Root == null) return;

            // Find Elements
            this.m_P1Container = this.m_Root.Q<VisualElement>("p1-rows-container");
            this.m_P2Container = this.m_Root.Q<VisualElement>("p2-rows-container");
            this.m_WaitingOverlay = this.m_Root.Q<VisualElement>("waiting-overlay");

            this.m_TabGeneralBtn = this.m_Root.Q<Button>("tab-button-general");
            this.m_TabAudioBtn = this.m_Root.Q<Button>("tab-button-audio");
            this.m_TabControlsBtn = this.m_Root.Q<Button>("tab-button-controls");

            this.m_ContentGeneral = this.m_Root.Q<VisualElement>("tab-content-general");
            this.m_ContentAudio = this.m_Root.Q<VisualElement>("tab-content-audio");
            this.m_ContentControls = this.m_Root.Q<VisualElement>("tab-content-controls");

            // Tab Click Events
            if (this.m_TabGeneralBtn != null) this.m_TabGeneralBtn.clicked += () => SwitchTab("general");
            if (this.m_TabAudioBtn != null) this.m_TabAudioBtn.clicked += () => SwitchTab("audio");
            if (this.m_TabControlsBtn != null) this.m_TabControlsBtn.clicked += () => SwitchTab("controls");

            // Setup Tabs
            SetupGeneralTab();
            SetupAudioTab();

            // Footer Buttons
            var closeBtn = this.m_Root.Q<Button>("close-button");
            if (closeBtn != null) closeBtn.clicked += this.CloseMenu;
            
            var resetBtn = this.m_Root.Q<Button>("reset-all-button");
            if (resetBtn != null) resetBtn.clicked += this.ResetAll;
            
            this.m_Root.style.display = DisplayStyle.None;
            if (this.m_WaitingOverlay != null) this.m_WaitingOverlay.style.display = DisplayStyle.None;
            
            if (this.controls != null)
                this.PopulateRows();
        }

        private void SwitchTab(string tabName)
        {
            // Reset active classes
            m_TabGeneralBtn?.RemoveFromClassList("tab-button--active");
            m_TabAudioBtn?.RemoveFromClassList("tab-button--active");
            m_TabControlsBtn?.RemoveFromClassList("tab-button--active");

            m_ContentGeneral?.RemoveFromClassList("tab-content--active");
            m_ContentAudio?.RemoveFromClassList("tab-content--active");
            m_ContentControls?.RemoveFromClassList("tab-content--active");

            // Set new active
            switch (tabName)
            {
                case "general":
                    m_TabGeneralBtn?.AddToClassList("tab-button--active");
                    m_ContentGeneral?.AddToClassList("tab-content--active");
                    break;
                case "audio":
                    m_TabAudioBtn?.AddToClassList("tab-button--active");
                    m_ContentAudio?.AddToClassList("tab-content--active");
                    break;
                case "controls":
                    m_TabControlsBtn?.AddToClassList("tab-button--active");
                    m_ContentControls?.AddToClassList("tab-content--active");
                    break;
            }
        }

        private void SetupGeneralTab()
        {
            var resDropdown = m_Root.Q<DropdownField>("resolution-dropdown");
            var fsToggle = m_Root.Q<Toggle>("fullscreen-toggle");
            var qualityDropdown = m_Root.Q<DropdownField>("quality-dropdown");
            var splitScreenToggle = m_Root.Q<Toggle>("split-screen-toggle");

            if (splitScreenToggle == null && this.m_ContentGeneral != null)
            {
                var row = new VisualElement();
                row.AddToClassList("setting-row");
                var label = new Label("Split Screen");
                label.AddToClassList("setting-label");
                splitScreenToggle = new Toggle { name = "split-screen-toggle" };
                splitScreenToggle.AddToClassList("setting-input");
                splitScreenToggle.tooltip = "Use one dedicated camera for each player. Press F10 during a match to toggle.";
                row.Add(label);
                row.Add(splitScreenToggle);
                this.m_ContentGeneral.Add(row);
            }

            if (resDropdown != null)
            {
                Resolution[] resolutions = Screen.resolutions;
                List<string> resOptions = resolutions.Select(r => $"{r.width}x{r.height}").Distinct().ToList();
                resDropdown.choices = resOptions;
                
                string currentResStr = $"{Screen.width}x{Screen.height}";
                resDropdown.value = resOptions.Contains(currentResStr) ? currentResStr : resOptions.LastOrDefault();
                resDropdown.RegisterValueChangedCallback(evt => {
                    string[] parts = evt.newValue.Split('x');
                    if (parts.Length == 2) {
                        int w = int.Parse(parts[0]);
                        int h = int.Parse(parts[1]);
                        if (SettingsManager.Instance != null) SettingsManager.Instance.SetResolution(w, h, Screen.fullScreen);
                    }
                });
            }

            if (fsToggle != null)
            {
                fsToggle.value = Screen.fullScreen;
                fsToggle.RegisterValueChangedCallback(evt => {
                    if (SettingsManager.Instance != null) SettingsManager.Instance.SetFullscreen(evt.newValue);
                });
            }

            if (qualityDropdown != null)
            {
                List<string> qualityLevels = QualitySettings.names.ToList();
                qualityDropdown.choices = qualityLevels;
                qualityDropdown.value = qualityLevels[QualitySettings.GetQualityLevel()];
                qualityDropdown.RegisterValueChangedCallback(evt => {
                    int index = qualityLevels.IndexOf(evt.newValue);
                    if (index != -1 && SettingsManager.Instance != null) SettingsManager.Instance.SetQuality(index);
                });
            }

            if (splitScreenToggle != null)
            {
                splitScreenToggle.value = PlayerPrefs.GetInt(SplitScreenManager.PreferenceKey, 1) == 1;
                splitScreenToggle.RegisterValueChangedCallback(evt =>
                {
                    if (SettingsManager.Instance != null) SettingsManager.Instance.SetSplitScreen(evt.newValue);
                    else PlayerPrefs.SetInt(SplitScreenManager.PreferenceKey, evt.newValue ? 1 : 0);
                });
            }
        }

        private void SetupAudioTab()
        {
            var masterSlider = m_Root.Q<Slider>("master-slider");
            var musicSlider = m_Root.Q<Slider>("music-slider");
            var sfxSlider = m_Root.Q<Slider>("sfx-slider");

            if (masterSlider != null)
            {
                masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
                masterSlider.RegisterValueChangedCallback(evt => {
                    if (SettingsManager.Instance != null) SettingsManager.Instance.SetVolume("MasterVolume", evt.newValue);
                });
            }
            if (musicSlider != null)
            {
                musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
                musicSlider.RegisterValueChangedCallback(evt => {
                    if (SettingsManager.Instance != null) SettingsManager.Instance.SetVolume("MusicVolume", evt.newValue);
                });
            }
            if (sfxSlider != null)
            {
                sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
                sfxSlider.RegisterValueChangedCallback(evt => {
                    if (SettingsManager.Instance != null) SettingsManager.Instance.SetVolume("SFXVolume", evt.newValue);
                });
            }
        }

        public void ToggleMenu()
        {
            if (this.m_Root == null) return;
            if (this.IsMenuOpen()) this.CloseMenu();
            else this.OpenMenu();
        }

        public bool IsMenuOpen() => this.m_Root != null && this.m_Root.style.display == DisplayStyle.Flex;

        public void OpenMenu()
        {
            if (this.m_Root == null) return;
            this.m_Root.style.display = DisplayStyle.Flex;
            this.m_Root.BringToFront();
            this.RefreshDisplay();
            SwitchTab("general");
        }

        public void CloseMenu()
        {
            if (this.m_Root == null) return;
            this.m_Root.style.display = DisplayStyle.None;
            this.m_RebindOperation?.Cancel();
        }

        private void PopulateRows()
        {
            if (this.m_P1Container == null || this.m_P2Container == null) return;
            this.m_P1Container.Clear();
            this.m_P2Container.Clear();
            this.m_RowRefs.Clear();
            this.AddPlayerSection("PlayerOne", this.m_P1Container);
            this.AddPlayerSection("PlayerTwo", this.m_P2Container);
        }

        private void AddPlayerSection(string mapName, VisualElement container)
        {
            var map = this.controls.FindActionMap(mapName);
            if (map == null) return;
            foreach (var action in map.actions)
            {
                if (action.bindings.Count > 1 && action.name == "Move")
                {
                    for (int i = 1; i < 5; i++) this.CreateRow(container, action, i);
                }
                else this.CreateRow(container, action, 0);
            }
        }

        private void CreateRow(VisualElement container, InputAction action, int bindingIndex)
        {
            var row = new VisualElement();
            row.AddToClassList("keybind-row");
            string labelName = (action.name == "Move") ? action.bindings[bindingIndex].name : action.name;
            var labelEl = new Label(labelName.ToUpper());
            labelEl.AddToClassList("action-label");
            row.Add(labelEl);
            var bindingEl = new Label("");
            bindingEl.AddToClassList("binding-label");
            row.Add(bindingEl);
            var rebindBtn = new Button(() => this.StartRebind(action, bindingIndex)) { text = "REBIND" };
            rebindBtn.AddToClassList("rebind-button");
            row.Add(rebindBtn);
            var resetBtn = new Button(() => this.ResetBinding(action, bindingIndex)) { text = "↺" };
            resetBtn.AddToClassList("reset-button");
            row.Add(resetBtn);
            container.Add(row);
            this.m_RowRefs.Add((action, bindingIndex, bindingEl));
        }

        private void StartRebind(InputAction action, int index)
        {
            if (this.m_RebindOperation != null) return;
            if (this.m_WaitingOverlay != null) this.m_WaitingOverlay.style.display = DisplayStyle.Flex;
            action.Disable();
            this.m_RebindOperation = action.PerformInteractiveRebinding(index)
                .WithControlsExcluding("<Mouse>").WithControlsExcluding("<Pointer>")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op => this.FinishRebind(action)).OnCancel(op => this.FinishRebind(action)).Start();
        }

        private void FinishRebind(InputAction action)
        {
            this.m_RebindOperation?.Dispose();
            this.m_RebindOperation = null;
            action.Enable();
            if (this.m_WaitingOverlay != null) this.m_WaitingOverlay.style.display = DisplayStyle.None;
            this.RefreshDisplay();
            KeybindingMenuController.SaveOverrides(this.controls);
        }

        private void ResetBinding(InputAction action, int index)
        {
            action.RemoveBindingOverride(index);
            this.RefreshDisplay();
            KeybindingMenuController.SaveOverrides(this.controls);
        }

        private void ResetAll()
        {
            if (this.controls == null) return;
            foreach (var map in this.controls.actionMaps) map.RemoveAllBindingOverrides();
            this.RefreshDisplay();
            KeybindingMenuController.SaveOverrides(this.controls);
        }

        private void RefreshDisplay()
        {
            foreach (var row in this.m_RowRefs) row.label.text = row.action.GetBindingDisplayString(row.index);
        }
    }
}
