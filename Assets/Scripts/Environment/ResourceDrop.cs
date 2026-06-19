using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// A pickup-able resource that spawns when a player is hit during the Resource phase. Walks-over
    /// pickup → adds the resource to the colliding player's inventory and despawns. Auto-cleans up
    /// after <see cref="m_Lifetime"/> seconds so the arena doesn't fill with stale drops.
    /// </summary>
    public class ResourceDrop : MonoBehaviour
    {
        [SerializeField] private ResourceType m_Type;
        [SerializeField] private int m_Amount;
        [SerializeField] private float m_Lifetime = 20f;
        [SerializeField] private float m_BobSpeed = 2.5f;
        [SerializeField] private float m_BobAmount = 0.12f;
        [SerializeField] private float m_PickupDelay = 0.4f;

        private float m_SpawnTime;
        private Vector3 m_BasePosition;

        /// <summary>
        /// Spawn a drop at <paramref name="worldPosition"/>. Builds its own visual (tinted cube) so
        /// no prefab/setup is needed.
        /// </summary>
        public static ResourceDrop Spawn(ResourceType type, int amount, Vector3 worldPosition)
        {
            if (amount <= 0) return null;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"ResourceDrop_{type}";
            go.transform.position = worldPosition + Vector3.up * 0.4f;
            go.transform.localScale = Vector3.one * 0.45f;

            Collider col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(renderer.sharedMaterial);
                Color color = GetTint(type);
                material.color = color;
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * 0.6f);
                }
                renderer.material = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            ResourceDrop drop = go.AddComponent<ResourceDrop>();
            drop.m_Type = type;
            drop.m_Amount = amount;
            return drop;
        }

        private void Awake()
        {
            this.m_SpawnTime = Time.time;
            this.m_BasePosition = transform.position;
        }

        private void Update()
        {
            float t = Time.time - this.m_SpawnTime;
            transform.position = this.m_BasePosition + Vector3.up * Mathf.Sin(t * this.m_BobSpeed) * this.m_BobAmount;
            transform.Rotate(0f, 80f * Time.deltaTime, 0f, Space.World);

            if (t > this.m_Lifetime) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - this.m_SpawnTime < this.m_PickupDelay) return;

            PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null) return;

            int added = inventory.AddResource(this.m_Type, this.m_Amount);
            if (added > 0) Destroy(gameObject);
        }

        private static Color GetTint(ResourceType type)
        {
            return type switch
            {
                ResourceType.Wood => new Color(0.94f, 0.56f, 0.25f, 1f),
                ResourceType.Stone => new Color(0.78f, 0.82f, 0.86f, 1f),
                ResourceType.Metal => new Color(0.38f, 0.72f, 1f, 1f),
                _ => Color.white,
            };
        }
    }
}
