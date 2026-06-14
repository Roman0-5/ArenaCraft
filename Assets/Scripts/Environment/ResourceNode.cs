using UnityEngine;
using System.Collections;
using System;

namespace ArenaCraft
{
    public class ResourceNode : MonoBehaviour
    {
        public enum NodeState
        {
            Available,
            Depleted,
            Respawning
        }

        public ResourceType resourceType;
        public int maxHealth = 3;
        public int resourcesPerHit = 10;
        [Tooltip("Extra resources awarded when the final hit fully depletes this node.")]
        public int depletionBonus = 5;
        public float respawnTime = 10f;
        [Tooltip("Random variation added to the respawn time so nearby nodes do not return together.")]
        public float respawnVariance = 1.5f;
        [Tooltip("Seconds before respawn when the visual begins growing back.")]
        public float respawnWarningTime = 1.5f;
        public GameObject visuals;
        public ParticleSystem harvestEffect;
        public AudioClip hitSound;

        private int m_CurrentHealth;
        private bool m_IsDestroyed;
        private AudioSource m_AudioSource;
        private Collider[] m_Colliders;
        private bool[] m_ColliderDefaults;
        private Coroutine m_HitFeedbackRoutine;
        private Coroutine m_RespawnRoutine;
        private Vector3 m_VisualStartPosition;
        private Vector3 m_VisualStartScale;
        private Vector3 m_IndicatorLocalPosition = new Vector3(0f, 2.5f, 0f);
        private bool m_Initialized;

        public int CurrentHealth => this.m_CurrentHealth;
        public bool IsDestroyed => this.m_IsDestroyed;
        public float HealthNormalized => this.maxHealth > 0 ? (float)this.m_CurrentHealth / this.maxHealth : 0f;
        public float RespawnRemaining { get; private set; }
        public float RespawnDuration { get; private set; }
        public float RespawnNormalized => this.RespawnDuration > 0f
            ? 1f - Mathf.Clamp01(this.RespawnRemaining / this.RespawnDuration)
            : 1f;
        public NodeState State { get; private set; } = NodeState.Available;
        public int TotalYield => this.maxHealth * this.resourcesPerHit + this.depletionBonus;
        public bool CanHarvest => this.State == NodeState.Available && !this.m_IsDestroyed;
        public Vector3 IndicatorWorldPosition => transform.TransformPoint(this.m_IndicatorLocalPosition);

        public event Action<ResourceNode, PlayerInventory, int> OnHarvested;
        public event Action<ResourceNode> OnDepleted;
        public event Action<ResourceNode> OnRespawned;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (this.m_Initialized)
            {
                FitBlockingColliderToVisuals();
                if (Application.isPlaying)
                    ResourceNodeIndicator.Attach(this);
                return;
            }

            this.maxHealth = Mathf.Max(1, this.maxHealth);
            this.resourcesPerHit = Mathf.Max(1, this.resourcesPerHit);
            this.depletionBonus = Mathf.Max(0, this.depletionBonus);
            this.respawnTime = Mathf.Max(0f, this.respawnTime);
            this.respawnVariance = Mathf.Max(0f, this.respawnVariance);
            this.respawnWarningTime = Mathf.Max(0f, this.respawnWarningTime);
            this.m_CurrentHealth = this.maxHealth;
            FitBlockingColliderToVisuals();
            this.m_Colliders = GetComponentsInChildren<Collider>(true);
            this.m_ColliderDefaults = new bool[this.m_Colliders.Length];
            for (int i = 0; i < this.m_Colliders.Length; i++)
                this.m_ColliderDefaults[i] = this.m_Colliders[i].enabled;
            this.m_AudioSource = GetComponent<AudioSource>();
            if (this.m_AudioSource == null) this.m_AudioSource = gameObject.AddComponent<AudioSource>();
            this.m_AudioSource.playOnAwake = false;
            this.m_AudioSource.spatialBlend = 1.0f;

            if (this.visuals != null)
            {
                this.m_VisualStartPosition = this.visuals.transform.localPosition;
                this.m_VisualStartScale = this.visuals.transform.localScale;
                ApplyResourceTint();
            }

            this.m_Initialized = true;
            if (Application.isPlaying)
                ResourceNodeIndicator.Attach(this);
        }

