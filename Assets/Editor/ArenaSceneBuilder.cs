using System;
using System.Collections.Generic;
using System.Linq;
using ArenaCraft;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaCraft.Editor
{
    internal static class ArenaSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string MetalVisualPrefabPath =
            "Assets/Palmov Island/Low Poly Atmospheric Locations Pack/Prefabs/Environment/coal.prefab";

        private static readonly Dictionary<ResourceType, Vector3[]> ResourcePositions =
            new Dictionary<ResourceType, Vector3[]>
            {
                {
                    ResourceType.Wood,
                    new[]
                    {
                        new Vector3(-14f, 0f, -10f), new Vector3(14f, 0f, -10f),
                        new Vector3(-16f, 0f, 2f), new Vector3(16f, 0f, 2f),
                        new Vector3(-12f, 0f, 12f), new Vector3(12f, 0f, 12f),
                        new Vector3(-17f, 0f, -5f), new Vector3(17f, 0f, -5f),
                        new Vector3(-17f, 0f, 9f), new Vector3(17f, 0f, 9f),
                        new Vector3(-7f, 0f, 15f), new Vector3(7f, 0f, 15f)
                    }
                },
                {
                    ResourceType.Stone,
                    new[]
                    {
                        new Vector3(-9f, 0f, -7f), new Vector3(9f, 0f, -7f),
                        new Vector3(-10f, 0f, 7f), new Vector3(10f, 0f, 7f),
                        new Vector3(-4f, 0f, 12f), new Vector3(4f, 0f, 12f),
                        new Vector3(-13f, 0f, -2f), new Vector3(13f, 0f, -2f),
                        new Vector3(-13f, 0f, 13f), new Vector3(13f, 0f, 13f)
                    }
                },
                {
                    ResourceType.Metal,
                    new[]
                    {
                        new Vector3(-4f, 0f, -3f), new Vector3(4f, 0f, -3f),
                        new Vector3(-5f, 0f, 4f), new Vector3(5f, 0f, 4f),
                        new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, 8f),
                        new Vector3(-8f, 0f, 1f), new Vector3(8f, 0f, 1f)
                    }
                }
            };

        [MenuItem("ArenaCraft/Build Classic Arena Resources")]
        public static void BuildResources()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before rebuilding the arena.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ResourceNode[] existingNodes = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            Dictionary<ResourceType, ResourceNode> templates = existingNodes
                .GroupBy(node => node.resourceType)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (!templates.ContainsKey(type))
                    throw new InvalidOperationException($"Scene is missing a {type} resource template.");
            }

            GameObject oldRoot = GameObject.Find("ArenaResources");
            GameObject root = new GameObject("ArenaResources_New");
            foreach (KeyValuePair<ResourceType, Vector3[]> entry in ResourcePositions)
            {
                ResourceNode template = templates[entry.Key];
                for (int index = 0; index < entry.Value.Length; index++)
                {
                    GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, root.transform);
                    clone.name = $"Resource_{entry.Key}_{index + 1:00}";
                    clone.transform.position = entry.Value[index];
                    clone.transform.rotation = Quaternion.Euler(0f, (index * 57f) % 360f, 0f);
                    float scaleVariation = 0.9f + (index % 4) * 0.06f;
                    clone.transform.localScale = Vector3.one * scaleVariation;

                    ResourceNode node = clone.GetComponent<ResourceNode>();
                    ConfigureNode(node, entry.Key);
                    ConfigureResourceVisual(node);
                    node.FitBlockingColliderToVisuals();
                }
            }

            foreach (ResourceNode node in existingNodes)
            {
                if (node != null) UnityEngine.Object.DestroyImmediate(node.gameObject);
            }
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot);
            root.name = "ArenaResources";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[ArenaCraft] Built 30 harvestable resource nodes and saved SampleScene.");
        }

        [MenuItem("ArenaCraft/Fix Player Harvest Hitboxes")]
        public static void FixPlayerHarvestHitboxes()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before fixing player hitboxes.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AttackHitbox[] hitboxes = UnityEngine.Object.FindObjectsByType<AttackHitbox>(FindObjectsSortMode.None);
            foreach (AttackHitbox hitbox in hitboxes)
            {
                BoxCollider box = hitbox.GetComponent<BoxCollider>();
                if (box == null) continue;
                box.size = new Vector3(1.8f, 2f, 2.2f);
                box.center = new Vector3(0f, -0.2f, 0.35f);
                hitbox.transform.localPosition = new Vector3(0f, 0.9f, 1.05f);
                hitbox.transform.localRotation = Quaternion.identity;
                hitbox.resourceHarvestRadius = 1.35f;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ArenaCraft] Updated {hitboxes.Length} player harvesting hitboxes.");
        }

        [MenuItem("ArenaCraft/Fit Resource Colliders")]
        public static void FitResourceColliders()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before fitting resource colliders.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ResourceNode[] nodes = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            foreach (ResourceNode node in nodes)
                node.FitBlockingColliderToVisuals();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ArenaCraft] Fitted {nodes.Length} resource colliders to their visible models.");
        }

        [MenuItem("ArenaCraft/Improve Resource Visuals")]
        public static void ImproveResourceVisuals()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before improving resource visuals.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ResourceNode[] nodes = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            foreach (ResourceNode node in nodes)
            {
                ConfigureResourceVisual(node);
                node.FitBlockingColliderToVisuals();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ArenaCraft] Centered and improved visuals for {nodes.Length} resource nodes.");
        }

        private static void ConfigureResourceVisual(ResourceNode node)
        {
            if (node.resourceType == ResourceType.Metal)
            {
                GameObject metalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MetalVisualPrefabPath);
                if (metalPrefab == null)
                    throw new InvalidOperationException($"Missing metal visual prefab at {MetalVisualPrefabPath}.");

                if (node.visuals != null)
                    UnityEngine.Object.DestroyImmediate(node.visuals);
                node.visuals = new GameObject("MetalOreVisual");
                node.visuals.transform.SetParent(node.transform, false);
                node.visuals.name = "MetalOreVisual";
                CreateMetalOrePiece(metalPrefab, node.visuals.transform, new Vector3(-0.55f, 0f, 0f), -24f);
                CreateMetalOrePiece(metalPrefab, node.visuals.transform, new Vector3(0.55f, 0f, 0.14f), 28f);
                CreateMetalOrePiece(metalPrefab, node.visuals.transform, new Vector3(0f, 0.16f, -0.32f), 90f);
            }
            else if (node.visuals != null)
            {
                node.visuals.transform.localScale = Vector3.one *
                    (node.resourceType == ResourceType.Wood ? 1.35f : 1.65f);
            }

            if (node.visuals == null) return;
            node.visuals.transform.localPosition = Vector3.zero;
            node.visuals.transform.localRotation = Quaternion.identity;
            foreach (Collider childCollider in node.visuals.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(childCollider);
        }

        private static void CreateMetalOrePiece(
            GameObject prefab,
            Transform parent,
            Vector3 localPosition,
            float yaw)
        {
            GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            piece.name = "OreChunk";
            piece.transform.localPosition = localPosition;
            piece.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            piece.transform.localScale = Vector3.one * 0.5f;
        }

        private static void ConfigureNode(ResourceNode node, ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    node.maxHealth = 3;
                    node.resourcesPerHit = 7;
                    node.depletionBonus = 7;
                    node.respawnTime = 10f;
                    node.respawnVariance = 2f;
                    break;
                case ResourceType.Stone:
                    node.maxHealth = 4;
                    node.resourcesPerHit = 6;
                    node.depletionBonus = 8;
                    node.respawnTime = 14f;
                    node.respawnVariance = 2.5f;
                    break;
                case ResourceType.Metal:
                    node.maxHealth = 5;
                    node.resourcesPerHit = 4;
                    node.depletionBonus = 10;
                    node.respawnTime = 18f;
                    node.respawnVariance = 3f;
                    break;
            }

            node.respawnWarningTime = 1.5f;
        }
    }
}
