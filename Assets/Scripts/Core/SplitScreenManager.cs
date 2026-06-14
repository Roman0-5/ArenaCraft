using UnityEngine;
using UnityEngine.InputSystem;

namespace ArenaCraft
{
    [RequireComponent(typeof(Camera))]
    public class SplitScreenManager : MonoBehaviour
    {
        public const string PreferenceKey = "ArenaCraft.SplitScreen";

        [SerializeField] private bool m_DefaultToSplitScreen;
        [SerializeField] private Vector3 m_PlayerViewOffset = new Vector3(0f, 14f, -11f);
        [SerializeField] private float m_FollowSmoothTime = 0.18f;
        [SerializeField] private float m_FieldOfView = 48f;

        private Camera m_SharedCamera;
        private ArenaCameraController m_SharedController;
        private Camera m_PlayerOneCamera;
        private Camera m_PlayerTwoCamera;
        private PlayerCameraFollow m_PlayerOneFollow;
        private PlayerCameraFollow m_PlayerTwoFollow;

        public bool IsSplitScreen { get; private set; }
        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            this.m_SharedCamera = GetComponent<Camera>();
            this.m_SharedController = GetComponent<ArenaCameraController>();
            this.CreatePlayerCameras();
            bool enabled = PlayerPrefs.GetInt(PreferenceKey, this.m_DefaultToSplitScreen ? 1 : 0) == 1;
            this.SetSplitScreen(enabled);
            this.IsInitialized = true;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
                this.SetSplitScreen(!this.IsSplitScreen);
        }

        public void SetSplitScreen(bool enabled)
        {
            this.IsSplitScreen = enabled;
            PlayerPrefs.SetInt(PreferenceKey, enabled ? 1 : 0);

            if (this.m_SharedController != null) this.m_SharedController.enabled = !enabled;
            if (this.m_SharedCamera != null) this.m_SharedCamera.enabled = !enabled;
            if (this.m_PlayerOneCamera != null) this.m_PlayerOneCamera.enabled = enabled;
            if (this.m_PlayerTwoCamera != null) this.m_PlayerTwoCamera.enabled = enabled;

            Debug.Log($"Camera mode: {(enabled ? "Split Screen" : "Shared Screen")} (F10 to toggle)");
        }

        private void CreatePlayerCameras()
        {
            this.m_PlayerOneCamera = this.CreateCamera("Player 1 Camera", new Rect(0f, 0f, 0.5f, 1f), out this.m_PlayerOneFollow);
            this.m_PlayerTwoCamera = this.CreateCamera("Player 2 Camera", new Rect(0.5f, 0f, 0.5f, 1f), out this.m_PlayerTwoFollow);
            this.AssignPlayers();
        }

        private Camera CreateCamera(string cameraName, Rect viewport, out PlayerCameraFollow follow)
        {
            GameObject cameraObject = new GameObject(cameraName);
            cameraObject.transform.SetParent(this.transform.parent);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(this.m_SharedCamera);
            camera.rect = viewport;
            camera.fieldOfView = this.m_FieldOfView;
            camera.depth = this.m_SharedCamera.depth;
            camera.enabled = false;

            follow = cameraObject.AddComponent<PlayerCameraFollow>();
            follow.Configure(this.m_PlayerViewOffset, this.m_FollowSmoothTime);
            return camera;
        }

        private void AssignPlayers()
        {
            foreach (PlayerInputProvider provider in Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None))
            {
                if (provider.Slot == PlayerSlot.One) this.m_PlayerOneFollow.SetTarget(provider.transform);
                else this.m_PlayerTwoFollow.SetTarget(provider.transform);
            }
        }
    }

    public class PlayerCameraFollow : MonoBehaviour
    {
        private Transform m_Target;
        private Vector3 m_Offset;
        private float m_SmoothTime;
        private Vector3 m_Velocity;

        public void Configure(Vector3 offset, float smoothTime)
        {
            this.m_Offset = offset;
            this.m_SmoothTime = smoothTime;
        }

        public void SetTarget(Transform target)
        {
            this.m_Target = target;
            if (target == null) return;
            this.transform.position = target.position + this.m_Offset;
            this.transform.LookAt(target.position + Vector3.up);
        }

        private void LateUpdate()
        {
            if (this.m_Target == null) return;

            Vector3 destination = this.m_Target.position + this.m_Offset;
            this.transform.position = Vector3.SmoothDamp(
                this.transform.position,
                destination,
                ref this.m_Velocity,
                this.m_SmoothTime);
            this.transform.rotation = Quaternion.LookRotation(
                this.m_Target.position + Vector3.up - this.transform.position,
                Vector3.up);
        }
    }
}