        private void ApplyResourceTint()
        {
            if (this.visuals == null || this.resourceType != ResourceType.Metal) return;

            Color oreColor = new Color(0.16f, 0.48f, 0.72f, 1f);
            foreach (Renderer renderer in this.visuals.GetComponentsInChildren<Renderer>(true))
            {
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", oreColor);
                properties.SetColor("_Color", oreColor);
                renderer.SetPropertyBlock(properties);
            }
        }

        public void FitBlockingColliderToVisuals()
        {
            if (this.visuals == null || !TryGetVisualLocalBounds(out Bounds bounds)) return;

            Collider blockingCollider = GetComponent<Collider>();
            if (blockingCollider == null)
            {
                blockingCollider = this.resourceType == ResourceType.Wood
                    ? gameObject.AddComponent<CapsuleCollider>()
                    : gameObject.AddComponent<BoxCollider>();
            }

            blockingCollider.isTrigger = false;
            float bottom = Mathf.Max(0f, bounds.min.y);
            if (blockingCollider is CapsuleCollider capsule)
            {
                float horizontalSize = Mathf.Min(bounds.size.x, bounds.size.z);
                capsule.direction = 1;
                capsule.radius = Mathf.Clamp(horizontalSize * 0.2f, 0.7f, 1.15f);
                capsule.height = Mathf.Clamp(bounds.size.y * 0.72f, 2.5f, 5.5f);
                capsule.center = new Vector3(
                    bounds.center.x,
                    bottom + capsule.height * 0.5f,
                    bounds.center.z);
                this.m_IndicatorLocalPosition = new Vector3(
                    capsule.center.x + capsule.radius + 0.65f,
                    bottom + capsule.height * 0.72f,
                    capsule.center.z);
            }
            else if (blockingCollider is BoxCollider box)
            {
                box.size = new Vector3(
                    Mathf.Clamp(bounds.size.x * 0.78f, 1.2f, 2.8f),
                    Mathf.Clamp(bounds.size.y * 0.82f, 1f, 3f),
                    Mathf.Clamp(bounds.size.z * 0.78f, 1.2f, 2.8f));
                box.center = new Vector3(
                    bounds.center.x,
                    bottom + box.size.y * 0.5f,
                    bounds.center.z);
                this.m_IndicatorLocalPosition = new Vector3(
                    box.center.x + box.size.x * 0.5f + 0.45f,
                    bottom + box.size.y + 0.45f,
                    box.center.z);
            }
        }

        private bool TryGetVisualLocalBounds(out Bounds localBounds)
        {
            localBounds = default;
            Renderer[] renderers = this.visuals.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer is ParticleSystemRenderer) continue;

