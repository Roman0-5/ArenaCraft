using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ArenaCraft
{
    /// <summary>
    /// Builds the whole keybinding menu UI at runtime from the ArenaControls asset, so no manual
    /// Canvas/row wiring is needed. Add this to an empty GameObject, assign the controls asset, and
    /// press Play — it creates a Canvas with one rebind row per binding (both players) plus a
    /// "Reset all" button, and wires up RebindActionUI + KeybindingMenuController automatically.
    /// </summary>
    public class RebindMenuBuilder : MonoBehaviour
    {
        #region Public Fields
        [Tooltip("Shared ArenaControls asset.")]
        public InputActionAsset controls;

        [Tooltip("Build the menu automatically on Start.")]
        public bool buildOnStart = true;
        #endregion

        #region Private Fields
        private Font font;
        private GameObject menuRoot;
        #endregion

        private void Start()
        {
            if (this.buildOnStart) this.Build();
        }

        public void Build()
        {
            if (this.controls == null)
            {
                Debug.LogError($"{nameof(RebindMenuBuilder)}: no ArenaControls asset assigned.", this);
                return;
            }

            this.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (this.font == null) this.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            this.EnsureEventSystem();

            // Build everything under an INACTIVE canvas so RebindActionUI / KeybindingMenuController
            // Awake/OnEnable only run once all their fields are populated. Activated at the end.
            var canvasGO = new GameObject("KeybindingCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(this.transform, false);
            canvasGO.SetActive(false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true; // crisp text (avoids upscaling blur)
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var panel = this.CreatePanel(canvasGO.transform);

            this.CreateLabel(panel.transform, "Keybindings", 30, TextAnchor.MiddleCenter, 44);

            var rows = new List<RebindActionUI>();
            this.AddPlayerRows(panel.transform, "PlayerOne", "P1", rows);
            this.AddPlayerRows(panel.transform, "PlayerTwo", "P2", rows);

            var resetAll = this.CreateButton(panel.transform, "Reset all to defaults", 40);

            var controller = canvasGO.AddComponent<KeybindingMenuController>();
            controller.controls = this.controls;
            controller.rows = rows.ToArray();
            controller.resetAllButton = resetAll;

            var closeBtn = this.CreateButton(panel.transform, "Save & Close  (Esc)", 44);
            closeBtn.onClick.AddListener(this.CloseMenu);

            // Always-visible gear button (top-left) that opens the menu.
            var gear = this.CreateButton(canvasGO.transform, "⚙");
            var grt = gear.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0f, 1f);
            grt.anchorMax = new Vector2(0f, 1f);
            grt.pivot = new Vector2(0f, 1f);
            grt.sizeDelta = new Vector2(50f, 50f);
            grt.anchoredPosition = new Vector2(12f, -12f);
            var gearText = gear.GetComponentInChildren<Text>();
            if (gearText != null) gearText.fontSize = 28;
            gear.onClick.AddListener(this.ToggleMenu);

            // The panel (not the whole canvas) is what gets shown/hidden, so the gear stays visible.
            this.menuRoot = panel.gameObject;
            canvasGO.SetActive(true);        // initialize children with fields populated
            this.menuRoot.SetActive(false);  // menu starts hidden; open via gear or Esc
        }

        /// <summary>Show the menu.</summary>
        public void OpenMenu()
        {
            if (this.menuRoot != null) this.menuRoot.SetActive(true);
        }

        /// <summary>Hide the menu. Rebinds are already saved live, so nothing else is needed.</summary>
        public void CloseMenu()
        {
            if (this.menuRoot != null) this.menuRoot.SetActive(false);
        }

        /// <summary>Show/hide the menu (bound to Esc).</summary>
        public void ToggleMenu()
        {
            if (this.menuRoot != null) this.menuRoot.SetActive(!this.menuRoot.activeSelf);
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) this.ToggleMenu();
        }

        private void AddPlayerRows(Transform parent, string map, string prefix, List<RebindActionUI> rows)
        {
            (string label, string action, int idx)[] entries =
            {
                ("Up", "Move", 1),
                ("Down", "Move", 2),
                ("Left", "Move", 3),
                ("Right", "Move", 4),
                ("Attack", "Attack", 0),
                ("Interact", "Interact", 0),
                ("Dash", "Dash", 0),
                ("Block", "Block", 0),
            };

            foreach (var e in entries)
            {
                var rowGO = new GameObject($"{prefix}_{e.label}",
                    typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                rowGO.transform.SetParent(parent, false);

                var hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childAlignment = TextAnchor.MiddleLeft;

                rowGO.GetComponent<LayoutElement>().preferredHeight = 36;

                var labelText = this.CreateLabel(rowGO.transform, $"{prefix} {e.label}", 20, TextAnchor.MiddleLeft, 0, 220);
                var bindingText = this.CreateLabel(rowGO.transform, "", 20, TextAnchor.MiddleCenter, 0, 170);
                var rebindBtn = this.CreateButton(rowGO.transform, "Rebind", 0, 120);
                var resetBtn = this.CreateButton(rowGO.transform, "Reset", 0, 90);

                var rebind = rowGO.AddComponent<RebindActionUI>();
                rebind.controls = this.controls;
                rebind.actionMap = map;
                rebind.actionName = e.action;
                rebind.bindingIndex = e.idx;
                rebind.labelText = labelText;
                rebind.bindingText = bindingText;
                rebind.rebindButton = rebindBtn;
                rebind.resetButton = resetBtn;

                rows.Add(rebind);
            }
        }

        private RectTransform CreatePanel(Transform parent)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(780, 980);
            rt.anchoredPosition = Vector2.zero;

            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 8;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childAlignment = TextAnchor.UpperCenter;

            return rt;
        }

        private Text CreateLabel(Transform parent, string text, int size, TextAnchor anchor,
            float preferredHeight = 0, float preferredWidth = 0)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var t = go.GetComponent<Text>();
            t.font = this.font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            this.ApplyLayout(go, preferredHeight, preferredWidth);
            return t;
        }

        private Button CreateButton(Transform parent, string text, float preferredHeight = 0, float preferredWidth = 0)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var label = new GameObject("Text", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(go.transform, false);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var t = label.GetComponent<Text>();
            t.font = this.font;
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = text;

            this.ApplyLayout(go, preferredHeight, preferredWidth);
            return go.GetComponent<Button>();
        }

        private void ApplyLayout(GameObject go, float preferredHeight, float preferredWidth)
        {
            if (preferredHeight <= 0 && preferredWidth <= 0) return;
            var le = go.AddComponent<LayoutElement>();
            if (preferredHeight > 0) le.preferredHeight = preferredHeight;
            if (preferredWidth > 0) le.preferredWidth = preferredWidth;
        }

        private void EnsureEventSystem()
        {
            var existing = EventSystem.current;
            if (existing == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                return;
            }

            // Project is New-Input-System only: make sure the EventSystem uses the new UI module,
            // otherwise clicks won't register (and the old StandaloneInputModule errors out).
            if (existing.GetComponent<InputSystemUIInputModule>() == null)
            {
                var legacy = existing.GetComponent<StandaloneInputModule>();
                if (legacy != null) Destroy(legacy);
                existing.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }
    }
}
