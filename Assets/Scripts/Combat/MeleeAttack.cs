using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaCraft
{
    /// <summary>
    /// Listens to the player's Attack action and performs a melee swing: triggers the attack
    /// animation, plays the swing SFX, and activates the <see cref="AttackHitbox"/> for a brief
    /// window. Enforces a per-weapon swing cooldown (GDD issue C.3).
    /// </summary>
    [RequireComponent(typeof(PlayerInputProvider))]
    public class MeleeAttack : MonoBehaviour
    {
        #region Public Fields
        [Header("References")]
        [Tooltip("Trigger hitbox in front of the character.")]
        public AttackHitbox hitbox;

        [Tooltip("Weapon stats (damage + cooldown). If empty, the fallback values below are used.")]
        public Weapon weapon;

        [Tooltip("Animator to trigger. If empty, the first Animator in children is used.")]
        public Animator animator;

        [Tooltip("Optional combat audio for swing/hit SFX.")]
        public CombatAudio combatAudio;

        [Tooltip("Owner Health (for the death check). Auto-filled from this object if empty.")]
        public Health owner;

        [Header("Timing")]
        [Tooltip("How long the hitbox stays active per swing (seconds).")]
        public float hitboxActiveTime = 0.15f;

        [Tooltip("Delay from key press to hitbox activation, to sync with the swing animation.")]
        public float windup = 0.05f;

        [Tooltip("Swing cooldown when no Weapon is assigned (seconds). GDD issue C.3.")]
        public float fallbackCooldown = 0.5f;

        [Tooltip("Damage when no Weapon is assigned.")]
        public float fallbackDamage = 10f;

        [Header("Animation")]
        public string attackTrigger = "Attack";
        #endregion

        #region Private Fields
        private PlayerInputProvider input;
        private float lastAttackTime = -999f;
        private int attackHash;
        #endregion

        private float Cooldown => this.weapon != null ? this.weapon.swingCooldown : this.fallbackCooldown;
        private float Damage => this.weapon != null ? this.weapon.damage : this.fallbackDamage;

        /// <summary>Current per-hit damage (from the equipped Weapon, or the fallback). For HUD.</summary>
        public float CurrentDamage => this.Damage;

        /// <summary>Name of the equipped weapon (or "Fists" when none). For HUD.</summary>
        public string CurrentWeaponName => this.weapon != null ? this.weapon.displayName : "Fists";

        /// <summary>
        /// Equip a weapon, replacing damage and swing cooldown. This is the hook the shop
        /// (Paket 3) calls on purchase, e.g. meleeAttack.EquipWeapon(advancedSword).
        /// </summary>
        public void EquipWeapon(Weapon newWeapon)
        {
            this.weapon = newWeapon;
        }

        private void Awake()
        {
            this.input = GetComponent<PlayerInputProvider>();
            if (this.animator == null) this.animator = GetComponentInChildren<Animator>();
            if (this.owner == null) this.owner = GetComponent<Health>();
            if (this.hitbox != null && this.hitbox.owner == null) this.hitbox.owner = this.owner;
            this.attackHash = Animator.StringToHash(this.attackTrigger);
        }

        private void OnEnable()
        {
            if (this.hitbox != null) this.hitbox.OnHit += this.HandleHit;
        }

        private void OnDisable()
        {
            if (this.hitbox != null) this.hitbox.OnHit -= this.HandleHit;
        }

        private void Update()
        {
            // Polled live each frame (not an OnEnable event subscription) so it does not depend on
            // whether PlayerInputProvider has resolved its actions yet at OnEnable time.
            if (this.input.Attack == null) return;
            if (!this.input.Attack.WasPerformedThisFrame()) return;
            if (this.owner != null && this.owner.IsDead) return;
            if (Time.time - this.lastAttackTime < this.Cooldown) return;

            this.lastAttackTime = Time.time;
            StartCoroutine(this.SwingRoutine());
        }

        private IEnumerator SwingRoutine()
        {
            if (this.animator != null) this.animator.SetTrigger(this.attackHash);
            if (this.combatAudio != null) this.combatAudio.PlaySwing();

            if (this.windup > 0f) yield return new WaitForSeconds(this.windup);

            if (this.hitbox != null) this.hitbox.BeginSwing(this.Damage);
            yield return new WaitForSeconds(this.hitboxActiveTime);
            if (this.hitbox != null) this.hitbox.EndSwing();
        }

        private void HandleHit(Health victim, float damage)
        {
            if (this.combatAudio != null) this.combatAudio.PlayHit();
        }
    }
}
