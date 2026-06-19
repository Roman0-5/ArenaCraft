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
        [Header("Spawn Points Battle Pit")]
        [SerializeField] private Transform m_SpawnPointP1;
        [SerializeField] private Transform m_SpawnPointP2;
        [Header("Playable Bounds")]
        [Tooltip("Optional. If set, the Resource-phase bounds match this collider exactly (e.g. an invisible box around the fences). Overrides Resource Bounds Center/Size below.")]
        [SerializeField] private Collider m_ResourceBoundsCollider;
        [SerializeField] private Vector2 m_ResourceBoundsCenter = new Vector2(0f, 2f);
        [SerializeField] private Vector2 m_ResourceBoundsSize = new Vector2(42f, 42f);
        [SerializeField] private Vector2 m_ShopBoundsCenter = new Vector2(0f, 18f);
        [SerializeField] private Vector2 m_ShopBoundsSize = new Vector2(12f, 12f);
        [SerializeField] private float m_BattleBoundsPadding = 18f;
        [Header("Audio")]
        public AudioClip phaseStartSound;

        [CreateProperty]
        public GamePhase CurrentPhase { get; private set; } = GamePhase.None;

        [CreateProperty]
        public float PhaseTimer { get; private set; }

        public event Action<GamePhase> OnPhaseChanged;
        private AudioSource m_AudioSource;
        private bool m_PhaseSkipRequested;
        private bool m_GameLoopStarted;
        private ArenaBoundsController m_BoundsController;

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
            this.m_BoundsController = GetComponent<ArenaBoundsController>();
            if (this.m_BoundsController == null)
                this.m_BoundsController = gameObject.AddComponent<ArenaBoundsController>();

            Camera arenaCamera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (arenaCamera != null && arenaCamera.GetComponent<ArenaCameraController>() == null)
                arenaCamera.gameObject.AddComponent<ArenaCameraController>();
            if (arenaCamera != null && arenaCamera.GetComponent<SplitScreenManager>() == null)
                arenaCamera.gameObject.AddComponent<SplitScreenManager>();
        }

        private void Start()
        {
            StartCoroutine(DelayedBeginMatch());
        }

        private IEnumerator DelayedBeginMatch()
        {
            yield return null; // einen Frame warten → alle Start() sind durch
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
            yield return null;
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
            this.m_PhaseSkipRequested = false;
            this.CurrentPhase = phase;
            this.PhaseTimer = duration;
            this.OnPhaseChanged?.Invoke(phase);

            if (this.phaseStartSound != null) this.m_AudioSource.PlayOneShot(this.phaseStartSound);

            if (phase == GamePhase.Resource)
            {
                Bounds resourceBounds = this.m_ResourceBoundsCollider != null
                    ? this.m_ResourceBoundsCollider.bounds
                    : ToWorldBounds(this.m_ResourceBoundsCenter, this.m_ResourceBoundsSize);
                this.m_BoundsController.Configure(resourceBounds, "Resource Arena");
                SetPlayerControlsEnabled(true);
            }
            else if (phase == GamePhase.Shopping)
            {
                SetPlayerControlsEnabled(false);
                this.m_BoundsController.Configure(
                    ToWorldBounds(this.m_ShopBoundsCenter, this.m_ShopBoundsSize),
                    "Shop");
                TeleportPlayersToShop();
            }
            else if (phase == GamePhase.BattleRoyale)
            {
                Time.timeScale = 1f;
                if (ShopController.Instance != null) ShopController.Instance.ForceCloseAll();
                TeleportPlayersToBattlePit();
                this.m_BoundsController.Configure(CreateBattleBounds(), "Battle Arena");
                SetPlayerControlsEnabled(true);
            }
        }
 
        private void TeleportPlayersToShop()
        {
            GameObject shopZone = GameObject.Find("ShopZone");
            if (shopZone == null) return;

            var players = UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            System.Array.Sort(players, (a, b) => ((int)a.Slot).CompareTo((int)b.Slot));
            if (ShopController.Instance != null)
                ShopController.Instance.BeginShoppingSession(players.Length);

            foreach (var p in players)
            {
                float side = p.Slot == PlayerSlot.One ? -1.5f : 1.5f;
                MovePlayer(p, shopZone.transform.position + Vector3.right * side + Vector3.up * 1.5f);

                if (ShopController.Instance != null)
                {
                    ShopController.Instance.OpenShop(
                        p.GetComponent<PlayerInventory>(),
                        p.GetComponent<Health>(),
                        p.GetComponent<MeleeAttack>(),
                        p.GetComponent<ShieldBlock>());
                }
            }
        }


        private void TeleportPlayersToBattlePit()
        {
            if (m_SpawnPointP1 == null || m_SpawnPointP2 == null)
            {
                GameObject spawnOne = GameObject.Find("ArenaSpawn1");
                GameObject spawnTwo = GameObject.Find("ArenaSpawn2");
                this.m_SpawnPointP1 = spawnOne != null ? spawnOne.transform : null;
                this.m_SpawnPointP2 = spawnTwo != null ? spawnTwo.transform : null;
                if (this.m_SpawnPointP1 == null || this.m_SpawnPointP2 == null)
                {
                    Debug.LogError("[BattlePit] Spawn points are missing.");
                    return;
                }
            }

            var players = UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            System.Array.Sort(players, (a, b) => ((int)a.Slot).CompareTo((int)b.Slot));

            foreach (var p in players)
            {
                Transform spawnPoint = p.Slot == PlayerSlot.One ? m_SpawnPointP1 : m_SpawnPointP2;
                MovePlayer(p, spawnPoint.position, spawnPoint.rotation);
            }

            Physics.SyncTransforms();
        }

        public void SkipToNextPhase()
        {
            if (this.CurrentPhase == GamePhase.Resource || this.CurrentPhase == GamePhase.Shopping)
                this.m_PhaseSkipRequested = true;
        }

        public void RequestShoppingComplete()
        {
            if (this.CurrentPhase != GamePhase.Shopping || this.m_PhaseSkipRequested)
                return;

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

        private static void MovePlayer(
            PlayerInputProvider provider,
            Vector3 position,
            Quaternion? rotation = null)
        {
            Rigidbody body = provider.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                provider.transform.SetPositionAndRotation(
                    position,
                    rotation ?? provider.transform.rotation);
                body.position = position;
                if (rotation.HasValue) body.rotation = rotation.Value;
            }
            else
            {
                provider.transform.position = position;
                if (rotation.HasValue) provider.transform.rotation = rotation.Value;
            }
        }

        private static Bounds ToWorldBounds(Vector2 center, Vector2 size)
        {
            return new Bounds(
                new Vector3(center.x, 0f, center.y),
                new Vector3(Mathf.Max(2f, size.x), 20f, Mathf.Max(2f, size.y)));
        }

        private Bounds CreateBattleBounds()
        {
            if (this.m_SpawnPointP1 == null || this.m_SpawnPointP2 == null)
                return new Bounds(Vector3.zero, new Vector3(50f, 20f, 50f));

            Vector3 first = this.m_SpawnPointP1.position;
            Vector3 second = this.m_SpawnPointP2.position;
            Vector3 center = (first + second) * 0.5f;
            Vector3 separation = new Vector3(
                Mathf.Abs(first.x - second.x),
                0f,
                Mathf.Abs(first.z - second.z));
            return new Bounds(
                new Vector3(center.x, center.y, center.z),
                new Vector3(
                    Mathf.Max(30f, separation.x + this.m_BattleBoundsPadding * 2f),
                    24f,
                    Mathf.Max(30f, separation.z + this.m_BattleBoundsPadding * 2f)));
        }
    }
}
