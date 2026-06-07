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
        #endregion

        #region Private Fields
        private Collider col;
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
        }

        /// <summary>End the swing: disables the trigger.</summary>
        public void EndSwing()
        {
            this.active = false;
            this.col.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!this.active) return;

            // Handle Health (Players)
            Health target = other.GetComponentInParent<Health>();
            if (target != null && target != this.owner)
            {
                if (this.hitThisSwing.Add(target))
                {
                    target.TakeDamage(this.currentDamage);
                    this.OnHit?.Invoke(target, this.currentDamage);
                }
                return;
            }

            // Handle Resource Nodes
            ResourceNode node = other.GetComponentInParent<ResourceNode>();
            if (node != null)
            {
                if (this.nodesHitThisSwing.Add(node))
                {
                    PlayerInventory inv = this.owner != null ? this.owner.GetComponent<PlayerInventory>() : null;
                    node.TakeDamage(1, inv);
                    this.OnResourceHit?.Invoke(node);
                }
            }
        }
    }
}
