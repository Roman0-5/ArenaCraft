using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ArenaCraft
{
    /// <summary>
    /// Builds and owns the whole 2D interface in code: the shared HUD (phase
    /// timer + two player panels), the item shop, and the full-screen Main Menu,
    /// Options, Tutorial and Victory screens. Implements the screen flow in
    /// GDD 2.2.7.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private GameManager _game;
        private GameSettings _settings;

        private Canvas _canvas;

        // HUD
        private Text _timerText;
        private Text _phaseText;
        private readonly List<PlayerHUD> _huds = new List<PlayerHUD>();

        // Shop
        private readonly List<ShopPanel> _shopPanels = new List<ShopPanel>();
        private GameObject _shopRoot;

        // Overlays
        private GameObject _mainMenu, _options, _victory, _tutorial;
        private Text _victoryTitle, _victoryScore;
        private Text _announcement;
        private float _announcementTimer;

        private MenuNavigator _menuNav, _optionsNav, _victoryNav;
        private Text _timerValueLabel;

        public void Build(GameManager game, GameSettings settings, PlayerConfig[] configs, PlayerStats[] stats)
        {
            _game = game;
            _settings = settings;

            // --- Canvas ---
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // --- EventSystem (for mouse clicks on buttons) ---
            // Uses the new Input System UI module since that is the project's
            // active input handler; AssignDefaultActions wires up pointer/click
            // without needing a serialized input-actions asset.
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
                es.transform.SetParent(transform, false);
                var module = es.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }

            BuildHud(configs, stats);
            BuildShop(configs, stats);
            BuildAnnouncement();
            BuildMainMenu();
            BuildOptions();
            BuildTutorial(configs);
            BuildVictory();
        }

        // ---------------- HUD ----------------
        private void BuildHud(PlayerConfig[] configs, PlayerStats[] stats)
        {
            // Top-center phase timer (GDD 5.1 shared timer / FSR2).
            var timerPanel = UIFactory.Panel("TimerPanel", _canvas.transform, new Color(0.08f, 0.07f, 0.06f, 0.8f));
            UIFactory.Anchor(timerPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(320f, 80f));

            _phaseText = UIFactory.Label("Phase", timerPanel.transform, "", 18, new Color(1f, 0.85f, 0.5f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(_phaseText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -20f), new Vector2(0f, 28f));
            _timerText = UIFactory.Label("Time", timerPanel.transform, "", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(_timerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(0f, -30f));

            for (int i = 0; i < configs.Length; i++)
            {
                var hudGo = new GameObject($"PlayerHUD{i + 1}");
                hudGo.transform.SetParent(transform, false);
                var hud = hudGo.AddComponent<PlayerHUD>();
                hud.Build(_canvas.transform, configs[i], stats[i], _settings, leftSide: i == 0);
                _huds.Add(hud);
            }
        }

        public void SetTimer(string phaseName, float timeValue, bool show, bool urgent = true)
        {
            _phaseText.transform.parent.gameObject.SetActive(show);
            if (!show) return;
            _phaseText.text = phaseName;
            int total = Mathf.CeilToInt(Mathf.Max(0f, timeValue));
            _timerText.text = $"{total / 60:0}:{total % 60:00}";
            _timerText.color = (urgent && timeValue <= 10f && timeValue > 0f) ? new Color(1f, 0.4f, 0.3f) : Color.white;
        }

        public void SetHudVisible(bool visible)
        {
            foreach (var h in _huds) if (h != null) h.SetVisible(visible);
        }

        // ---------------- Shop ----------------
        private void BuildShop(PlayerConfig[] configs, PlayerStats[] stats)
        {
            _shopRoot = UIFactory.Panel("ShopRoot", _canvas.transform, new Color(0f, 0f, 0f, 0.45f));
            UIFactory.Stretch(_shopRoot.GetComponent<RectTransform>());

            var header = UIFactory.Label("ShopHeader", _shopRoot.transform, "SHOPPING PHASE — Spend your gold!", 26, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(700f, 40f));

            var catalog = ShopCatalog.BuildDefault();
            for (int i = 0; i < configs.Length; i++)
            {
                var panelGo = new GameObject($"ShopPanel{i + 1}");
                panelGo.transform.SetParent(transform, false);
                var panel = panelGo.AddComponent<ShopPanel>();
                // Give each player an independent copy of the catalog list.
                panel.Build(_shopRoot.transform, configs[i], stats[i], new List<ShopItem>(catalog), leftSide: i == 0);
                _shopPanels.Add(panel);
            }
            _shopRoot.SetActive(false);
        }

        public void ShowShop(bool show)
        {
            _shopRoot.SetActive(show);
            foreach (var p in _shopPanels) p.SetActive(show);
        }

        public bool AllPlayersReady()
        {
            foreach (var p in _shopPanels) if (!p.Ready) return false;
            return _shopPanels.Count > 0;
        }

        public void UpdateShopWarning(bool showWarning)
        {
            foreach (var p in _shopPanels) p.UpdateWarning(showWarning);
        }

        // ---------------- Announcement banner ----------------
        private void BuildAnnouncement()
        {
            _announcement = UIFactory.Label("Announcement", _canvas.transform, "", 48, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(_announcement.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1200f, 120f));
            _announcement.gameObject.SetActive(false);
        }

        public void Announce(string text, float duration = 2.5f)
        {
            _announcement.text = text;
            _announcement.gameObject.SetActive(true);
            _announcementTimer = duration;
        }

        // ---------------- Main Menu ----------------
        private void BuildMainMenu()
        {
            _mainMenu = UIFactory.Panel("MainMenu", _canvas.transform, new Color(0.10f, 0.08f, 0.07f, 1f));
            UIFactory.Stretch(_mainMenu.GetComponent<RectTransform>());

            var title = UIFactory.Label("Title", _mainMenu.transform, "ArenaCraft", 80, new Color(0.95f, 0.75f, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(900f, 110f));
            var subtitle = UIFactory.Label("Sub", _mainMenu.transform, "Local 2-Player Battle Royale", 26, new Color(0.8f, 0.7f, 0.6f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(900f, 40f));

            var start = UIFactory.Button("Start", _mainMenu.transform, "Start Match", 26, new Color(0.55f, 0.30f, 0.18f), Color.white);
            UIFactory.Anchor(start.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(360f, 64f));
            start.onClick.AddListener(() => _game.StartMatch());

            var options = UIFactory.Button("Options", _mainMenu.transform, "Options", 26, new Color(0.40f, 0.35f, 0.30f), Color.white);
            UIFactory.Anchor(options.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(360f, 64f));
            options.onClick.AddListener(() => ShowOptions(true));

            var quit = UIFactory.Button("Quit", _mainMenu.transform, "Quit", 26, new Color(0.35f, 0.25f, 0.22f), Color.white);
            UIFactory.Anchor(quit.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(360f, 64f));
            quit.onClick.AddListener(() => _game.QuitGame());

            var hint = UIFactory.Label("Hint", _mainMenu.transform, "W/S or ↑/↓ to navigate • Space/Enter to select", 18, new Color(0.6f, 0.55f, 0.5f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(800f, 30f));

            _menuNav = _mainMenu.AddComponent<MenuNavigator>();
            _menuNav.SetButtons(new[] { start, options, quit });
        }

        public void ShowMainMenu(bool show)
        {
            _mainMenu.SetActive(show);
            _menuNav.SetActive(show);
        }

        // ---------------- Options ----------------
        private void BuildOptions()
        {
            _options = UIFactory.Panel("Options", _canvas.transform, new Color(0.10f, 0.08f, 0.07f, 0.98f));
            UIFactory.Stretch(_options.GetComponent<RectTransform>());

            var title = UIFactory.Label("Title", _options.transform, "Options", 50, new Color(0.95f, 0.75f, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(700f, 70f));

            // Resource phase duration adjuster (GDD 2.3 adjustable round timer).
            _timerValueLabel = UIFactory.Label("TimerVal", _options.transform, "", 24, Color.white, TextAnchor.MiddleCenter);
            UIFactory.Anchor(_timerValueLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(700f, 36f));

            var minus = UIFactory.Button("Minus", _options.transform, "-30s", 22, new Color(0.4f, 0.35f, 0.3f), Color.white);
            UIFactory.Anchor(minus.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-150f, 80f), new Vector2(120f, 54f));
            minus.onClick.AddListener(() => AdjustTimer(-30f));

            var plus = UIFactory.Button("Plus", _options.transform, "+30s", 22, new Color(0.4f, 0.35f, 0.3f), Color.white);
            UIFactory.Anchor(plus.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150f, 80f), new Vector2(120f, 54f));
            plus.onClick.AddListener(() => AdjustTimer(30f));

            // Controls reference (GDD 5.2).
            var controls = UIFactory.Label("Controls", _options.transform,
                "Controls\n" +
                "P1:  Move W A S D   •   Attack Space   •   Interact / Shop F\n" +
                "P2:  Move Arrow Keys   •   Attack Enter   •   Interact / Shop Right-Shift",
                20, new Color(0.85f, 0.8f, 0.75f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(controls.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(900f, 120f));

            var back = UIFactory.Button("Back", _options.transform, "Back", 24, new Color(0.55f, 0.30f, 0.18f), Color.white);
            UIFactory.Anchor(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -180f), new Vector2(300f, 60f));
            back.onClick.AddListener(() => ShowOptions(false));

            _optionsNav = _options.AddComponent<MenuNavigator>();
            _optionsNav.SetButtons(new[] { minus, plus, back });

            RefreshTimerLabel();
            _options.SetActive(false);
        }

        private void AdjustTimer(float delta)
        {
            _settings.resourcePhaseDuration = Mathf.Clamp(_settings.resourcePhaseDuration + delta, 30f, 600f);
            RefreshTimerLabel();
        }

        private void RefreshTimerLabel()
        {
            int s = Mathf.RoundToInt(_settings.resourcePhaseDuration);
            _timerValueLabel.text = $"Resource Phase Length:  {s / 60:0}:{s % 60:00}";
        }

        public void ShowOptions(bool show)
        {
            _options.SetActive(show);
            _optionsNav.SetActive(show);
            // While in options the main menu nav should pause if menu is open.
            if (_menuNav != null && _mainMenu.activeSelf) _menuNav.SetActive(!show);
        }

        // ---------------- Tutorial ----------------
        private void BuildTutorial(PlayerConfig[] configs)
        {
            _tutorial = UIFactory.Panel("Tutorial", _canvas.transform, new Color(0f, 0f, 0f, 0.85f));
            UIFactory.Stretch(_tutorial.GetComponent<RectTransform>());

            var title = UIFactory.Label("Title", _tutorial.transform, "How to Play", 54, new Color(0.95f, 0.75f, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 280f), new Vector2(900f, 80f));

            var body = UIFactory.Label("Body", _tutorial.transform,
                "1. RESOURCE PHASE — Attack trees, stone and metal nodes to harvest\n" +
                "   resources. They auto-convert to gold. Fill your bar before time runs out!\n\n" +
                "2. SHOPPING PHASE — Spend your gold on weapons (more damage) and\n" +
                "   armor (more HP). Don't go into battle empty-handed!\n\n" +
                "3. BATTLE ROYALE — Last gladiator standing wins the round.\n\n" +
                "Controls\n" +
                "P1:  Move W A S D   •   Attack Space   •   Interact / Shop F\n" +
                "P2:  Move Arrow Keys   •   Attack Enter   •   Interact / Shop Right-Shift",
                24, Color.white, TextAnchor.MiddleCenter);
            UIFactory.Anchor(body.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1100f, 420f));

            var go = UIFactory.Button("Begin", _tutorial.transform, "Begin!  (Space / Enter)", 26, new Color(0.55f, 0.30f, 0.18f), Color.white);
            UIFactory.Anchor(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(420f, 64f));
            go.onClick.AddListener(() => _game.DismissTutorial());

            var nav = _tutorial.AddComponent<MenuNavigator>();
            nav.SetButtons(new[] { go });

            _tutorial.SetActive(false);
        }

        public void ShowTutorial(bool show)
        {
            _tutorial.SetActive(show);
            var nav = _tutorial.GetComponent<MenuNavigator>();
            if (nav != null) nav.SetActive(show);
        }

        // ---------------- Victory ----------------
        private void BuildVictory()
        {
            _victory = UIFactory.Panel("Victory", _canvas.transform, new Color(0.08f, 0.06f, 0.05f, 0.95f));
            UIFactory.Stretch(_victory.GetComponent<RectTransform>());

            _victoryTitle = UIFactory.Label("Title", _victory.transform, "", 70, new Color(0.95f, 0.8f, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(_victoryTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1000f, 100f));

            _victoryScore = UIFactory.Label("Score", _victory.transform, "", 34, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(_victoryScore.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(900f, 50f));

            var rematch = UIFactory.Button("Rematch", _victory.transform, "Rematch", 28, new Color(0.55f, 0.30f, 0.18f), Color.white);
            UIFactory.Anchor(rematch.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(360f, 64f));
            rematch.onClick.AddListener(() => _game.Rematch());

            var menu = UIFactory.Button("Menu", _victory.transform, "Main Menu", 28, new Color(0.40f, 0.35f, 0.30f), Color.white);
            UIFactory.Anchor(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(360f, 64f));
            menu.onClick.AddListener(() => _game.ReturnToMenu());

            _victoryNav = _victory.AddComponent<MenuNavigator>();
            _victoryNav.SetButtons(new[] { rematch, menu });

            _victory.SetActive(false);
        }

        public void ShowVictory(bool show, string winnerLabel = "", string scoreLine = "")
        {
            _victory.SetActive(show);
            _victoryNav.SetActive(show);
            if (show)
            {
                _victoryTitle.text = winnerLabel;
                _victoryScore.text = scoreLine;
            }
        }

        private void Update()
        {
            if (_announcementTimer > 0f)
            {
                _announcementTimer -= Time.deltaTime;
                if (_announcementTimer <= 0f) _announcement.gameObject.SetActive(false);
            }
        }
    }
}
