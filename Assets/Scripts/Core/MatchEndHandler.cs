using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ArenaCraft
{
    public class MatchEndHandler : MonoBehaviour
    {
        public VisualTreeAsset victoryUxml;
        public PanelSettings panelSettings;

        private bool m_GameEnded;
        private UIDocument m_VictoryDoc;

        private void Update()
        {
            if (this.m_GameEnded || GamePhaseManager.Instance == null || GamePhaseManager.Instance.CurrentPhase != GamePhase.BattleRoyale)
                return;

            CheckWinCondition();
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
            // Hide HUD
            var hud = UnityEngine.Object.FindAnyObjectByType<HUDController>();
            if (hud != null) hud.GetComponent<UIDocument>().enabled = false;

            GameObject victoryObj = new GameObject("VictoryUI");
            this.m_VictoryDoc = victoryObj.AddComponent<UIDocument>();
            this.m_VictoryDoc.panelSettings = this.panelSettings;
            this.m_VictoryDoc.visualTreeAsset = this.victoryUxml;
            this.m_VictoryDoc.sortingOrder = 300;

            var root = this.m_VictoryDoc.rootVisualElement;
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;
            root.Q<Label>("winner-label").text = winner == "DRAW" ? "IT'S A DRAW!" : winner.ToUpper() + " WINS!";
            root.Q<Button>("rematch-button").clicked += () => SceneManager.LoadScene("SampleScene");
            root.Q<Button>("main-menu-button").clicked += () => SceneManager.LoadScene("MainMenu");

            foreach (var provider in Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None))
                provider.enabled = false;
        }

        private static string GetPlayerName(Health health)
        {
            var provider = health.GetComponent<PlayerInputProvider>();
            if (provider == null) return health.name;
            return provider.Slot == PlayerSlot.One ? "PLAYER 1" : "PLAYER 2";
        }
    }
}
