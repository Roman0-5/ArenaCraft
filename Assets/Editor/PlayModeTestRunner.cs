using System;
using System.Collections.Generic;
using System.Reflection;
using ArenaCraft;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ArenaCraft.Editor
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "ArenaCraft.FlowValidation.State";
        private const string ResultKey = "ArenaCraft.FlowValidation.Result";
        private const float TimeoutSeconds = 25f;

        private static readonly List<string> Results = new List<string>();
        private static double s_StartedAt;
        private static int s_Step;
        private static GamePhaseManager s_Manager;
        private static PlayerInputProvider[] s_Players;
        private static ResourceNode s_TestNode;
        private static PlayerInventory s_TestInventory;
        private static Health s_P1Health;
        private static Health s_P2Health;
        private static int s_P2HealthBeforeBattleHit;

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            if (state == "EnterPlayMode")
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.delayCall += BeginValidation;
                else
                    EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            }
            else if (state == "Running" && EditorApplication.isPlaying)
            {
                EditorApplication.update += Tick;
            }
            else if (state == "LeavingPlayMode" && !EditorApplication.isPlaying)
            {
                EditorApplication.delayCall += Report;
            }
        }

        [MenuItem("ArenaCraft/Run Full Game Flow Validation")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[ArenaCraft Validation] Stop Play Mode before starting validation.");
                return;
            }

            Results.Clear();
            SessionState.SetString(ResultKey, "");
            SessionState.SetString(StateKey, "EnterPlayMode");
            PlayModeStartScene.Configure();
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.isPlaying = true;
        }

        [MenuItem("ArenaCraft/Validate Real Harvest Interaction")]
        public static void ValidateRealHarvestInteraction()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            PlayerInputProvider[] players =
                UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            Array.Sort(players, (a, b) => ((int)a.Slot).CompareTo((int)b.Slot));
            if (players.Length < 1)
                throw new InvalidOperationException("Harvest validation could not find Player 1.");

            PlayerInputProvider player = players[0];
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            Health health = player.GetComponent<Health>();
            AttackHitbox hitbox = player.GetComponent<MeleeAttack>()?.hitbox;
            if (inventory == null || health == null || hitbox == null)
                throw new InvalidOperationException("Harvest interaction components are not fully wired.");

            hitbox.owner = health;
            InvokePrivate(hitbox, "Awake");

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                ResourceNode node = Array.Find(
                    UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None),
                    candidate => candidate.resourceType == type);
                if (node == null)
                    throw new InvalidOperationException($"Harvest validation could not find a {type} resource.");

                InvokePrivate(node, "Awake");
                Collider resourceCollider = node.GetComponent<Collider>();
                if (resourceCollider == null)
                    throw new InvalidOperationException($"{node.name} has no harvest collider.");

                GameObject originalVisuals = node.visuals;
                AudioClip originalSound = node.hitSound;
                ParticleSystem originalEffect = node.harvestEffect;
                node.visuals = null;
                node.hitSound = null;
                node.harvestEffect = null;

                int healthBefore = node.CurrentHealth;
                int resourcesBefore = inventory.CurrentResources;
                Vector3 target = resourceCollider.bounds.center;
                player.transform.SetPositionAndRotation(
                    new Vector3(target.x, player.transform.position.y, target.z - 1.45f),
                    Quaternion.identity);
                Physics.SyncTransforms();

                hitbox.BeginSwing(10f);
                hitbox.EndSwing();

                node.visuals = originalVisuals;
                node.hitSound = originalSound;
                node.harvestEffect = originalEffect;

                if (node.CurrentHealth >= healthBefore || inventory.CurrentResources <= resourcesBefore)
                    throw new InvalidOperationException(
                        $"Player hitbox did not harvest the {type} world resource.");
            }

            Debug.Log(
                $"[ArenaCraft Harvest Validation] PASS: {player.name} harvested Wood, Stone and Metal through the real attack hitbox.");
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new MissingMethodException(target.GetType().Name, methodName);
            method.Invoke(target, null);
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
                BeginValidation();
            }
        }

        private static void BeginValidation()
        {
            EditorApplication.isPaused = false;
            SessionState.SetString(StateKey, "Running");
            Results.Clear();
            s_StartedAt = EditorApplication.timeSinceStartup;
            s_Step = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            try
            {
                if (EditorApplication.timeSinceStartup - s_StartedAt > TimeoutSeconds)
                    throw new TimeoutException($"Validation timed out during step {s_Step}.");

                switch (s_Step)
                {
                    case 0:
                        if (SceneManager.GetActiveScene().name != SceneNavigation.MainMenuScene) return;
                        ValidateMainMenuAndStartMatch();
                        s_Step++;
                        break;
                    case 1:
                        GamePhaseManager phaseManager = UnityEngine.Object.FindAnyObjectByType<GamePhaseManager>();
                        SplitScreenManager cameraManager = UnityEngine.Object.FindAnyObjectByType<SplitScreenManager>();
                        if (phaseManager == null || cameraManager == null) return;

                        phaseManager.BeginMatch();
                        if (phaseManager.CurrentPhase == GamePhase.None || !cameraManager.IsInitialized) return;

                        ValidateSceneAndEconomy();
                        s_Step++;
                        break;
                    case 2:
                        if (s_Manager.CurrentPhase != GamePhase.Resource) return;
                        ValidateResourcePhase();
                        ActivatePhase(GamePhase.Shopping, 1f);
                        s_Step++;
                        break;
                    case 3:
                        if (s_Manager.CurrentPhase != GamePhase.Shopping) return;
                        ValidateShoppingPhase();
                        ActivatePhase(GamePhase.BattleRoyale, 0f);
                        ValidateBattlePhase();
                        s_P2Health.TakeDamage(10000f);
                        s_Step = 5;
                        break;
                    case 5:
                        if (GameObject.Find("VictoryUI") == null) return;
                        ValidateVictoryAndReturnToMenu();
                        s_Step++;
                        break;
                    case 6:
                        if (SceneManager.GetActiveScene().name != SceneNavigation.MainMenuScene) return;
                        Require(UnityEngine.Object.FindAnyObjectByType<MainMenuController>() != null,
                            "victory screen returns to Main Menu");
                        Finish(true);
                        break;
                }
            }
            catch (Exception exception)
            {
                Results.Add($"FAIL: {exception.Message}");
                Debug.LogException(exception);
                Finish(false);
            }
        }

        private static void ValidateMainMenuAndStartMatch()
        {
            MainMenuController controller = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
            Require(controller != null, "fresh Play starts in Main Menu");

            UIDocument document = controller.GetComponent<UIDocument>();
            Require(document != null, "Main Menu UI document exists");
            VisualElement root = document.rootVisualElement;
            Require(root.Q<Button>("start-button") != null, "Main Menu start button exists");
            Button classicButton = root.Q<Button>("mode-classic-button");
            Require(classicButton != null && classicButton.text == "CLASSIC",
                "Classic mode uses the player-facing CLASSIC label");
            Require(root.Q<Button>("mode-quick-button") != null,
                "match rule selection exists");
            Require(root.Q<Button>("camera-shared-button") != null && root.Q<Button>("camera-split-button") != null,
                "camera mode selection exists");

            MethodInfo start = typeof(MainMenuController).GetMethod(
                "OnStartClicked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Require(start != null, "Main Menu start action is wired");
            start.Invoke(controller, null);
        }

        private static void ValidateSceneAndEconomy()
        {
            s_Manager = UnityEngine.Object.FindAnyObjectByType<GamePhaseManager>();
            Require(s_Manager != null, "GamePhaseManager exists");

            s_Players = UnityEngine.Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            Array.Sort(s_Players, (a, b) => ((int)a.Slot).CompareTo((int)b.Slot));
            Require(s_Players.Length == 2, "exactly two players exist");
            s_P1Health = s_Players[0].GetComponent<Health>();
            s_P2Health = s_Players[1].GetComponent<Health>();
            Require(s_P1Health != null && s_P2Health != null, "both players have Health");
            Require(Mathf.Approximately(Time.timeScale, 1f), "match starts unpaused");
            PauseMenuController pauseMenu = UnityEngine.Object.FindAnyObjectByType<PauseMenuController>();
            VisualElement pauseRoot = pauseMenu != null
                ? pauseMenu.GetComponent<UIDocument>()?.rootVisualElement.Q<VisualElement>("pause-root")
                : null;
            Require(pauseRoot != null && pauseRoot.resolvedStyle.display == DisplayStyle.None,
                "pause overlay starts hidden");
            ValidatePlayerMovement();

            var resourceTypes = new HashSet<ResourceType>();
            ResourceNode[] nodes = UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            foreach (ResourceNode node in nodes)
                resourceTypes.Add(node.resourceType);
            Require(resourceTypes.SetEquals(new[] { ResourceType.Wood, ResourceType.Stone, ResourceType.Metal }),
                "Wood, Stone and Metal nodes exist");
            Require(nodes.Length == 30, "arena contains 30 resource nodes");
            Require(Array.FindAll(nodes, node => node.resourceType == ResourceType.Wood).Length == 12,
                "arena contains twelve Wood nodes");
            Require(Array.FindAll(nodes, node => node.resourceType == ResourceType.Stone).Length == 10,
                "arena contains ten Stone nodes");
            Require(Array.FindAll(nodes, node => node.resourceType == ResourceType.Metal).Length == 8,
                "arena contains eight Metal nodes");
            Require(Array.TrueForAll(nodes, node =>
                    node.visuals != null && node.GetComponent<Collider>() != null && node.hitSound != null),
                "all resource nodes keep visuals, colliders and harvest audio");
            Require(Array.TrueForAll(
                    Array.FindAll(nodes, node => node.resourceType == ResourceType.Metal),
                    node => node.visuals.name == "MetalOreVisual" &&
                            node.visuals.GetComponentsInChildren<Renderer>(true).Length >= 3),
                "Metal nodes use visible multi-piece ore clusters");
            Require(Array.TrueForAll(nodes, node =>
                    !node.GetComponent<Collider>().isTrigger &&
                    node.GetComponentInChildren<ResourceNodeIndicator>(true) != null),
                "all resource nodes block players and show harvest indicators");
            Require(Array.TrueForAll(nodes, HasUsableResourceCollider),
                "resource colliders match the visible harvest props");
            Require(Array.TrueForAll(nodes, node =>
                    node.TotalYield >= 28 && node.respawnTime >= 10f && node.respawnVariance > 0f),
                "resource nodes have balanced yields and staggered respawns");

            SplitScreenManager splitScreen = UnityEngine.Object.FindAnyObjectByType<SplitScreenManager>();
            bool expectedSplitScreen = PlayerPrefs.GetInt(SplitScreenManager.PreferenceKey, 0) == 1;
            Require(splitScreen != null && splitScreen.IsSplitScreen == expectedSplitScreen,
                "selected camera mode is applied");
            Camera[] gameplayCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (expectedSplitScreen)
            {
                Require(Array.FindAll(gameplayCameras, camera => camera.enabled && camera.rect.width == 0.5f).Length == 2,
                    "two half-width player cameras are active");
            }
            else
            {
                Require(Array.FindAll(gameplayCameras, camera => camera.enabled).Length == 1,
                    "one shared camera is active");
            }

            Require(Mathf.Approximately(MatchRules.ResourcePhaseDuration,
                    MatchRules.Current == MatchRuleSet.GddClassic ? 180f : 75f),
                "selected match rules are applied");

            GameObject inventoryObject = new GameObject("FlowValidationInventory");
            s_TestInventory = inventoryObject.AddComponent<PlayerInventory>();
            Require(s_TestInventory.AddResource(ResourceType.Wood, 10) == 10 && s_TestInventory.Gold == 10,
                "Wood converts at 1 gold");
            Require(s_TestInventory.AddResource(ResourceType.Stone, 10) == 10 && s_TestInventory.Gold == 30,
                "Stone converts at 2 gold");
            Require(s_TestInventory.AddResource(ResourceType.Metal, 90) == 80 && s_TestInventory.Gold == 430,
                "Metal converts at 5 gold and capacity clamps at 100");
            Require(s_TestInventory.GetResourceCount(ResourceType.Wood) == 10 &&
                    s_TestInventory.GetResourceCount(ResourceType.Stone) == 10 &&
                    s_TestInventory.GetResourceCount(ResourceType.Metal) == 80,
                "inventory tracks each resource type");
            Require(s_TestInventory.IsFull && s_TestInventory.AddResource(ResourceType.Wood, 1) == 0,
                "full inventory rejects resources");
            Require(s_TestInventory.SpendGold(100) && s_TestInventory.Gold == 330,
                "valid purchase spends gold");
            Require(!s_TestInventory.SpendGold(0) && !s_TestInventory.SpendGold(-1),
                "zero and negative purchases are rejected");
            UnityEngine.Object.Destroy(inventoryObject);

            GameObject nodeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nodeObject.name = "FlowValidationNode";
            s_TestNode = nodeObject.AddComponent<ResourceNode>();
            s_TestNode.resourceType = ResourceType.Wood;
            s_TestNode.resourcesPerHit = 10;
            s_TestNode.depletionBonus = 5;
            s_TestNode.respawnTime = 0.2f;
            s_TestNode.respawnVariance = 0f;
            s_TestNode.respawnWarningTime = 0.1f;

            GameObject harvestObject = new GameObject("FlowValidationHarvester");
            s_TestInventory = harvestObject.AddComponent<PlayerInventory>();
        }

        private static void ValidateResourcePhase()
        {
            Require(s_TestNode.TakeDamage(1, s_TestInventory), "resource node accepts hits in Resource phase");
            Require(s_TestNode.CurrentHealth == 2 && s_TestInventory.CurrentResources == 10,
                "resource hit damages node and awards resources");
            Require(s_TestInventory.GetResourceCount(ResourceType.Wood) == 10,
                "harvested resource type is recorded");
            Require(s_TestNode.TakeDamage(1, s_TestInventory) && s_TestNode.TakeDamage(1, s_TestInventory),
                "resource node can be fully harvested");
            Require(s_TestInventory.CurrentResources == 35,
                "fully harvesting a node awards its depletion bonus");
            Require(s_TestNode.IsDestroyed && s_TestNode.State == ResourceNode.NodeState.Respawning,
                "depleted resource node starts its respawn countdown");
            Require(s_TestNode.RespawnRemaining > 0f,
                "depleted resource node receives a positive respawn timer");
            s_TestNode.RestoreNode();
            Require(!s_TestNode.IsDestroyed &&
                    s_TestNode.State == ResourceNode.NodeState.Available &&
                    s_TestNode.CurrentHealth == s_TestNode.maxHealth &&
                    s_TestNode.GetComponent<Collider>().enabled,
                "respawn restores node health, state and collision");

            ValidateRealPlayerHarvestHit();

            int healthBefore = s_P2Health.CurrentHP;
            TriggerPlayerHit(s_Players[0], s_Players[1]);
            Require(s_P2Health.CurrentHP == healthBefore, "PvP damage is blocked in Resource phase");
        }

        private static void ValidatePlayerMovement()
        {
            Require(Keyboard.current != null, "keyboard input device exists");
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W, Key.UpArrow));
            InputSystem.Update();

            Require(s_Players[0].ReadMove().y > 0.5f, "Player 1 receives WASD movement");
            Require(s_Players[1].ReadMove().y > 0.5f, "Player 2 receives arrow-key movement");

            MethodInfo update = typeof(ArenaPlayerController).GetMethod(
                "Update",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo fixedUpdate = typeof(ArenaPlayerController).GetMethod(
                "FixedUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Require(update != null && fixedUpdate != null, "player movement loop is available");

            foreach (PlayerInputProvider player in s_Players)
            {
                ArenaPlayerController controller = player.GetComponent<ArenaPlayerController>();
                Rigidbody body = player.GetComponent<Rigidbody>();
                Require(controller != null && controller.enabled && body != null && !body.isKinematic,
                    $"{player.Slot} movement components are active");
                update.Invoke(controller, null);
                fixedUpdate.Invoke(controller, null);
                FieldInfo speed = typeof(ArenaPlayerController).GetField(
                    "currentPlanarSpeed",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Require(speed != null && (float)speed.GetValue(controller) > 1f,
                    $"{player.Slot} produces movement");
            }

            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            InputSystem.Update();

            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Space, Key.Enter));
            InputSystem.Update();
            Require(s_Players[0].WasAttackPressedThisFrame(), "Player 1 receives Space attack input");
            Require(s_Players[1].WasAttackPressedThisFrame(), "Player 2 receives Enter attack input");

            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            InputSystem.Update();
        }

        private static void ValidateRealPlayerHarvestHit()
        {
            ResourceNode realNode = Array.Find(
                UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None),
                node => node != null && node.name.StartsWith("Resource_", StringComparison.Ordinal));
            Require(realNode != null, "a real scene resource node is available for hitbox validation");

            PlayerInputProvider player = s_Players[0];
            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            AttackHitbox hitbox = player.GetComponent<MeleeAttack>()?.hitbox;
            Collider resourceCollider = realNode.GetComponent<Collider>();
            Rigidbody body = player.GetComponent<Rigidbody>();
            Require(inventory != null && hitbox != null && resourceCollider != null && body != null,
                "player harvesting components are fully wired");

            int resourcesBefore = inventory.CurrentResources;
            int healthBefore = realNode.CurrentHealth;
            ResourceNodeIndicator indicator =
                realNode.GetComponentInChildren<ResourceNodeIndicator>(true);
            Vector3 target = resourceCollider.bounds.center;
            Vector3 playerPosition = new Vector3(
                target.x,
                player.transform.position.y,
                target.z - 1.45f);
            player.transform.SetPositionAndRotation(playerPosition, Quaternion.identity);
            Physics.SyncTransforms();
            Collider attackCollider = hitbox.GetComponent<Collider>();
            player.transform.position += target - attackCollider.bounds.center;
            Physics.SyncTransforms();

            hitbox.BeginSwing(10f);
            hitbox.EndSwing();

            Require(realNode.CurrentHealth < healthBefore,
                "real player hitbox damages a world resource node");
            Require(inventory.CurrentResources > resourcesBefore,
                "real player hitbox awards harvested resources");
            Require(indicator != null, "world resource has a harvest indicator");
            MethodInfo indicatorUpdate = typeof(ResourceNodeIndicator).GetMethod(
                "LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            indicatorUpdate?.Invoke(indicator, null);
            Require(indicator.IsVisible,
                "nearby resource indicator is visible to the player");
            Require(indicator.DetailText.Contains("HARVEST") &&
                    !indicator.DetailText.Contains("MOVE CLOSER"),
                "resource indicator gives a direct readable harvest action");
            Require(Mathf.Abs(indicator.DisplayedProgress - realNode.HealthNormalized) < 0.01f,
                "resource indicator tracks remaining node durability");
            realNode.RestoreNode();
        }

        private static bool HasUsableResourceCollider(ResourceNode node)
        {
            Collider collider = node.GetComponent<Collider>();
            if (collider is CapsuleCollider capsule)
                return capsule.radius >= 0.65f && capsule.height >= 2.4f;
            if (collider is BoxCollider box)
                return box.size.x >= 1.15f && box.size.y >= 0.95f && box.size.z >= 1.15f;
            return false;
        }

        private static void ValidateShoppingPhase()
        {
            Require(!s_TestNode.TakeDamage(1, s_TestInventory), "resource harvesting is blocked in Shopping phase");
            Require(!s_Players[0].enabled && !s_Players[1].enabled, "player controls are locked while shopping");
            ValidateShopLayout();

            int healthBefore = s_P2Health.CurrentHP;
            TriggerPlayerHit(s_Players[0], s_Players[1]);
            Require(s_P2Health.CurrentHP == healthBefore, "PvP damage is blocked in Shopping phase");
        }

        private static void ValidateShopLayout()
        {
            ShopController shop = UnityEngine.Object.FindAnyObjectByType<ShopController>();
            UIDocument document = shop != null ? shop.GetComponent<UIDocument>() : null;
            Require(document != null, "shop UI document exists");

            VisualElement root = document.rootVisualElement;
            List<VisualElement> cards = root.Query<VisualElement>(className: "shop-item").ToList();
            Require(cards.Count == 4, "shop contains four equipment cards");
            foreach (VisualElement card in cards)
            {
                Rect cardBounds = card.worldBound;
                foreach (Label label in card.Query<Label>().ToList())
                {
                    Rect labelBounds = label.worldBound;
                    Require(
                        labelBounds.yMin >= cardBounds.yMin - 0.5f &&
                        labelBounds.yMax <= cardBounds.yMax + 0.5f,
                        $"{label.text} stays inside its shop card");
                }
            }

            VisualElement grid = root.Q<VisualElement>("items-grid");
            VisualElement footer = root.Q<VisualElement>(className: "shop-footer");
            Require(grid != null && footer != null && grid.worldBound.yMax <= footer.worldBound.yMin + 0.5f,
                "shop cards do not overlap the footer");
        }

        private static void ValidateBattlePhase()
        {
            Require(s_Players[0].enabled && s_Players[1].enabled, "player controls return for Battle Royale");
            Require(!s_TestNode.TakeDamage(1, s_TestInventory), "resource harvesting is blocked in Battle Royale");

            s_P2HealthBeforeBattleHit = s_P2Health.CurrentHP;
            TriggerPlayerHit(s_Players[0], s_Players[1]);
            Require(s_P2Health.CurrentHP < s_P2HealthBeforeBattleHit, "PvP damage works in Battle Royale");
        }

        private static void ValidateVictoryAndReturnToMenu()
        {
            GameObject victoryObject = GameObject.Find("VictoryUI");
            UIDocument document = victoryObject.GetComponent<UIDocument>();
            Require(document != null, "victory screen appears after elimination");
            Require(document.rootVisualElement.Q<Button>("rematch-button") != null,
                "victory screen provides rematch");
            Require(document.rootVisualElement.Q<Button>("main-menu-button") != null,
                "victory screen provides Main Menu return");

            MatchEndHandler handler = UnityEngine.Object.FindAnyObjectByType<MatchEndHandler>();
            Require(handler != null, "match end handler remains available");
            handler.ReturnToMainMenu();
        }

        private static void TriggerPlayerHit(PlayerInputProvider attacker, PlayerInputProvider victim)
        {
            AttackHitbox hitbox = attacker.GetComponentInChildren<AttackHitbox>(true);
            Collider victimCollider = victim.GetComponent<Collider>();
            Require(hitbox != null && victimCollider != null, "combat hitbox and victim collider exist");

            hitbox.BeginSwing(10f);
            MethodInfo trigger = typeof(AttackHitbox).GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(trigger != null, "AttackHitbox trigger method is available");
            trigger.Invoke(hitbox, new object[] { victimCollider });
            hitbox.EndSwing();
        }

        private static void ActivatePhase(GamePhase phase, float duration)
        {
            MethodInfo enterPhase = typeof(GamePhaseManager).GetMethod(
                "EnterPhase",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Require(enterPhase != null, $"{phase} phase transition is available");
            enterPhase.Invoke(s_Manager, new object[] { phase, duration });
            Require(s_Manager.CurrentPhase == phase, $"{phase} phase initializes");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            Results.Add($"PASS: {message}");
        }

        private static void Finish(bool success)
        {
            EditorApplication.update -= Tick;
            Results.Insert(0, success ? "PASS: FULL GAME FLOW" : "FAIL: FULL GAME FLOW");
            SessionState.SetString(ResultKey, string.Join("\n", Results));
            SessionState.SetString(StateKey, "LeavingPlayMode");
            EditorApplication.playModeStateChanged += HandleExitPlayMode;
            EditorApplication.isPlaying = false;
        }

        private static void HandleExitPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= HandleExitPlayMode;
            Report();
        }

        private static void Report()
        {
            string result = SessionState.GetString(ResultKey, "FAIL: No validation result was produced.");
            if (result.StartsWith("PASS"))
                Debug.Log($"[ArenaCraft Validation]\n{result}");
            else
                Debug.LogError($"[ArenaCraft Validation]\n{result}");

            SessionState.SetString(StateKey, "Idle");
        }
    }
}
