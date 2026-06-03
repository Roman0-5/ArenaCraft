using System.Collections.Generic;
using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// The match state machine and central coordinator. Owns the three-phase flow
    /// from GDD 2.1.1 (Resource -> Shopping -> Battle Royale -> Victory), builds and
    /// tears down the world each match, tracks per-session win counts (FCR5) and
    /// routes all UI transitions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameSettings Settings { get; private set; }
        public GamePhase Phase { get; private set; } = GamePhase.MainMenu;

        private PlayerConfig[] _configs;
        private PlayerStats[] _stats;
        private ResourceTypeDef[] _resourceTypes;
        private readonly List<PlayerController> _players = new List<PlayerController>();
        private List<ResourceNode> _nodes = new List<ResourceNode>();

        private UIManager _ui;
        private Camera _camera;
        private ArenaCamera _arenaCam;
        private Transform _matchRoot;

        private readonly int[] _winCounts = new int[2];
        private bool _firstMatch = true;

        private float _phaseTimer;        // counts down for resource/shopping phases
        private float _battleElapsed;     // counts up during battle royale
        private float _endDelay;          // settle time before showing the victory screen
        private bool _awaitingTutorial;

        public void Initialize(GameSettings settings, UIManager ui, Camera cam, ArenaCamera arenaCam)
        {
            Instance = this;
            Settings = settings;
            _ui = ui;
            _camera = cam;
            _arenaCam = arenaCam;

            _resourceTypes = ResourceTypeDef.DefaultSet();
            _configs = new[] { PlayerConfig.Player1(), PlayerConfig.Player2() };
            _stats = new[] { new PlayerStats(settings), new PlayerStats(settings) };

            _ui.Build(this, settings, _configs, _stats);

            EnterMainMenu();
        }

        public PlayerConfig[] Configs => _configs;
        public PlayerStats[] Stats => _stats;

        // ----------------------------------------------------------------- Update
        private void Update()
        {
            switch (Phase)
            {
                case GamePhase.ResourcePhase:
                    if (_awaitingTutorial) break;
                    _phaseTimer -= Time.deltaTime;
                    _ui.SetTimer("Resource Phase", _phaseTimer, true);
                    if (_phaseTimer <= 0f || AllResourceBarsFull())
                        BeginShoppingPhase();
                    break;

                case GamePhase.ShoppingPhase:
                    _phaseTimer -= Time.deltaTime;
                    _ui.SetTimer("Shopping Phase", _phaseTimer, true);
                    _ui.UpdateShopWarning(_phaseTimer <= Settings.noPurchaseWarningAt);
                    if (_phaseTimer <= 0f || _ui.AllPlayersReady())
                        BeginBattleRoyale();
                    break;

                case GamePhase.BattleRoyale:
                    _battleElapsed += Time.deltaTime;
                    _ui.SetTimer("Battle Royale", _battleElapsed, true, urgent: false);
                    UpdateBattle();
                    break;
            }

            // Esc backs out to the main menu from gameplay / victory.
            if (Input.GetKeyDown(KeyCode.Escape) && Phase != GamePhase.MainMenu)
                ReturnToMenu();
        }

        private bool AllResourceBarsFull()
        {
            foreach (var s in _stats) if (!s.ResourceBarFull) return false;
            return true;
        }

        private void UpdateBattle()
        {
            int alive = 0;
            PlayerController survivor = null;
            foreach (var p in _players)
            {
                if (p != null && p.IsAlive) { alive++; survivor = p; }
            }

            if (alive <= 1)
            {
                _endDelay -= Time.deltaTime;
                if (_endDelay <= 0f)
                    EndMatch(alive == 1 ? survivor : null);
            }
            else
            {
                _endDelay = 0.7f; // reset settle timer while >1 alive
            }
        }

        // ----------------------------------------------------------------- Phases
        private void EnterMainMenu()
        {
            Phase = GamePhase.MainMenu;
            AudioManager.Instance?.StopMusic();
            ClearWorld();
            _ui.SetHudVisible(false);
            _ui.ShowShop(false);
            _ui.ShowVictory(false);
            _ui.ShowTutorial(false);
            _ui.SetTimer("", 0f, false);
            _ui.ShowMainMenu(true);
            Cursor.visible = true;
        }

        public void StartMatch()
        {
            _ui.ShowMainMenu(false);
            _ui.ShowOptions(false);
            BuildWorld();
            _ui.SetHudVisible(true);

            if (_firstMatch && Settings.showTutorialOnFirstMatch)
            {
                // Freeze in the resource phase until the tutorial is dismissed.
                Phase = GamePhase.ResourcePhase;
                _awaitingTutorial = true;
                _phaseTimer = Settings.resourcePhaseDuration;
                SetPlayersInput(false);
                _ui.SetTimer("Resource Phase", _phaseTimer, true);
                _ui.ShowTutorial(true);
            }
            else
            {
                BeginResourcePhase();
            }
        }

        public void DismissTutorial()
        {
            _firstMatch = false;
            _awaitingTutorial = false;
            _ui.ShowTutorial(false);
            BeginResourcePhase();
        }

        private void BeginResourcePhase()
        {
            Phase = GamePhase.ResourcePhase;
            _awaitingTutorial = false;
            _phaseTimer = Settings.resourcePhaseDuration;
            SetPlayersInput(true);
            _ui.ShowShop(false);
            _ui.Announce("Resource Phase — Harvest!", 2.2f);
            AudioManager.Instance?.PlayCalmMusic();
            AudioManager.Instance?.PlayTransition();
        }

        private void BeginShoppingPhase()
        {
            Phase = GamePhase.ShoppingPhase;
            _phaseTimer = Settings.shoppingPhaseDuration;
            SetPlayersInput(false);   // players interact with the shop, not the arena
            PositionPlayersOnRing(Settings.arenaRadius * 0.55f);
            _ui.ShowShop(true);
            _ui.Announce("Shopping Phase — Spend your gold!", 2f);
            AudioManager.Instance?.PlayTransition();
        }

        private void BeginBattleRoyale()
        {
            Phase = GamePhase.BattleRoyale;
            _battleElapsed = 0f;
            _endDelay = 0.7f;
            _ui.ShowShop(false);

            // Reset arena state for combat: full HP, spread to opposite sides.
            PositionPlayersOnRing(Settings.arenaRadius * 0.6f, faceCenter: true);
            foreach (var s in _stats) s.FullHeal();
            SetPlayersInput(true);

            _ui.Announce("BATTLE ROYALE — Last one standing wins!", 2.6f);
            AudioManager.Instance?.PlayBattleMusic();
            AudioManager.Instance?.PlayTransition();
        }

        private void EndMatch(PlayerController winner)
        {
            Phase = GamePhase.GameOver;
            SetPlayersInput(false);
            AudioManager.Instance?.StopMusic();
            AudioManager.Instance?.PlayVictory();

            string title;
            if (winner != null && winner.Config != null)
            {
                int idx = winner.Config.playerId - 1;
                if (idx >= 0 && idx < _winCounts.Length) _winCounts[idx]++;
                title = $"{winner.Config.label}  WINS!";
            }
            else
            {
                title = "DRAW!";
            }

            string score = $"P1 Wins: {_winCounts[0]}   |   P2 Wins: {_winCounts[1]}";
            _ui.SetTimer("", 0f, false);
            _ui.ShowVictory(true, title, score);
        }

        public void Rematch()
        {
            _ui.ShowVictory(false);
            BuildWorld();              // fresh nodes, fresh stats; win counts persist
            _ui.SetHudVisible(true);
            BeginResourcePhase();
        }

        public void ReturnToMenu()
        {
            _ui.ShowVictory(false);
            _ui.ShowShop(false);
            _ui.ShowTutorial(false);
            EnterMainMenu();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ----------------------------------------------------------------- World
        private void BuildWorld()
        {
            ClearWorld();

            _matchRoot = new GameObject("Match").transform;

            ArenaBuilder.Build(Settings, _matchRoot);
            _arenaCam.Setup(_camera, Settings.arenaRadius);

            _nodes = ResourceNodeSpawner.Spawn(Settings, _resourceTypes, _matchRoot);

            _players.Clear();
            var spawns = ArenaBuilder.RingPoints(Settings.arenaRadius * 0.5f, _configs.Length, 0f);
            for (int i = 0; i < _configs.Length; i++)
            {
                _stats[i].ResetForNewMatch();
                var pc = PlayerFactory.Create(_configs[i], _stats[i], Settings, _matchRoot, spawns[i]);
                _players.Add(pc);
            }
        }

        private void ClearWorld()
        {
            if (_matchRoot != null) Destroy(_matchRoot.gameObject);
            _matchRoot = null;
            _players.Clear();
            _nodes.Clear();
        }

        private void SetPlayersInput(bool enabled)
        {
            foreach (var p in _players)
            {
                if (p == null) continue;
                p.InputEnabled = enabled;
                p.MovementEnabled = enabled;
            }
        }

        private void PositionPlayersOnRing(float radius, bool faceCenter = false)
        {
            var pts = ArenaBuilder.RingPoints(radius, _players.Count, 0f);
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i] == null) continue;
                Vector3 pos = pts[i];
                Vector3 face = faceCenter ? (-pos).normalized : Vector3.forward;
                _players[i].Teleport(pos, face);
            }
        }
    }
}
