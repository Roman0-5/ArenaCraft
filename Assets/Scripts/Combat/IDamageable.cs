using UnityEngine;

namespace ArenaCraft
{
    /// <summary>Anything that can be hit by a melee swing (players, resource nodes).</summary>
    public interface IDamageable
    {
        /// <param name="amount">Damage dealt.</param>
        /// <param name="source">The attacker (may be null), used for credit / knockback.</param>
        void TakeDamage(float amount, PlayerController source);

        bool IsAlive { get; }
        Transform Transform { get; }
    }
}
