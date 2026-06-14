using UnityEngine;
using UnityEngine.Rendering;

namespace ArenaCraft
{
    public sealed class ResourceNodeIndicator : MonoBehaviour
    {
        private const float HarvestRange = 3.1f;
        private const int PlayerOneIndicatorLayer = 30;
        private const int PlayerTwoIndicatorLayer = 31;

        private ResourceNode m_Node;
        private TextMesh m_Title;
        private TextMesh m_Detail;
        private Transform m_Fill;
        private MeshRenderer m_FillRenderer;
        private Renderer[] m_Renderers;
        private PlayerInputProvider[] m_Players;
        private Camera[] m_Cameras;
        private Camera m_BillboardCamera;
        private float m_NextLookupTime;

        public bool IsVisible
        {
            get
            {
                if (this.m_Renderers == null) return false;
                foreach (Renderer renderer in this.m_Renderers)
                {
                    if (renderer != null && renderer.enabled) return true;
                }
                return false;
            }
        }

        public float DisplayedProgress => this.m_Fill != null
            ? Mathf.Clamp01(this.m_Fill.localScale.x / 0.96f)
            : 0f;
        public string DetailText => this.m_Detail != null ? this.m_Detail.text : string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AttachToLoadedNodes()
        {
            foreach (ResourceNode node in
                     Object.FindObjectsByType<ResourceNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                node.FitBlockingColliderToVisuals();
                Attach(node);
            }
        }

        public static ResourceNodeIndicator Attach(ResourceNode node)
        {
            ResourceNodeIndicator existing = node.GetComponentInChildren<ResourceNodeIndicator>(true);
            if (existing != null)
            {
                existing.Initialize(node);
                return existing;
            }

            GameObject indicatorObject = new GameObject("ResourceIndicator");
            indicatorObject.transform.SetParent(node.transform, false);
            ResourceNodeIndicator indicator = indicatorObject.AddComponent<ResourceNodeIndicator>();
            indicator.Initialize(node);
            return indicator;
        }

        private void Initialize(ResourceNode node)
        {
            this.m_Node = node;
            if (this.m_Title == null)
                BuildVisuals();
            transform.position = this.m_Node.IndicatorWorldPosition;
            RefreshReferences();
            UpdateVisuals();
        }

        private void BuildVisuals()
        {
            CreateQuad(
                "Panel",
                transform,
                new Vector3(0f, 0f, 0.04f),
                new Vector3(2.2f, 0.78f, 1f),
                new Color(0.055f, 0.045f, 0.04f, 0.9f),
                30);

            this.m_Title = CreateText("Title", transform, new Vector3(0f, 0.23f, -0.025f), 0.04f, 32);
            this.m_Detail = CreateText("Detail", transform, new Vector3(0f, -0.25f, -0.025f), 0.028f, 32);

            Transform barBackground = CreateQuad(
                "BarBackground",
                transform,
                new Vector3(0f, -0.04f, 0f),
                new Vector3(1.82f, 0.14f, 1f),
                new Color(0.12f, 0.1f, 0.09f, 1f),
                31).transform;
            GameObject fill = CreateQuad(
                "BarFill",
                barBackground,
                new Vector3(0f, 0f, -0.02f),
                new Vector3(0.96f, 0.62f, 1f),
                Color.white,
                32);
            this.m_Fill = fill.transform;
            this.m_FillRenderer = fill.GetComponent<MeshRenderer>();
            this.m_Renderers = GetComponentsInChildren<Renderer>(true);
        }

        private static GameObject CreateQuad(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Color color,
            int sortingOrder)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = objectName;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPosition;
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = localScale;
            Collider collider = quad.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.material = CreateColorMaterial(color);
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return quad;
        }

        private static TextMesh CreateText(
            string objectName,
            Transform parent,
            Vector3 localPosition,
            float characterSize,
            int sortingOrder)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            textObject.transform.localRotation = Quaternion.identity;

            TextMesh text = textObject.AddComponent<TextMesh>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 64;
            text.characterSize = characterSize;
            text.fontStyle = FontStyle.Bold;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = Color.white;

