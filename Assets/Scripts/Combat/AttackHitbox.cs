using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// A trigger collider placed in front of the character. <see cref="MeleeAttack"/> activates it
    /// for a brief window per swing. Damages any opposing <see cref="Health"/> that overlaps, never
    /// the owner, and only once per swing.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AttackHitbox : MonoBehaviour
    {
        #region Public Fields
        [Tooltip("The Health that owns this hitbox; it is never damaged by its own swing. " +
                 "Auto-filled by MeleeAttack if left empty.")]
        public Health owner;

        [Tooltip("Extra tolerance around the forward hitbox when harvesting differently sized world props.")]
        public float resourceHarvestRadius = 1.35f;

        [Header("PvP Hit Reaction")]
        [Tooltip("Knockback velocity applied to the victim on hit (units/second).")]
        public float knockbackSpeed = 4.5f;

        [Tooltip("How long the victim is briefly stunned after a hit (seconds).")]
        public float knockbackDuration = 0.18f;

        [Tooltip("Resource Phase only: how many units from the victim's biggest stack are stolen and dropped on hit.")]
        public int resourceStealOnHit = 4;
        #endregion

        #region Private Fields
        private Collider col;
        private BoxCollider boxCollider;
        private float currentDamage;
        private bool active;
        private readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
        private readonly HashSet<ResourceNode> nodesHitThisSwing = new HashSet<ResourceNode>();
        #endregion

        /// <summary>Raised when this hitbox damages a target: (victim, damage).</summary>
        public event Action<Health, float> OnHit;
        public event Action<ResourceNode> OnResourceHit;

        private void Awake()
        {
            this.col = GetComponent<Collider>();
            this.boxCollider = this.col as BoxCollider;
            this.col.isTrigger = true;
            this.col.enabled = false;
        }

        /// <summary>Begin a swing: clears per-swing hits and enables the trigger.</summary>
        public void BeginSwing(float damage)
        {
            this.currentDamage = damage;
            this.hitThisSwing.Clear();
            this.nodesHitThisSwing.Clear();
            this.active = true;
            this.col.enabled = true;
            Physics.SyncTransforms();
            this.ScanCurrentOverlaps();
            this.ScanNearbyResources();
        }

        /// <summary>End the swing: disables the trigger.</summary>
        public void EndSwing()
        {
            this.active = false;
            this.col.enabled = false;
        }

        private void FixedUpdate()
        {
            if (this.active)
            {
                this.ScanCurrentOverlaps();
                this.ScanNearbyResources();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            this.TryHit(other);
        }

        private void ScanCurrentOverlaps()
        {
            Collider[] overlaps;
            if (this.boxCollider != null)
            {
                Vector3 center = this.boxCollider.transform.TransformPoint(this.boxCollider.center);
                Vector3 scale = this.boxCollider.transform.lossyScale;
                Vector3 halfExtents = Vector3.Scale(
                    this.boxCollider.size * 0.5f,
                    new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                overlaps = Physics.OverlapBox(
                    center,
                    halfExtents,
                    this.boxCollider.transform.rotation,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide);
            }
            else
            {
                Bounds bounds = this.col.bounds;
                overlaps = Physics.OverlapBox(
                    bounds.center,
                    bounds.extents,
                    Quaternion.identity,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Collide);
            }

            foreach (Collider overlap in overlaps)
                this.TryHit(overlap);
        }

        private void ScanNearbyResources()
        {
            if (CurrentPhase() != GamePhase.Resource) return;

            Vector3 center = this.col.bounds.center;
            float radius = Mathf.Max(0.1f, this.resourceHarvestRadius);
            System.Collections.Generic.IReadOnlyList<ResourceNode> nodes = ResourceNode.AllActive;
            for (int i = 0; i < nodes.Count; i++)
            {
                ResourceNode node = nodes[i];
                if (node == null || !node.CanHarvest || this.nodesHitThisSwing.Contains(node))
                    continue;

                Collider[] colliders = node.GetComponentsInChildren<Collider>(true);
                foreach (Collider resourceCollider in colliders)
                {
                    if (resourceCollider == null || !resourceCollider.enabled) continue;
                    Vector3 closest = resourceCollider.ClosestPoint(center);
                    if ((closest - center).sqrMagnitude > radius * radius) continue;

                    PlayerInventory inventory = this.owner != null
                        ? this.owner.GetComponent<PlayerInventory>()
                        : null;
                    if (this.nodesHitThisSwing.Add(node) && node.TakeDamage(1, inventory))
                        this.OnResourceHit?.Invoke(node);
                    break;
                }
            }
        }

        private void TryHit(Collider other)
        {
            if (!this.active || other == null || other == this.col) return;

            // Handle Health (Players)
            Health target = other.GetComponentInParent<Health>();
            if (target != null && target != this.owner)
            {
                GamePhase phase = CurrentPhase();
                if (phase != GamePhase.Resource && phase != GamePhase.BattleRoyale) return;
                if (!this.hitThisSwing.Add(target)) return;

                ApplyKnockback(target);

                if (phase == GamePhase.BattleRoyale)
                {
                    target.TakeDamage(this.currentDamage);
                    this.OnHit?.Invoke(target, this.currentDamage);
                }
                else // Resource phase: steal + drop resources, no HP damage
                {
                    StealAndDrop(target);
                    this.OnHit?.Invoke(target, 0f);
                }
                return;
            }

            // Handle Resource Nodes
            ResourceNode node = other.GetComponentInParent<ResourceNode>();
            if (node != null)
            {
                if (CurrentPhase() != GamePhase.Resource) return;
                if (this.nodesHitThisSwing.Add(node))
                {
                    PlayerInventory inv = this.owner != null ? this.owner.GetComponent<PlayerInventory>() : null;
                    if (node.TakeDamage(1, inv)) this.OnResourceHit?.Invoke(node);
                }
            }
        }

        private void ApplyKnockback(Health victim)
        {
            ArenaPlayerController ctrl = victim.GetComponent<ArenaPlayerController>();
            if (ctrl == null) return;

            Vector3 fromAttacker = victim.transform.position - this.owner.transform.position;
            fromAttacker.y = 0f;
            Vector3 direction = fromAttacker.sqrMagnitude > 0.0001f
                ? fromAttacker.normalized
                : this.transform.forward;

            ctrl.ApplyKnockback(direction * this.knockbackSpeed, this.knockbackDuration);
        }

        private void StealAndDrop(Health victim)
        {
            PlayerInventory victimInv = victim.GetComponent<PlayerInventory>();
            if (victimInv == null || this.resourceStealOnHit <= 0) return;

            int dropped = victimInv.DropLargestStack(this.resourceStealOnHit, out ResourceType type);
            if (dropped <= 0) return;

            // Drop slightly behind the victim along the knockback direction so they fly off the pile.
            Vector3 toVictim = victim.transform.position - this.owner.transform.position;
            toVictim.y = 0f;
            Vector3 offset = toVictim.sqrMagnitude > 0.0001f ? toVictim.normalized * -0.6f : Vector3.zero;
            ResourceDrop.Spawn(type, dropped, victim.transform.position + offset);
        }

        private static GamePhase CurrentPhase()
        {
            return GamePhaseManager.Instance != null
                ? GamePhaseManager.Instance.CurrentPhase
                : GamePhase.BattleRoyale;
        }
    }
}
