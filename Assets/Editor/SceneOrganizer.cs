using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArenaCraft.EditorTools
{
    /// <summary>
    /// One-shot scene tidy-up: groups loose root decoration GameObjects (rocks, trees, fences,
    /// lanterns, etc.) under sensible parent containers. Preserves world transforms via
    /// Undo.SetTransformParent. Never touches gameplay/manager objects (players, cameras, UI,
    /// managers, lighting, the new arena prefab).
    /// </summary>
    public static class SceneOrganizer
    {
        private const string ItemsRoot = "Items";

        // Pattern → parent name. Order matters: first match wins (so "tree trunk" hits
        // Decorations before a hypothetical "Trunk" group).
        // Rocks, Bushes, and Lanterns get their own subfolder. Everything else decorative
        // (barrels, trees, flowers, mushrooms, stairs, plants, grass, ground...) goes into
        // a catch-all Decorations subfolder so the hierarchy stays compact.
        private static readonly (string Parent, string[] Patterns)[] s_Rules = new (string, string[])[]
        {
            ("Rocks",       new[] { "rock", "stone" }),
            ("Bushes",      new[] { "bush", "shrub" }),
            ("Lanterns",    new[] { "lantern" }),
            ("Decorations", new[] {
                "barrel", "stair", "mushroom",
                "tree", "dry tree", "dry_tree",
                "flower", "plants", "grass", "ground",
            }),
        };

        // Roots that must NEVER be moved. Anything not matched by a rule below is also left alone.
        private static readonly HashSet<string> s_Preserve = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Directional Light", "Plane", "Light_08",
            "RPGHeroHP", "Player2",
            "Camera", "Controls", "KeybindMenu", "Managers",
            "ShopZone", "Shop", "Environment", "EventSystem", "GlobalVolume",
            "PauseMenu", "SettingsManager", "Decoration",
            "ArenaResources", "Arena", "Arena (1)",
            "Gladiator Low Poly Arena",
            ItemsRoot,
        };

        // Manually-grouped containers the user has already created. We move these under
        // the Items root so everything decoration-related lives in one place.
        private static readonly string[] s_AdoptUnderItems =
        {
            "Stolzen", "Fences",
        };

        [MenuItem("ArenaCraft/Organize Scene Decoration")]
        public static void Organize()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            Dictionary<string, Transform> parents = new Dictionary<string, Transform>();

            Transform itemsRoot = GetOrCreateRoot(ItemsRoot);
            int moved = 0;

            foreach (GameObject root in roots)
            {
                if (root == null) continue;
                if (s_Preserve.Contains(root.name)) continue;
                if (TryFindCategory(root.name, out string categoryName))
                {
                    Transform parent = GetOrCreateCategoryParent(categoryName, itemsRoot, parents);
                    if (root.transform == parent) continue;
                    Undo.SetTransformParent(root.transform, parent, "Organize Scene Decoration");
                    moved++;
                }
            }

            // Adopt existing user-made groups (Stolzen, Fences) under Items so everything sits together.
            foreach (string adoptName in s_AdoptUnderItems)
            {
                GameObject group = GameObject.Find(adoptName);
                if (group == null) continue;
                if (group.transform.parent == itemsRoot) continue;
                Undo.SetTransformParent(group.transform, itemsRoot, "Organize Scene Decoration");
                moved++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[ArenaCraft] Organize Scene: moved {moved} object(s) into '{ItemsRoot}'. Save the scene to persist.");
        }

        [MenuItem("ArenaCraft/Organize Scene Decoration (Dry Run)")]
        public static void OrganizeDryRun()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();
            Dictionary<string, int> counts = new Dictionary<string, int>();
            int skipped = 0;

            foreach (GameObject root in roots)
            {
                if (root == null) continue;
                if (s_Preserve.Contains(root.name)) { skipped++; continue; }
                if (TryFindCategory(root.name, out string categoryName))
                {
                    counts.TryGetValue(categoryName, out int n);
                    counts[categoryName] = n + 1;
                }
                else
                {
                    skipped++;
                }
            }

            string summary = $"Would move into '{ItemsRoot}':\n";
            foreach (KeyValuePair<string, int> kv in counts) summary += $"  → {ItemsRoot}/{kv.Key}: {kv.Value}\n";
            foreach (string adoptName in s_AdoptUnderItems)
            {
                if (GameObject.Find(adoptName) != null) summary += $"  → {ItemsRoot}/{adoptName} (adopt existing group)\n";
            }
            summary += $"Untouched: {skipped} root(s).";
            Debug.Log("[ArenaCraft] Organize Scene (Dry Run):\n" + summary);
        }

        private static bool TryFindCategory(string objectName, out string categoryName)
        {
            string lower = objectName.ToLowerInvariant();
            foreach ((string parent, string[] patterns) in s_Rules)
            {
                foreach (string pattern in patterns)
                {
                    if (lower.Contains(pattern))
                    {
                        categoryName = parent;
                        return true;
                    }
                }
            }

            categoryName = null;
            return false;
        }

        private static Transform GetOrCreateRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null && existing.transform.parent == null) return existing.transform;

            GameObject created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Create " + name);
            return created.transform;
        }

        private static Transform GetOrCreateCategoryParent(string name, Transform parentRoot, Dictionary<string, Transform> cache)
        {
            if (cache.TryGetValue(name, out Transform cached) && cached != null) return cached;

            // Prefer an existing child of parentRoot with this name (re-running the tool shouldn't duplicate).
            Transform underRoot = parentRoot.Find(name);
            if (underRoot != null)
            {
                cache[name] = underRoot;
                return underRoot;
            }

            GameObject created = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(created, "Create " + name);
            Undo.SetTransformParent(created.transform, parentRoot, "Organize Scene Decoration");
            cache[name] = created.transform;
            return created.transform;
        }
    }
}
