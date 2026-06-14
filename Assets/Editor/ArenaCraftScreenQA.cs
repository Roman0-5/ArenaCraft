using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ArenaCraft.Editor
{
    internal static class ArenaCraftScreenQA
    {
        [MenuItem("ArenaCraft/Screen QA/Open Settings")]
        private static void OpenSettings()
        {
            Object.FindAnyObjectByType<SettingsUIController>()?.OpenMenu();
        }

        [MenuItem("ArenaCraft/Screen QA/Close Settings")]
        private static void CloseSettings()
        {
            Object.FindAnyObjectByType<SettingsUIController>()?.CloseMenu();
        }

        [MenuItem("ArenaCraft/Screen QA/Load Gameplay")]
        private static void LoadGameplay()
        {
            SceneNavigation.LoadGame();
        }

        [MenuItem("ArenaCraft/Screen QA/Show Pause")]
        private static void ShowPause()
        {
            Object.FindAnyObjectByType<PauseMenuController>()?.PauseGame();
        }

        [MenuItem("ArenaCraft/Screen QA/Show Shop")]
        private static void ShowShop()
        {
            EnterPhase(GamePhase.Shopping, 60f);
        }

        [MenuItem("ArenaCraft/Screen QA/Show Victory")]
        private static void ShowVictory()
        {
            EnterPhase(GamePhase.BattleRoyale, 0f);
            PlayerInputProvider[] players = Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            foreach (PlayerInputProvider player in players)
            {
                if (player.Slot == PlayerSlot.Two)
                {
                    player.GetComponent<Health>()?.TakeDamage(10000f);
                    return;
                }
            }
        }

        [MenuItem("ArenaCraft/Screen QA/Demo Resource Respawn")]
        private static void DemoResourceRespawn()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ArenaCraft QA] Enter Play Mode before running the resource respawn demo.");
                return;
            }

            PlayerInputProvider player = null;
            foreach (PlayerInputProvider candidate in
                     Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None))
            {
                if (candidate.Slot == PlayerSlot.One)
                {
                    player = candidate;
                    break;
                }
            }

            if (player == null) return;

            ResourceNode closest = null;
            float closestDistance = float.PositiveInfinity;
            foreach (ResourceNode node in Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (!node.CanHarvest) continue;
                float distance = Vector3.SqrMagnitude(node.transform.position - player.transform.position);
                if (distance >= closestDistance) continue;
                closest = node;
                closestDistance = distance;
            }

            if (closest == null) return;

            Rigidbody body = player.GetComponent<Rigidbody>();
            Vector3 previewPosition = closest.transform.position + Vector3.left * 2.2f + Vector3.up * 0.5f;
            if (body != null) body.position = previewPosition;
            else player.transform.position = previewPosition;

            closest.respawnTime = 3f;
            closest.respawnVariance = 0f;
            closest.respawnWarningTime = 1.5f;
            closest.TakeDamage(closest.maxHealth, player.GetComponent<PlayerInventory>());
            Debug.Log($"[ArenaCraft QA] Depleted {closest.name}; it will respawn in three seconds.");
        }

        private static void EnterPhase(GamePhase phase, float duration)
        {
            GamePhaseManager manager = Object.FindAnyObjectByType<GamePhaseManager>();
            if (manager == null) return;

            MethodInfo enterPhase = typeof(GamePhaseManager).GetMethod(
                "EnterPhase",
                BindingFlags.Instance | BindingFlags.NonPublic);
            enterPhase?.Invoke(manager, new object[] { phase, duration });
        }
    }
}