            MeshRenderer renderer = text.GetComponent<MeshRenderer>();
            renderer.material = new Material(text.font.material);
            if (renderer.material.HasProperty("_Cull"))
                renderer.material.SetFloat("_Cull", 0f);
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return text;
        }

        private static Material CreateColorMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            return material;
        }

        private void LateUpdate()
        {
            if (this.m_Node == null) return;

            transform.position = this.m_Node.IndicatorWorldPosition;
            if (Time.unscaledTime >= this.m_NextLookupTime)
            {
                RefreshReferences();
                this.m_NextLookupTime = Time.unscaledTime + 0.5f;
            }

            UpdateVisuals();
            if (this.m_BillboardCamera != null)
            {
                Vector3 cameraToIndicator = transform.position - this.m_BillboardCamera.transform.position;
                if (cameraToIndicator.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(
                        cameraToIndicator.normalized,
                        this.m_BillboardCamera.transform.up);
                }
            }
        }

        private void RefreshReferences()
        {
            this.m_Players = Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            this.m_Cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        }

        private void UpdateVisuals()
        {
            PlayerInputProvider targetPlayer = FindTargetPlayer();
            bool visible = targetPlayer != null;
            SetRenderersVisible(visible);
            if (!visible)
            {
                this.m_BillboardCamera = null;
                return;
            }

            this.m_BillboardCamera = FindCameraForPlayer(targetPlayer);
            ConfigureCameraVisibility(targetPlayer);
            Color resourceColor = GetResourceColor(this.m_Node.resourceType);
            this.m_Title.color = resourceColor;
            SetRendererColor(this.m_FillRenderer, resourceColor);

            float progress;
            if (this.m_Node.State == ResourceNode.NodeState.Available)
            {
                this.m_Title.text = this.m_Node.resourceType.ToString().ToUpperInvariant();
                string action = targetPlayer.Slot == PlayerSlot.One ? "SPACE" : "ENTER";
                this.m_Detail.text =
                    $"{action} HARVEST  {this.m_Node.CurrentHealth}/{this.m_Node.maxHealth}";
                progress = this.m_Node.HealthNormalized;
            }
            else
            {
                this.m_Title.text = "RESPAWNING";
                this.m_Detail.text = $"{Mathf.CeilToInt(this.m_Node.RespawnRemaining)}s";
                progress = this.m_Node.RespawnNormalized;
            }

            progress = Mathf.Clamp01(progress);
            this.m_Fill.localScale = new Vector3(progress * 0.96f, 0.62f, 1f);
            this.m_Fill.localPosition = new Vector3((progress - 1f) * 0.48f, 0f, -0.02f);
        }

        private PlayerInputProvider FindTargetPlayer()
        {
            if (this.m_Players == null || this.m_Players.Length == 0) return null;

            PlayerInputProvider target = null;
            float targetDistance = float.MaxValue;
            foreach (PlayerInputProvider player in this.m_Players)
            {
                if (player == null) continue;
                float distance = DistanceToNode(player, this.m_Node);
                if (distance > HarvestRange || distance >= targetDistance) continue;
                if (!IsClosestNodeForPlayer(player, distance)) continue;
                target = player;
                targetDistance = distance;
            }
            return target;
        }

        private bool IsClosestNodeForPlayer(PlayerInputProvider player, float thisDistance)
        {
            foreach (ResourceNode candidate in
                     Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                if (candidate == null || candidate == this.m_Node) continue;
                if (DistanceToNode(player, candidate) + 0.05f < thisDistance)
                    return false;
            }
            return true;
        }

        private static float DistanceToNode(PlayerInputProvider player, ResourceNode node)
        {
            Collider collider = node.GetComponent<Collider>();
            Vector3 playerPosition = player.transform.position;
            Vector3 closestPoint = collider != null
                ? collider.ClosestPoint(playerPosition)
                : node.transform.position;
            return Vector3.Distance(playerPosition, closestPoint);
        }

        private Camera FindCameraForPlayer(PlayerInputProvider player)
        {
            if (this.m_Cameras == null) return null;

            string expectedName = player.Slot == PlayerSlot.One
                ? "Player 1 Camera"
                : "Player 2 Camera";
            Camera shared = null;
            foreach (Camera camera in this.m_Cameras)
            {
                if (camera == null || !camera.enabled) continue;
                if (camera.name == expectedName) return camera;
                if (camera.rect.width > 0.9f) shared = camera;
            }
            return shared;
        }

        private void ConfigureCameraVisibility(PlayerInputProvider player)
        {
            int ownLayer = player.Slot == PlayerSlot.One
                ? PlayerOneIndicatorLayer
                : PlayerTwoIndicatorLayer;
            SetLayerRecursively(transform, ownLayer);

            if (this.m_Cameras == null) return;
            int playerOneMask = 1 << PlayerOneIndicatorLayer;
            int playerTwoMask = 1 << PlayerTwoIndicatorLayer;
            foreach (Camera camera in this.m_Cameras)
            {
                if (camera == null) continue;
                if (camera.rect.width > 0.9f)
                {
                    camera.cullingMask |= playerOneMask | playerTwoMask;
                    continue;
                }

                if (camera.name == "Player 1 Camera")
                    camera.cullingMask = (camera.cullingMask | playerOneMask) & ~playerTwoMask;
                else if (camera.name == "Player 2 Camera")
                    camera.cullingMask = (camera.cullingMask | playerTwoMask) & ~playerOneMask;
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            foreach (Transform child in root)
                SetLayerRecursively(child, layer);
        }

        private static void SetRendererColor(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            renderer.material.color = color;
            if (renderer.material.HasProperty("_BaseColor"))
                renderer.material.SetColor("_BaseColor", color);
        }

        private void SetRenderersVisible(bool visible)
        {
            if (this.m_Renderers == null)
                this.m_Renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in this.m_Renderers)
            {
                if (renderer != null) renderer.enabled = visible;
            }
        }

        private static Color GetResourceColor(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Wood:
                    return new Color(0.94f, 0.56f, 0.25f, 1f);
                case ResourceType.Stone:
                    return new Color(0.78f, 0.82f, 0.86f, 1f);
                case ResourceType.Metal:
                    return new Color(0.38f, 0.72f, 1f, 1f);
                default:
                    return Color.white;
            }
        }
    }
}
