using UnityEngine;
using System;
using System.Collections;
using Unity.Properties;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

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
        private bool m_PhaseSkipRequested;
        private bool m_GameLoopStarted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            this.m_AudioSource = gameObject.AddComponent<AudioSource>();
            this.m_AudioSource.playOnAwake = false;

            Camera arenaCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (arenaCamera != null && arenaCamera.GetComponent<ArenaCameraController>() == null)
                arenaCamera.gameObject.AddComponent<ArenaCameraController>();
            if (arenaCamera != null && arenaCamera.GetComponent<SplitScreenManager>() == null)
                arenaCamera.gameObject.AddComponent<SplitScreenManager>();
        }

        private void Start()
        {
            BeginMatch();
        }

        public void BeginMatch()
        {
            if (this.m_GameLoopStarted)
                return;

            Time.timeScale = 1f;
            this.m_GameLoopStarted = true;
            this.m_ResourcePhaseTime = MatchRules.ResourcePhaseDuration;
            this.m_ShoppingPhaseTime = MatchRules.ShoppingPhaseDuration;
            StartCoroutine(GameLoop());
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
                this.SkipToNextPhase();
        }
#endif

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
            EnterPhase(phase, duration);

            if (duration > 0)
            {
                while (this.PhaseTimer > 0)
                {
                    if (this.m_PhaseSkipRequested || (phase == GamePhase.Resource && AreAllInventoriesFull()))
                        break;

                    this.PhaseTimer -= Time.deltaTime;
                    yield return null;
                }
                this.PhaseTimer = 0;
                this.m_PhaseSkipRequested = false;
            }
            else
            {
                // Permanent phase (Battle Royale)
                while (true) yield return null;
            }
        }

        private void EnterPhase(GamePhase phase, float duration)
        {
            this.CurrentPhase = phase;
            this.PhaseTimer = duration;
            this.OnPhaseChanged?.Invoke(phase);

            if (this.phaseStartSound != null) this.m_AudioSource.PlayOneShot(this.phaseStartSound);
            Debug.Log($"Phase Started: {phase}");

            if (phase == GamePhase.Resource)
            {
                SetPlayerControlsEnabled(true);
            }
            else if (phase == GamePhase.Shopping)
            {
                SetPlayerControlsEnabled(false);
                TeleportPlayersToShop();
            }
            else if (phase == GamePhase.BattleRoyale)
            {
                if (ShopController.Instance != null) ShopController.Instance.ForceCloseAll();
                TeleportPlayersToBattlePit();
                SetPlayerControlsEnabled(true);
            }
        }

        private void TeleportPlayersToShop()
        {
            GameObject shopZone = GameObject.Find("ShopZone");
            if (shopZone == null) return;

            var players = UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            System.Array.Sort(players, (a, b) => ((int)a.Slot).CompareTo((int)b.Slot));
            foreach (var p in players)
            {
                float side = p.Slot == PlayerSlot.One ? -1.5f : 1.5f;
                MovePlayer(p, shopZone.transform.position + Vector3.right * side + Vector3.up * 0.5f);

                if (ShopController.Instance != null)
                {
                    ShopController.Instance.OpenShop(
                        p.GetComponent<PlayerInventory>(),
                        p.GetComponent<Health>(),
                        p.GetComponent<MeleeAttack>());
                }
            }
        }

        private void TeleportPlayersToBattlePit()
        {
            GameObject pit = GameObject.Find("BattlePit");
            if (pit == null) return;

            var players = UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            System.Array.Sort(players, (a, b) => ((int)a.Slot).CompareTo((int)b.Slot));
            foreach (var p in players)
            {
                float side = p.Slot == PlayerSlot.One ? -2f : 2f;
                Vector3 position = pit.transform.position + Vector3.right * side + Vector3.up * 0.5f;
                MovePlayer(p, position);
                p.transform.rotation = Quaternion.LookRotation(pit.transform.position - new Vector3(position.x, pit.transform.position.y, position.z));
            }
        }

        public void SkipToNextPhase()
        {
            if (this.CurrentPhase == GamePhase.Resource || this.CurrentPhase == GamePhase.Shopping)
                this.m_PhaseSkipRequested = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static bool AreAllInventoriesFull()
        {
            var players = UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            if (players.Length == 0) return false;

            foreach (PlayerInputProvider player in players)
            {
                PlayerInventory inventory = player.GetComponent<PlayerInventory>();
                if (inventory == null || !inventory.IsFull) return false;
            }
            return true;
        }

        private static void SetPlayerControlsEnabled(bool enabled)
        {
            foreach (PlayerInputProvider provider in UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None))
            {
                provider.enabled = enabled;
                if (!enabled)
                {
                    Rigidbody body = provider.GetComponent<Rigidbody>();
                    if (body != null)
                    {
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                    }
                }
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
