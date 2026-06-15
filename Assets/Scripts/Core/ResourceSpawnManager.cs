using UnityEngine;

namespace ArenaCraft
{
    public class ResourceSpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform[] m_SpawnPoints;

        private void Awake()
        {
            if (GamePhaseManager.Instance != null)
            {
                GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
                Debug.Log("[ResourceSpawn] Erfolgreich beim GamePhaseManager registriert");
            }
            else
            {
                Debug.LogWarning("[ResourceSpawn] GamePhaseManager.Instance ist NULL in Awake!");
            }
        }

        private void OnDestroy()
        {
            if (GamePhaseManager.Instance != null)
                GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Resource)
                SpawnPlayers();
        }

        public void SpawnPlayers()
        {
            Debug.Log("[ResourceSpawn] SpawnPlayers() aufgerufen");

            if (m_SpawnPoints == null || m_SpawnPoints.Length < 2)
            {
                Debug.LogWarning($"[ResourceSpawn] Mindestens 2 Spawn Points nötig! Aktuell: {m_SpawnPoints?.Length ?? 0}");
                return;
            }

            var players = UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            Debug.Log($"[ResourceSpawn] {players.Length} Spieler gefunden");

            if (players.Length == 0)
            {
                Debug.LogWarning("[ResourceSpawn] Keine Spieler gefunden!");
                return;
            }

            System.Array.Sort(players, (a, b) => ((int)a.Slot).CompareTo((int)b.Slot));

            int indexP1 = Random.Range(0, m_SpawnPoints.Length);
            int indexP2;
            do
            {
                indexP2 = Random.Range(0, m_SpawnPoints.Length);
            } while (indexP2 == indexP1);

            Debug.Log($"[ResourceSpawn] P1 → Spawn Index {indexP1} ({m_SpawnPoints[indexP1].position})");
            Debug.Log($"[ResourceSpawn] P2 → Spawn Index {indexP2} ({m_SpawnPoints[indexP2].position})");

            foreach (var p in players)
            {
                Transform spawnPoint = p.Slot == PlayerSlot.One
                    ? m_SpawnPoints[indexP1]
                    : m_SpawnPoints[indexP2];

                Debug.Log($"[ResourceSpawn] Spawne {p.Slot} auf {spawnPoint.position}");
                MovePlayer(p, spawnPoint.position);
                p.transform.rotation = spawnPoint.rotation;
                Debug.Log($"[ResourceSpawn] {p.Slot} ist jetzt bei {p.transform.position}");
            }
        }

        private static void MovePlayer(PlayerInputProvider provider, Vector3 position)
        {
            Rigidbody body = provider.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.position = position;
            }
            else
            {
                provider.transform.position = position;
            }
        }
    }
}