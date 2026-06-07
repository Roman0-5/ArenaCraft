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
                string winnerName = alivePlayers.Count == 1 ? alivePlayers[0].name : "DRAW";
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

            var root = this.m_VictoryDoc.rootVisualElement;
            root.Q<Label>("winner-label").text = winner == "DRAW" ? "IT'S A DRAW!" : winner.ToUpper() + " WINS!";
            root.Q<Button>("rematch-button").clicked += () => SceneManager.LoadScene("SampleScene");
        }
}
}
