using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Builds a low-poly gladiator from primitives: a capsule body tinted with the
    /// player accent colour (P1 red, P2 blue – GDD 3.2.1), a head, a forward-facing
    /// weapon "blade" used for the swing visual, and the Rigidbody/collider needed
    /// for X/Z physics movement. Cosmetic hook (FCR4) provided via AddCosmetic.
    /// </summary>
    public static class PlayerFactory
    {
        public static PlayerController Create(PlayerConfig config, PlayerStats stats, GameSettings settings,
            Transform parent, Vector3 spawnPos)
        {
            var root = new GameObject($"Player{config.playerId}");
            root.transform.SetParent(parent, false);
            root.transform.position = spawnPos;

            // Physics body.
            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 6f;          // stops sliding so movement feels responsive
            rb.angularDamping = 0.05f;

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.45f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            // Visual body.
            var body = Prim.Create(PrimitiveType.Capsule, "Body", root.transform,
                new Vector3(0f, 0.9f, 0f), new Vector3(0.9f, 0.9f, 0.9f), config.accentColor, collider: false);

            // Head.
            Prim.Create(PrimitiveType.Sphere, "Head", root.transform,
                new Vector3(0f, 1.85f, 0f), Vector3.one * 0.55f, new Color(0.85f, 0.72f, 0.55f), collider: false);

            // A little crest so the two gladiators read as "slightly different" (GDD 3.3).
            var crest = Prim.Create(PrimitiveType.Cube, "Crest", root.transform,
                new Vector3(0f, 2.15f, 0f), new Vector3(0.12f, 0.3f, 0.5f),
                Color.Lerp(config.accentColor, Color.white, 0.4f), collider: false);
            if (config.playerId == 2) crest.transform.localScale = new Vector3(0.5f, 0.25f, 0.12f);

            // Weapon pivot in front of the gladiator (also the attack hitbox origin).
            var weaponPivot = new GameObject("WeaponPivot");
            weaponPivot.transform.SetParent(root.transform, false);
            weaponPivot.transform.localPosition = new Vector3(0.35f, 1.0f, 0.55f);
            var blade = Prim.Create(PrimitiveType.Cube, "Blade", weaponPivot.transform,
                new Vector3(0f, 0f, 0.4f), new Vector3(0.12f, 0.12f, 0.7f), new Color(0.8f, 0.8f, 0.85f), collider: false);

            var attackOrigin = new GameObject("AttackOrigin");
            attackOrigin.transform.SetParent(root.transform, false);
            attackOrigin.transform.localPosition = new Vector3(0f, 0.9f, settings.attackRange);

            var controller = root.AddComponent<PlayerController>();
            controller.Initialize(config, stats, settings, blade.transform, attackOrigin.transform);
            return controller;
        }

        /// <summary>FCR4 cosmetics hook – attach a hat/glasses primitive on top of the head.</summary>
        public static void AddCosmetic(PlayerController player, PrimitiveType type, Color color)
        {
            Prim.Create(type, "Cosmetic", player.transform,
                new Vector3(0f, 2.2f, 0f), Vector3.one * 0.4f, color, collider: false);
        }
    }
}
