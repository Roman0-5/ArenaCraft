using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// A destructible resource node (Tree / Stone / Metal / hidden Relic – GDD 2.2.3,
    /// FMR2). Players attack it; each hit awards resource units (and therefore gold)
    /// to the attacker and chips away its HP. At 0 HP it shatters and (optionally)
    /// respawns after a delay – respawn is disabled by default per issue C.2.
    /// </summary>
    public class ResourceNode : MonoBehaviour, IDamageable
    {
        private ResourceTypeDef _def;
        private GameSettings _settings;
        private float _hp;
        private float _maxHp;
        private MeshRenderer _renderer;
        private Color _baseColor;
        private Vector3 _baseScale;
        private float _hitFlash;
        private bool _depleted;
        private float _respawnTimer;

        public bool IsAlive => !_depleted;
        public Transform Transform => transform;
        public ResourceKind Kind => _def != null ? _def.kind : ResourceKind.Wood;

        public void Initialize(ResourceTypeDef def, GameSettings settings, MeshRenderer renderer)
        {
            _def = def;
            _settings = settings;
            _renderer = renderer;
            _maxHp = def.nodeHp;
            _hp = _maxHp;
            _baseScale = transform.localScale;
            _baseColor = renderer != null ? renderer.material.color : def.color;
        }

        private void Update()
        {
            // Hit flash decay.
            if (_hitFlash > 0f)
            {
                _hitFlash -= Time.deltaTime * 4f;
                if (_renderer != null)
                {
                    Color c = Color.Lerp(_baseColor, Color.white, Mathf.Clamp01(_hitFlash));
                    _renderer.material.color = c;
                }
                // Slight squash on hit.
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 10f * Time.deltaTime);
            }

            if (_depleted && _settings.nodesRespawn)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0f) Respawn();
            }
        }

        public void TakeDamage(float amount, PlayerController source)
        {
            if (_depleted) return;

            _hp -= amount;
            _hitFlash = 1f;
            transform.localScale = _baseScale * 0.9f;

            // Award resources to the attacker (auto-converted to gold inside PlayerStats).
            if (source != null && source.Stats != null)
            {
                int accepted = source.Stats.CollectResource(_def, _def.unitsPerHit);
                if (accepted > 0)
                    AudioManager.Instance?.PlayCollect();
            }

            if (_hp <= 0f)
                Deplete();
        }

        private void Deplete()
        {
            _depleted = true;
            if (_renderer != null) _renderer.enabled = false;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            AudioManager.Instance?.PlayNodeBreak();

            if (_settings.nodesRespawn)
                _respawnTimer = 15f;
        }

        private void Respawn()
        {
            _depleted = false;
            _hp = _maxHp;
            transform.localScale = _baseScale;
            if (_renderer != null)
            {
                _renderer.enabled = true;
                _renderer.material.color = _baseColor;
            }
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }
    }
}
