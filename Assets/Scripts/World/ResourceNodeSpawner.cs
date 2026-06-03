using System.Collections.Generic;
using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Randomly scatters resource nodes across the arena each match (GDD 4.1:
    /// "Node placement is randomized in the MVP"). Central placements are biased
    /// toward rarer/higher-value nodes to create the risk/reward zoning in GDD 3.2.2.
    /// </summary>
    public static class ResourceNodeSpawner
    {
        public static List<ResourceNode> Spawn(GameSettings settings, ResourceTypeDef[] types, Transform parent)
        {
            var nodes = new List<ResourceNode>();
            var holder = new GameObject("ResourceNodes").transform;
            holder.SetParent(parent, false);

            var placed = new List<Vector2>();
            float minDist = 2.2f;

            for (int i = 0; i < settings.resourceNodeCount; i++)
            {
                Vector2 p = FindSpot(settings.arenaRadius - 1.5f, placed, minDist);
                placed.Add(p);
                float distFromCenter = p.magnitude / settings.arenaRadius; // 0 center .. 1 edge

                ResourceTypeDef def = PickType(types, distFromCenter);
                nodes.Add(CreateNode(def, settings, holder, new Vector3(p.x, 0f, p.y)));
            }

            // FCR6: one hidden rare relic node, tucked near the arena edge.
            if (settings.spawnHiddenRareNode)
            {
                var rare = System.Array.Find(types, t => t.kind == ResourceKind.Rare);
                if (rare != null)
                {
                    Vector2 p = FindSpot(settings.arenaRadius - 1.2f, placed, minDist);
                    // Bias the relic toward the perimeter so it feels "hidden" away from the action.
                    p = p.normalized * (settings.arenaRadius - 1.4f);
                    nodes.Add(CreateNode(rare, settings, holder, new Vector3(p.x, 0f, p.y)));
                }
            }

            return nodes;
        }

        private static ResourceTypeDef PickType(ResourceTypeDef[] types, float distFromCenter)
        {
            // Centre = more metal/stone (higher risk, higher reward); edges = more wood.
            float total = 0f;
            var weights = new float[types.Length];
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].kind == ResourceKind.Rare) { weights[i] = 0f; continue; }
                float w = types[i].spawnWeight;
                if (types[i].kind == ResourceKind.Metal) w *= Mathf.Lerp(2.2f, 0.4f, distFromCenter);
                if (types[i].kind == ResourceKind.Wood) w *= Mathf.Lerp(0.5f, 1.8f, distFromCenter);
                weights[i] = w;
                total += w;
            }
            float roll = Random.value * total;
            for (int i = 0; i < types.Length; i++)
            {
                roll -= weights[i];
                if (roll <= 0f) return types[i];
            }
            return types[0];
        }

        private static Vector2 FindSpot(float maxR, List<Vector2> placed, float minDist)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                float ang = Random.value * Mathf.PI * 2f;
                float rad = Mathf.Sqrt(Random.value) * maxR; // uniform over disc
                Vector2 candidate = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                bool ok = true;
                foreach (var q in placed)
                    if ((q - candidate).sqrMagnitude < minDist * minDist) { ok = false; break; }
                if (ok) return candidate;
            }
            // Fallback: accept overlap.
            float a = Random.value * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (maxR * 0.7f);
        }

        private static ResourceNode CreateNode(ResourceTypeDef def, GameSettings settings, Transform parent, Vector3 pos)
        {
            GameObject go;
            switch (def.kind)
            {
                case ResourceKind.Wood:
                    go = BuildTree(parent, pos, def.color);
                    break;
                case ResourceKind.Stone:
                    go = Prim.Create(PrimitiveType.Cube, "Stone Node", parent,
                        pos + Vector3.up * 0.55f, new Vector3(1.2f, 1.1f, 1.2f), def.color);
                    break;
                case ResourceKind.Metal:
                    go = BuildMetal(parent, pos, def.color);
                    break;
                default: // Rare relic
                    go = Prim.Create(PrimitiveType.Sphere, "Relic Node", parent,
                        pos + Vector3.up * 0.7f, new Vector3(1.1f, 1.4f, 1.1f), def.color);
                    var rr = go.GetComponent<MeshRenderer>();
                    if (rr != null && rr.sharedMaterial.HasProperty("_EmissionColor"))
                    {
                        rr.sharedMaterial.EnableKeyword("_EMISSION");
                        rr.sharedMaterial.SetColor("_EmissionColor", def.color * 1.5f);
                    }
                    break;
            }

            var node = go.AddComponent<ResourceNode>();
            node.Initialize(def, settings, go.GetComponentInChildren<MeshRenderer>());
            return node;
        }

        private static GameObject BuildTree(Transform parent, Vector3 pos, Color leafColor)
        {
            var root = new GameObject("Tree Node");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            // Trunk.
            Prim.Create(PrimitiveType.Cylinder, "Trunk", root.transform,
                new Vector3(0f, 0.7f, 0f), new Vector3(0.35f, 0.7f, 0.35f), new Color(0.35f, 0.24f, 0.13f), collider: false);
            // Canopy (the harvestable mesh whose colour flashes on hit).
            var canopy = Prim.Create(PrimitiveType.Sphere, "Canopy", root.transform,
                new Vector3(0f, 1.7f, 0f), new Vector3(1.6f, 1.7f, 1.6f), new Color(0.30f, 0.55f, 0.25f), collider: false);
            // Give the whole tree a single capsule collider region via a box on the root.
            var col = root.AddComponent<CapsuleCollider>();
            col.height = 3.2f; col.radius = 0.9f; col.center = new Vector3(0f, 1.3f, 0f);
            return root;
        }

        private static GameObject BuildMetal(Transform parent, Vector3 pos, Color color)
        {
            var root = new GameObject("Metal Node");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;
            Prim.Create(PrimitiveType.Cube, "Base", root.transform,
                new Vector3(0f, 0.35f, 0f), new Vector3(1.4f, 0.7f, 1.4f), new Color(0.4f, 0.38f, 0.35f), collider: false);
            Prim.Create(PrimitiveType.Cube, "Ore", root.transform,
                new Vector3(0.2f, 0.9f, 0.1f), new Vector3(0.7f, 0.8f, 0.7f), color, collider: false)
                .transform.localRotation = Quaternion.Euler(20f, 30f, 15f);
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(1.5f, 1.4f, 1.5f); col.center = new Vector3(0f, 0.7f, 0f);
            return root;
        }
    }
}
