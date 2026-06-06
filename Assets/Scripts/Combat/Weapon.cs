using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Weapon stats. The default asset represents "Fists". The shop (Paket 3) will swap the
    /// weapon assigned to a player's <see cref="MeleeAttack"/> later; here it is just the
    /// source of damage and swing cooldown.
    /// Create via: Assets > Create > ArenaCraft > Weapon.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon", menuName = "ArenaCraft/Weapon")]
    public class Weapon : ScriptableObject
    {
        [Tooltip("Damage dealt per successful hit.")]
        public float damage = 10f;

        [Tooltip("Seconds between swings (GDD issue C.3 — tune later).")]
        public float swingCooldown = 0.5f;

        [Tooltip("Display name, e.g. 'Fists', 'Basic Sword'.")]
        public string displayName = "Fists";

        [Tooltip("If true, equipping this weapon shows the character's weapon model (via Equipment). " +
                 "Fists = false; swords = true.")]
        public bool showsWeaponModel = false;
    }
}
