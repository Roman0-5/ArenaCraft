using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Drives one gladiator: X/Z movement, melee attacks and resource harvesting.
    /// Movement is locked to the X/Z plane via Rigidbody constraints (GDD 2.2.1/2.2.2).
    /// The same melee swing damages resource nodes (Resource Phase) and opponents
    /// (Battle Royale), implementing the "attack to collect / attack to fight" loop.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public PlayerConfig Config { get; private set; }
        public PlayerStats Stats { get; private set; }
        public bool InputEnabled { get; set; }
        public bool MovementEnabled { get; set; } = true;

        private GameSettings _settings;
        private Rigidbody _rb;
        private Transform _weaponVisual;     // the swung "blade" cube
        private Transform _attackOrigin;     // a point in front of the body
        private Vector3 _facing = Vector3.forward;
        private float _attackTimer;
        private float _swingVisualTimer;

        public bool IsAlive => Stats != null && Stats.IsAlive;
        public Transform Transform => transform;

        public void Initialize(PlayerConfig config, PlayerStats stats, GameSettings settings,
            Transform weaponVisual, Transform attackOrigin)
        {
            Config = config;
            Stats = stats;
            _settings = settings;
            _weaponVisual = weaponVisual;
            _attackOrigin = attackOrigin;
            _rb = GetComponent<Rigidbody>();
            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void Update()
        {
            if (_attackTimer > 0f) _attackTimer -= Time.deltaTime;

            // Animate the swing: scale the blade up briefly when attacking.
            if (_weaponVisual != null)
            {
                if (_swingVisualTimer > 0f)
                {
                    _swingVisualTimer -= Time.deltaTime;
                    _weaponVisual.localScale = Vector3.Lerp(_weaponVisual.localScale, new Vector3(0.18f, 0.18f, 1.4f), 18f * Time.deltaTime);
                }
                else
                {
                    _weaponVisual.localScale = Vector3.Lerp(_weaponVisual.localScale, new Vector3(0.12f, 0.12f, 0.7f), 12f * Time.deltaTime);
                }
            }

            if (!InputEnabled || !IsAlive || Config == null) return;

            if (Config.AttackPressed())
                TryAttack();
        }

        private void FixedUpdate()
        {
            if (_rb == null) return;

            Vector3 move = Vector3.zero;
            if (InputEnabled && MovementEnabled && IsAlive && Config != null)
                move = Config.ReadMove();

            _rb.linearVelocity = new Vector3(move.x * _settings.moveSpeed, _rb.linearVelocity.y, move.z * _settings.moveSpeed);

            if (move.sqrMagnitude > 0.01f)
            {
                _facing = move.normalized;
                // Smoothly face the movement direction.
                Quaternion target = Quaternion.LookRotation(_facing, Vector3.up);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, target, 15f * Time.fixedDeltaTime));
            }
        }

        private void TryAttack()
        {
            if (_attackTimer > 0f) return;
            _attackTimer = Stats.AttackCooldown;
            _swingVisualTimer = 0.12f;

            AudioManager.Instance?.PlayAttack();

            Vector3 origin = _attackOrigin != null
                ? _attackOrigin.position
                : transform.position + _facing * _settings.attackRange;

            // Detect every collider in the swing arc once.
            Collider[] hits = Physics.OverlapSphere(origin, _settings.attackHitboxRadius);
            foreach (var col in hits)
            {
                if (col.transform == transform) continue;
                var dmg = col.GetComponentInParent<IDamageable>();
                if (dmg == null || ReferenceEquals(dmg, this) || !dmg.IsAlive) continue;
                dmg.TakeDamage(Stats.WeaponDamage, this);
            }
        }

        // ---- IDamageable ----
        public void TakeDamage(float amount, PlayerController source)
        {
            if (!IsAlive) return;
            Stats.TakeDamage(amount);
            AudioManager.Instance?.PlayHit();

            // A little knockback away from the attacker for game feel.
            if (source != null && _rb != null)
            {
                Vector3 dir = (transform.position - source.transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    _rb.AddForce(dir.normalized * 3.5f, ForceMode.VelocityChange);
            }
        }

        /// <summary>Reposition (used between phases) and zero out velocity.</summary>
        public void Teleport(Vector3 position, Vector3 faceDir)
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.position = position;
            }
            transform.position = position;
            if (faceDir.sqrMagnitude > 0.001f)
            {
                _facing = faceDir.normalized;
                transform.rotation = Quaternion.LookRotation(_facing, Vector3.up);
            }
        }
    }
}
