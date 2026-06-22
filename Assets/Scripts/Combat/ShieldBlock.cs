using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Active shield blocking. While the player holds Block AND has a shield, incoming melee hits
    /// are negated. Each blocked hit consumes one charge; after <see cref="maxBlocks"/> blocks the
    /// shield breaks (model hidden, blocking disabled). The shield is a separate item from armor:
    /// it grants blocking, NOT extra HP (armor/HP is Paket 3). The shop grants it via EquipShield().
    /// </summary>
    public class ShieldBlock : MonoBehaviour
    {
        #region Public Fields
        [Tooltip("Does the player currently own a shield? Off at start; the shop grants it (Paket 3).")]
        public bool hasShield = false;

        [Tooltip("How many hits the shield can block before it breaks.")]
        public int maxBlocks = 10;

        [Tooltip("Animator bool parameter set true while blocking (for the block animation).")]
        public string blockingParameter = "Blocking";
        #endregion

        #region Private Fields
        private PlayerInputProvider input;
        private Equipment equipment;
        private Animator animator;
        private CombatAudio combatAudio;
        private int blocksRemaining;
        private int blockingHash;
        #endregion

        /// <summary>True while the player is actively holding block with an intact shield.</summary>
        public bool IsBlocking =>
            this.hasShield &&
            this.blocksRemaining > 0 &&
            this.input != null && this.input.Block != null && this.input.Block.IsPressed();

        public int BlocksRemaining => this.blocksRemaining;

        private void Awake()
        {
            this.input = GetComponent<PlayerInputProvider>();
            this.equipment = GetComponent<Equipment>();
            this.animator = GetComponentInChildren<Animator>();
            this.combatAudio = GetComponent<CombatAudio>();
            this.blocksRemaining = this.maxBlocks;
            this.blockingHash = Animator.StringToHash(this.blockingParameter);
        }

        private void Start()
        {
            this.ApplyShieldVisible();
        }

        private void Update()
        {
            if (this.animator != null) this.animator.SetBool(this.blockingHash, this.IsBlocking);
        }

        /// <summary>
        /// Called by <see cref="Health"/> when a hit lands. If currently blocking, consumes a
        /// charge (and breaks the shield at zero) and returns true so the hit is negated.
        /// </summary>
        public bool TryBlock()
        {
            if (!this.IsBlocking) return false;

            if (this.combatAudio != null) this.combatAudio.PlayBlock();
            this.blocksRemaining--;
            if (this.blocksRemaining <= 0) this.BreakShield();
            return true;
        }

        /// <summary>Shop hook: give the player a fresh shield with the given durability.</summary>
        public void EquipShield(int blocks)
        {
            this.hasShield = true;
            this.maxBlocks = Mathf.Max(1, blocks);
            this.blocksRemaining = this.maxBlocks;
            this.ApplyShieldVisible();
        }

        private void BreakShield()
        {
            this.hasShield = false;
            if (this.combatAudio != null) this.combatAudio.PlayShieldBreak();
            this.ApplyShieldVisible();
        }

        private void ApplyShieldVisible()
        {
            if (this.equipment != null) this.equipment.SetShieldVisible(this.hasShield);
        }

        // Lets you toggle "hasShield" in the Inspector during Play to test blocking.
        private void OnValidate()
        {
            if (Application.isPlaying && this.equipment != null)
            {
                this.equipment.SetShieldVisible(this.hasShield);
            }
        }
    }
}
