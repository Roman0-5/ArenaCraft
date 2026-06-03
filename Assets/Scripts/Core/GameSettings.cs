using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Central, designer-tunable configuration for a match. All balancing values
    /// from the GDD live here so they can be tweaked in one place (and adjusted at
    /// runtime via the Options menu, e.g. the Resource Phase timer in GDD 2.3).
    /// </summary>
    [System.Serializable]
    public class GameSettings
    {
        [Header("Phase timers (seconds)")]
        public float resourcePhaseDuration = 180f; // GDD default 3 minutes
        public float shoppingPhaseDuration = 60f;   // GDD: 1 minute
        public float noPurchaseWarningAt = 12f;      // show "didn't buy" warning when this much time is left

        [Header("Economy")]
        public int resourceBarCapacity = 100;        // GDD 2.2.6 default 100 units

        [Header("Combat")]
        public int baseMaxHp = 100;                  // GDD 2.2.5
        public float baseFistDamage = 8f;
        public float baseFistCooldown = 0.50f;       // resolves issue C.3 for bare fists
        public float attackRange = 1.6f;             // how far in front the hitbox reaches
        public float attackHitboxRadius = 0.9f;
        public float attackActiveTime = 0.12f;       // how long the trigger stays live
        public float lowHpFraction = 0.30f;          // HP bar turns red below this (FSR4)

        [Header("Movement")]
        public float moveSpeed = 6.5f;               // constant speed, no sprint (GDD 2.2.2)

        [Header("Arena")]
        public float arenaRadius = 16f;              // playable radius inside the colosseum walls
        public int resourceNodeCount = 22;           // total nodes scattered each match
        public bool spawnHiddenRareNode = true;      // FCR6
        public bool nodesRespawn = false;            // resolves issue C.2: nodes do NOT respawn (one-shot economy)

        [Header("Match")]
        public bool showTutorialOnFirstMatch = true; // GDD 4.2 instructional overlay
    }
}
