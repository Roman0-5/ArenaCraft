using System;
using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// HP and armor for a player. Base 100 HP (GDD); armor raises max HP. Exposes events so the
    /// HUD (Paket 3) and game flow (Paket 1) can react to damage and elimination.
    /// </summary>
    public class Health : MonoBehaviour
    {
        #region Public Fields
        [Tooltip("Base max HP before armor (GDD: 100).")]
        public int baseMaxHP = 100;

        [Tooltip("Starting armor tier.")]
        public ArmorType armor = ArmorType.None;

        [Header("On death")]
        [Tooltip("Stop movement/attacks when this player is eliminated.")]
        public bool disableControlsOnDeath = true;

        [Tooltip("Optional Animator trigger fired on death (leave empty to skip).")]
        public string deathTrigger = "";
        #endregion

        #region Private Fields
        private int maxHP;
        private int currentHP;
        private bool isDead;
        private ShieldBlock shieldBlock;
        #endregion

        public int MaxHP => this.maxHP;
        public int CurrentHP => this.currentHP;
        public bool IsDead => this.isDead;
        public float Normalized => this.maxHP > 0 ? (float)this.currentHP / this.maxHP : 0f;

        /// <summary>Raised after any HP or max-HP change: (currentHP, maxHP).</summary>
        public event Action<int, int> OnHealthChanged;

        /// <summary>Raised once when this player reaches 0 HP.</summary>
        public event Action<Health> OnDied;

        /// <summary>Raised after damage is applied: (target, damage dealt).</summary>
        public event Action<Health, int> OnDamaged;

        /// <summary>Raised when an incoming hit is blocked.</summary>
        public event Action<Health> OnBlocked;

        private void Awake()
        {
            this.shieldBlock = GetComponent<ShieldBlock>();
            this.RecalculateMaxHP(true);
        }

        /// <summary>Recompute max HP from base + armor.</summary>
        /// <param name="refill">If true, also heal to full (use at match start).</param>
        public void RecalculateMaxHP(bool refill)
        {
            this.maxHP = this.baseMaxHP + this.armor.BonusHP();
            this.currentHP = refill ? this.maxHP : Mathf.Min(this.currentHP, this.maxHP);
            this.OnHealthChanged?.Invoke(this.currentHP, this.maxHP);
        }

        /// <summary>Equip an armor tier; raises max HP and grants the bonus delta as current HP.</summary>
        public void ApplyArmor(ArmorType newArmor)
        {
            int before = this.armor.BonusHP();
            this.armor = newArmor;
            int after = this.armor.BonusHP();

            this.maxHP = this.baseMaxHP + after;
            this.currentHP = Mathf.Min(this.currentHP + Mathf.Max(0, after - before), this.maxHP);
            this.OnHealthChanged?.Invoke(this.currentHP, this.maxHP);
        }

        public void TakeDamage(float amount)
        {
            if (this.isDead || amount <= 0f) return;

            // Active shield block negates the hit (and consumes a shield charge).
            if (this.shieldBlock != null && this.shieldBlock.TryBlock())
            {
                this.OnBlocked?.Invoke(this);
                ArenaCameraImpact.Shake(0.08f, 0.08f);
                Debug.Log($"{name}: BLOCKED  ({this.shieldBlock.BlocksRemaining} blocks left)", this);
                return;
            }

            int damage = Mathf.RoundToInt(amount);
            this.currentHP = Mathf.Max(0, this.currentHP - damage);
            this.OnHealthChanged?.Invoke(this.currentHP, this.maxHP);
            this.OnDamaged?.Invoke(this, damage);
            ArenaCameraImpact.Shake(this.currentHP <= 0 ? 0.22f : 0.13f, this.currentHP <= 0 ? 0.24f : 0.12f);

            Debug.Log($"{name}: -{damage} HP  ->  {this.currentHP}/{this.maxHP}", this);

            if (this.currentHP <= 0) this.Die();
        }

        private void Die()
        {
            if (this.isDead) return;
            this.isDead = true;

            // Temporary dev feedback until the HUD exists (Paket 3).
            Debug.Log($"{name}: ELIMINATED", this);

            if (this.disableControlsOnDeath)
            {
                // Disabling the input provider stops both movement and attacks at once,
                // since PlayerController and MeleeAttack both read input through it.
                var provider = GetComponent<PlayerInputProvider>();
                if (provider != null) provider.enabled = false;

                var body = GetComponent<Rigidbody>();
                if (body != null) body.linearVelocity = Vector3.zero;
            }

            if (!string.IsNullOrEmpty(this.deathTrigger))
            {
                var anim = GetComponentInChildren<Animator>();
                if (anim != null) anim.SetTrigger(this.deathTrigger);
            }

            this.OnDied?.Invoke(this);
        }
    }
}
