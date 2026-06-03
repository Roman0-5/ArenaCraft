using System.Collections.Generic;
using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Builds the colosseum arena from primitives: a sandy circular floor, a ring
    /// of stone walls that keep players inside, pillars and flickering torches.
    /// Warm earth-tone palette per GDD 1.5 / 7.2.
    /// </summary>
    public static class ArenaBuilder
    {
        public static readonly Color Sand = new Color(0.82f, 0.70f, 0.47f);
        public static readonly Color Stone = new Color(0.46f, 0.43f, 0.40f);
        public static readonly Color DarkStone = new Color(0.30f, 0.28f, 0.26f);
        public static readonly Color Terracotta = new Color(0.70f, 0.36f, 0.24f);

        public static GameObject Build(GameSettings settings, Transform parent)
        {
            var root = new GameObject("Arena");
            root.transform.SetParent(parent, false);

            float r = settings.arenaRadius;

            // --- Floor ---
            var floor = Prim.Create(PrimitiveType.Cylinder, "Floor", root.transform,
                new Vector3(0f, -0.5f, 0f), new Vector3(r * 2f + 2f, 0.5f, r * 2f + 2f), Sand);

            // --- Ring of walls (segments around the perimeter) ---
            int segments = 40;
            var wallParent = new GameObject("Walls").transform;
            wallParent.SetParent(root.transform, false);
            for (int i = 0; i < segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                float wallR = r + 0.9f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * wallR, 1.1f, Mathf.Sin(ang) * wallR);
                float segLen = (Mathf.PI * 2f * wallR) / segments + 0.4f;
                var seg = Prim.Create(PrimitiveType.Cube, "Wall", wallParent,
                    pos, new Vector3(0.8f, 2.2f, segLen), (i % 2 == 0) ? Stone : DarkStone);
                seg.transform.localRotation = Quaternion.Euler(0f, -ang * Mathf.Rad2Deg, 0f);
            }

            // --- Pillars + torches every 45 degrees ---
            for (int i = 0; i < 8; i++)
            {
                float ang = (i / 8f) * Mathf.PI * 2f;
                float pr = r - 0.6f;
                Vector3 basePos = new Vector3(Mathf.Cos(ang) * pr, 1.4f, Mathf.Sin(ang) * pr);
                Prim.Create(PrimitiveType.Cylinder, "Pillar", root.transform,
                    basePos, new Vector3(1.1f, 1.4f, 1.1f), Stone);

                // Torch flame – emissive sphere + point light.
                var torch = new GameObject("Torch");
                torch.transform.SetParent(root.transform, false);
                torch.transform.localPosition = basePos + new Vector3(0f, 1.6f, 0f);
                var flame = Prim.Create(PrimitiveType.Sphere, "Flame", torch.transform,
                    Vector3.zero, Vector3.one * 0.45f, new Color(1f, 0.55f, 0.12f), collider: false);
                var fr = flame.GetComponent<MeshRenderer>();
                if (fr != null && fr.sharedMaterial.HasProperty("_EmissionColor"))
                {
                    fr.sharedMaterial.EnableKeyword("_EMISSION");
                    fr.sharedMaterial.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.1f) * 2.5f);
                }
                var light = torch.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.6f, 0.25f);
                light.range = 9f;
                light.intensity = 2.2f;
                torch.AddComponent<TorchFlicker>().Init(light);
            }

            // --- A subtle ground tint ring marking the central high-risk zone (GDD 3.2.2) ---
            Prim.Create(PrimitiveType.Cylinder, "CenterMarker", root.transform,
                new Vector3(0f, -0.24f, 0f), new Vector3(r * 0.5f, 0.26f, r * 0.5f),
                new Color(0.78f, 0.62f, 0.40f), collider: false);

            return root;
        }

        /// <summary>Evenly spread points used as battle spawn / shop positions.</summary>
        public static List<Vector3> RingPoints(float radius, int count, float y = 1f)
        {
            var list = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                float ang = (i / (float)count) * Mathf.PI * 2f;
                list.Add(new Vector3(Mathf.Cos(ang) * radius, y, Mathf.Sin(ang) * radius));
            }
            return list;
        }
    }

    /// <summary>Cheap torch flicker so the arena feels alive (GDD 1.5 torchlight mood).</summary>
    public class TorchFlicker : MonoBehaviour
    {
        private Light _light;
        private float _seed;
        public void Init(Light l) { _light = l; _seed = Random.value * 10f; }
        private void Update()
        {
            if (_light == null) return;
            _light.intensity = 1.8f + Mathf.PerlinNoise(_seed, Time.time * 6f) * 1.4f;
        }
    }
}
