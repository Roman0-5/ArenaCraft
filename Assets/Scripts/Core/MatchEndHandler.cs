using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace ArenaCraft
{
    public class MatchEndHandler : MonoBehaviour
    {
        public VisualTreeAsset victoryUxml;
        public PanelSettings panelSettings;

        private bool m_GameEnded;
        private UIDocument m_VictoryDoc;

        private void Awake()
        {
            Health.OnAnyDied += OnPlayerDied;
        }

        private void Update()
        {
            if (this.m_GameEnded || GamePhaseManager.Instance == null || GamePhaseManager.Instance.CurrentPhase != GamePhase.BattleRoyale)
                return;

            CheckWinCondition();
        }

        private void OnDestroy()
        {
            Health.OnAnyDied -= OnPlayerDied;
        }

        private void OnPlayerDied(Health player)
        {
            if (GamePhaseManager.Instance != null &&
                GamePhaseManager.Instance.CurrentPhase == GamePhase.BattleRoyale)
            {
                CheckWinCondition();
            }
        }

        private void CheckWinCondition()
        {
            var players = Object.FindObjectsByType<Health>(FindObjectsSortMode.None);
            List<Health> alivePlayers = new List<Health>();
            foreach (var p in players)
            {
                if (!p.IsDead) alivePlayers.Add(p);
            }

            if (alivePlayers.Count <= 1)
            {
                this.m_GameEnded = true;
                string winnerName = alivePlayers.Count == 1 ? GetPlayerName(alivePlayers[0]) : "DRAW";
                Debug.Log($"Game Over! Winner: {winnerName}");
                ShowVictoryScreen(winnerName);
            }
        }

        private void ShowVictoryScreen(string winner)
        {
            if (this.victoryUxml == null || this.panelSettings == null)
            {
                Debug.LogError("Victory screen assets are not assigned.", this);
                return;
            }

            // Hide HUD
            var hud = UnityEngine.Object.FindAnyObjectByType<HUDController>();
            if (hud != null) hud.SetVisible(false);

            GameObject victoryObj = new GameObject("VictoryUI");
            this.m_VictoryDoc = victoryObj.AddComponent<UIDocument>();
            this.m_VictoryDoc.panelSettings = this.panelSettings;
            this.m_VictoryDoc.visualTreeAsset = this.victoryUxml;
            this.m_VictoryDoc.sortingOrder = 300;

            var root = this.m_VictoryDoc.rootVisualElement;
            ResponsiveUILayout.Attach(root);
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            Label winnerLabel = root.Q<Label>("winner-label");
            Button rematchButton = root.Q<Button>("rematch-button");
            Button mainMenuButton = root.Q<Button>("main-menu-button");
            if (winnerLabel != null)
                winnerLabel.text = winner == "DRAW" ? "IT'S A DRAW!" : winner.ToUpper() + " WINS!";
            if (rematchButton != null) rematchButton.clicked += SceneNavigation.LoadGame;
            if (mainMenuButton != null) mainMenuButton.clicked += SceneNavigation.LoadMainMenu;

            foreach (var provider in Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None))
                provider.enabled = false;
        }

        public void Rematch() => SceneNavigation.LoadGame();
        public void ReturnToMainMenu() => SceneNavigation.LoadMainMenu();

        private static string GetPlayerName(Health health)
        {
            var provider = health.GetComponent<PlayerInputProvider>();
            if (provider == null) return health.name;
            return provider.Slot == PlayerSlot.One ? "PLAYER 1" : "PLAYER 2";
        }
    }
}
