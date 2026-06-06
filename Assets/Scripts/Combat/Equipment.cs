using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Controls the visibility of the character's weapon and shield models. Both are hidden at
    /// startup (the player begins with bare fists and no armor, per the GDD). They are shown again
    /// when a weapon/armor is equipped — driven by <see cref="MeleeAttack"/> and <see cref="Health"/>,
    /// which the shop (Paket 3) triggers on purchase.
    /// </summary>
    public class Equipment : MonoBehaviour
    {
        #region Public Fields
        [Tooltip("The character's weapon model (e.g. the 'Sword' child object).")]
        public GameObject weaponObject;

        [Tooltip("The character's shield model (e.g. the 'Shield' child object).")]
        public GameObject shieldObject;

        [Tooltip("Hide both at start so the character begins unarmed.")]
        public bool hideOnStart = true;
        #endregion

        private void Awake()
        {
            if (this.hideOnStart)
            {
                this.SetWeaponVisible(false);
                this.SetShieldVisible(false);
            }
        }

        public void SetWeaponVisible(bool visible)
        {
            if (this.weaponObject != null) this.weaponObject.SetActive(visible);
        }

        public void SetShieldVisible(bool visible)
        {
            if (this.shieldObject != null) this.shieldObject.SetActive(visible);
        }
    }
}