                Bounds worldBounds = renderer.bounds;
                Vector3 min = worldBounds.min;
                Vector3 max = worldBounds.max;
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            Vector3 worldCorner = new Vector3(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            Vector3 localCorner = transform.InverseTransformPoint(worldCorner);
                            if (!hasBounds)
                            {
                                localBounds = new Bounds(localCorner, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(localCorner);
                            }
                        }
                    }
                }
            }

            return hasBounds;
        }

        public bool TakeDamage(int damage, PlayerInventory harvester)
        {
            EnsureInitialized();
            if (!this.CanHarvest || damage <= 0 || harvester == null) return false;
            if (GamePhaseManager.Instance != null && GamePhaseManager.Instance.CurrentPhase != GamePhase.Resource)
                return false;

            int healthDamage = Mathf.Min(damage, this.m_CurrentHealth);
            bool willDeplete = healthDamage >= this.m_CurrentHealth;
            int requestedYield = this.resourcesPerHit * healthDamage + (willDeplete ? this.depletionBonus : 0);
            int awarded = harvester.AddResource(this.resourceType, requestedYield);
            if (awarded <= 0) return false;

            this.m_CurrentHealth -= healthDamage;
            this.OnHarvested?.Invoke(this, harvester, awarded);

            if (this.hitSound != null && this.m_AudioSource != null)
            {
                this.m_AudioSource.PlayOneShot(this.hitSound);
            }

            if (this.harvestEffect != null)
            {
                this.harvestEffect.Play();
            }

            if (this.m_HitFeedbackRoutine != null) StopCoroutine(this.m_HitFeedbackRoutine);
            this.m_HitFeedbackRoutine = StartCoroutine(HitFeedbackRoutine());

            if (this.m_CurrentHealth <= 0)
            {
                DestroyNode();
            }

            return true;
        }

        private IEnumerator HitFeedbackRoutine()
        {
            if (this.visuals == null) yield break;

            this.visuals.transform.localPosition = this.m_VisualStartPosition;
            this.visuals.transform.localScale = this.m_VisualStartScale;
            float elapsed = 0f;
            const float duration = 0.16f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float shake = (1f - t) * 0.14f;
                this.visuals.transform.localPosition = this.m_VisualStartPosition +
                    new Vector3(UnityEngine.Random.Range(-shake, shake), 0f, UnityEngine.Random.Range(-shake, shake));
                this.visuals.transform.localScale = Vector3.Lerp(
                    this.m_VisualStartScale * 1.12f,
                    this.m_VisualStartScale,
                    t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            this.visuals.transform.localPosition = this.m_VisualStartPosition;
            this.visuals.transform.localScale = this.m_VisualStartScale;
            this.m_HitFeedbackRoutine = null;
        }

        private void DestroyNode()
        {
            this.m_IsDestroyed = true;
            this.State = NodeState.Depleted;
            this.OnDepleted?.Invoke(this);
            if (this.m_HitFeedbackRoutine != null)
            {
                StopCoroutine(this.m_HitFeedbackRoutine);
                this.m_HitFeedbackRoutine = null;
            }
            ResetVisualTransform();
            if (this.visuals != null) this.visuals.SetActive(false);
            SetCollidersEnabled(false);

            if (this.respawnTime > 0)
            {
                this.m_RespawnRoutine = StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            this.State = NodeState.Respawning;
            float variance = UnityEngine.Random.Range(-this.respawnVariance, this.respawnVariance);
            this.RespawnRemaining = Mathf.Max(
                0.05f,
                (this.respawnTime + variance) * MatchRules.RespawnMultiplier);
            this.RespawnDuration = this.RespawnRemaining;
            float previousTime = Time.realtimeSinceStartup;

            while (this.RespawnRemaining > 0f)
            {
                if (GamePhaseManager.Instance != null &&
                    GamePhaseManager.Instance.CurrentPhase != GamePhase.Resource)
                {
                    this.State = NodeState.Depleted;
                    this.RespawnRemaining = 0f;
                    this.m_RespawnRoutine = null;
                    yield break;
                }

                float currentTime = Time.realtimeSinceStartup;
                float elapsed = currentTime - previousTime;
                previousTime = currentTime;
                UpdateRespawnVisual();
                if (Time.timeScale > 0f)
                    this.RespawnRemaining -= elapsed;
                yield return null;
            }

            this.m_RespawnRoutine = null;
            RestoreNode();
        }

        public void RestoreNode()
        {
            EnsureInitialized();
            if (this.m_RespawnRoutine != null)
            {
                StopCoroutine(this.m_RespawnRoutine);
                this.m_RespawnRoutine = null;
            }
            this.m_CurrentHealth = this.maxHealth;
            this.m_IsDestroyed = false;
            this.State = NodeState.Available;
            this.RespawnRemaining = 0f;
            this.RespawnDuration = 0f;
            ResetVisualTransform();
            if (this.visuals != null) this.visuals.SetActive(true);
            SetCollidersEnabled(true);
            this.OnRespawned?.Invoke(this);
        }

        private void UpdateRespawnVisual()
        {
            if (this.visuals == null || this.respawnWarningTime <= 0f ||
                this.RespawnRemaining > this.respawnWarningTime)
                return;

            if (!this.visuals.activeSelf) this.visuals.SetActive(true);
            float progress = 1f - Mathf.Clamp01(this.RespawnRemaining / this.respawnWarningTime);
            float eased = progress * progress * (3f - 2f * progress);
            this.visuals.transform.localPosition = this.m_VisualStartPosition;
            this.visuals.transform.localScale = Vector3.Lerp(
                this.m_VisualStartScale * 0.12f,
                this.m_VisualStartScale,
                eased);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (this.m_Colliders == null) return;
            for (int i = 0; i < this.m_Colliders.Length; i++)
            {
                Collider collider = this.m_Colliders[i];
                if (collider != null)
                    collider.enabled = enabled && this.m_ColliderDefaults[i];
            }
        }

        private void ResetVisualTransform()
        {
            if (this.visuals == null) return;
            this.visuals.transform.localPosition = this.m_VisualStartPosition;
            this.visuals.transform.localScale = this.m_VisualStartScale;
        }

        private void OnDisable()
        {
            if (this.m_HitFeedbackRoutine != null) this.m_HitFeedbackRoutine = null;
            if (this.m_RespawnRoutine != null) this.m_RespawnRoutine = null;
            ResetVisualTransform();
        }
    }
}
