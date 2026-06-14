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

        [MenuItem("ArenaCraft/Build GDD Arena Resources")]
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
