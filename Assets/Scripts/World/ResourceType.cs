using UnityEngine;

namespace ArenaCraft
{
    /// <summary>The three harvestable resource families described in GDD 2.2.6 / 4.1.</summary>
    public enum ResourceKind
    {
        Wood,
        Stone,
        Metal,
        Rare // FCR6: hidden rare node special drop
    }

    /// <summary>
    /// Static, designer-facing description of a resource type: how much a single
    /// node holds, how tough it is, its gold value and the colour used for its
    /// low-poly mesh. Values are taken from GDD 2.2.6 (1 Wood = 1g, 1 Stone = 2g,
    /// 1 Metal = 5g). Issue C.1 is resolved here with playtest-ready defaults.
    /// </summary>
    [System.Serializable]
    public class ResourceTypeDef
    {
        public ResourceKind kind;
        public string displayName;
        public int goldPerUnit;       // gold awarded per resource unit
        public int unitsPerHit;       // resource units gained per attack on the node
        public float nodeHp;          // hit points of a node of this type
        public Color color;
        public float spawnWeight;     // relative likelihood when randomising placement

        public ResourceTypeDef(ResourceKind kind, string displayName, int goldPerUnit,
            int unitsPerHit, float nodeHp, Color color, float spawnWeight)
        {
            this.kind = kind;
            this.displayName = displayName;
            this.goldPerUnit = goldPerUnit;
            this.unitsPerHit = unitsPerHit;
            this.nodeHp = nodeHp;
            this.color = color;
            this.spawnWeight = spawnWeight;
        }

        public static ResourceTypeDef[] DefaultSet()
        {
            return new[]
            {
                // Common, cheap, plentiful.
                new ResourceTypeDef(ResourceKind.Wood,  "Wood",  1, 5, 30f, new Color(0.45f, 0.30f, 0.15f), 0.55f),
                // Uncommon.
                new ResourceTypeDef(ResourceKind.Stone, "Stone", 2, 4, 50f, new Color(0.55f, 0.55f, 0.58f), 0.30f),
                // Rare, valuable, tough.
                new ResourceTypeDef(ResourceKind.Metal, "Metal", 5, 3, 80f, new Color(0.70f, 0.55f, 0.25f), 0.15f),
                // FCR6 hidden rare node – only one spawns, huge payout.
                new ResourceTypeDef(ResourceKind.Rare,  "Relic", 15, 5, 60f, new Color(0.95f, 0.85f, 0.20f), 0f),
            };
        }
    }
}
