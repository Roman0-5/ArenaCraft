using UnityEngine;
using System;
using System.Collections;
using Unity.Properties;

namespace ArenaCraft
{
    public enum GamePhase
    {
        None,
        Resource,
        Shopping,
        BattleRoyale
    }

    public class GamePhaseManager : MonoBehaviour
    {
        public static GamePhaseManager Instance { get; private set; }

        [Header("Phase Durations")]
        [SerializeField] private float m_ResourcePhaseTime = 180f;
        [SerializeField] private float m_ShoppingPhaseTime = 60f;
        public AudioClip phaseStartSound;

        [CreateProperty]
        public GamePhase CurrentPhase { get; private set; } = GamePhase.None;

        [CreateProperty]
        public float PhaseTimer { get; private set; }

        public event Action<GamePhase> OnPhaseChanged;
        private AudioSource m_AudioSource;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            this.m_AudioSource = gameObject.AddComponent<AudioSource>();
            this.m_AudioSource.playOnAwake = false;
        }

        private void Start()
        {
            StartCoroutine(GameLoop());
        }

        private IEnumerator GameLoop()
        {
            // Resource Phase
            yield return StartPhase(GamePhase.Resource, this.m_ResourcePhaseTime);

            // Shopping Phase
            yield return StartPhase(GamePhase.Shopping, this.m_ShoppingPhaseTime);

            // Battle Royale Phase
            yield return StartPhase(GamePhase.BattleRoyale, 0f);
        }

        private IEnumerator StartPhase(GamePhase phase, float duration)
        {
            this.CurrentPhase = phase;
            this.PhaseTimer = duration;
            this.OnPhaseChanged?.Invoke(phase);
            
            if (this.phaseStartSound != null) this.m_AudioSource.PlayOneShot(this.phaseStartSound);
            Debug.Log($"Phase Started: {phase}");

            if (duration > 0)
            {
                if (phase == GamePhase.Shopping)
                {
                    TeleportPlayersToShop();
                }

                while (this.PhaseTimer > 0)
                {
                    this.PhaseTimer -= Time.deltaTime;
                    yield return null;
                }
                this.PhaseTimer = 0;
            }
            else
            {
                if (phase == GamePhase.BattleRoyale)
                {
                    TeleportPlayersToBattlePit();
                }

                // Permanent phase (Battle Royale)
                while (true) yield return null;
            }
        }

        private void TeleportPlayersToShop()
        {
            GameObject shopZone = GameObject.Find("ShopZone");
            if (shopZone == null) return;

            var players = UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                p.transform.position = shopZone.transform.position + Vector3.up * 0.5f;
            }
        }

        private void TeleportPlayersToBattlePit()
        {
            GameObject pit = GameObject.Find("BattlePit");
            if (pit == null) return;

            var players = UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                p.transform.position = pit.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 2f;
                p.transform.position = new Vector3(p.transform.position.x, 0.5f, p.transform.position.z);
            }
        }

        public void SkipToNextPhase()
        {
            this.PhaseTimer = 0;
        }
    }
}
