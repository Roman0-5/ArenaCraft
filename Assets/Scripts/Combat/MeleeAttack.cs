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

        [Header("Combo")]
        [Tooltip("Animator trigger for the second combo hit.")]
        public string attackTrigger2 = "Attack2";

        [Tooltip("Damage multiplier for the second (heavy) combo hit.")]
        public float secondHitMultiplier = 1.5f;

        [Tooltip("Wait longer than this between swings and the combo resets to the first hit (seconds).")]
        public float comboResetTime = 1.2f;

        [Tooltip("Longer recovery after the 2nd (finisher) hit, so the combo isn't strictly better (seconds).")]
        public float comboFinisherCooldown = 0.9f;
        #endregion

        #region Private Fields
        private PlayerInputProvider input;
        private Equipment equipment;
        private float lastAttackTime = -999f;
        private int attackHash;
        private int attack2Hash;
        private int comboIndex;
        private float activeCooldown;
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
            this.RefreshWeaponVisual();
        }

        private void RefreshWeaponVisual()
        {
            if (this.equipment != null)
            {
                this.equipment.SetWeaponVisible(this.weapon != null && this.weapon.showsWeaponModel);
            }
        }

        // Lets you test weapon swaps live by changing the Weapon field in the Inspector during Play.
        private void OnValidate()
        {
            if (Application.isPlaying) this.RefreshWeaponVisual();
        }

        private void Awake()
        {
            this.input = GetComponent<PlayerInputProvider>();
            if (this.animator == null) this.animator = GetComponentInChildren<Animator>();
            if (this.owner == null) this.owner = GetComponent<Health>();
            if (this.hitbox != null && this.hitbox.owner == null) this.hitbox.owner = this.owner;
            this.equipment = GetComponent<Equipment>();
            this.attackHash = Animator.StringToHash(this.attackTrigger);
            this.attack2Hash = Animator.StringToHash(this.attackTrigger2);
        }

        private void Start()
        {
            // Apply the initial weapon's visibility (e.g. start unarmed with Fists).
            this.RefreshWeaponVisual();
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
            if (Time.time - this.lastAttackTime < this.activeCooldown) return;

            // Advance the combo if the last swing was recent, otherwise restart at the first hit.
            this.comboIndex = (Time.time - this.lastAttackTime <= this.comboResetTime)
                ? (this.comboIndex + 1) % 2
                : 0;
            this.lastAttackTime = Time.time;

            // The finisher (2nd hit) has a longer recovery so the combo isn't strictly better.
            this.activeCooldown = this.comboIndex == 1 ? this.comboFinisherCooldown : this.Cooldown;

            StartCoroutine(this.SwingRoutine(this.comboIndex));
        }

        private IEnumerator SwingRoutine(int combo)
        {
            // Second hit uses the alternate animation and deals more damage.
            int triggerHash = combo == 1 ? this.attack2Hash : this.attackHash;
            float damage = combo == 1 ? this.Damage * this.secondHitMultiplier : this.Damage;

            if (this.animator != null) this.animator.SetTrigger(triggerHash);
            if (this.combatAudio != null) this.combatAudio.PlaySwing();

            if (this.windup > 0f) yield return new WaitForSeconds(this.windup);

            if (this.hitbox != null) this.hitbox.BeginSwing(damage);
            yield return new WaitForSeconds(this.hitboxActiveTime);
            if (this.hitbox != null) this.hitbox.EndSwing();
        }

        private void HandleHit(Health victim, float damage)
        {
            if (this.combatAudio != null) this.combatAudio.PlayHit();
        }
    }
}
