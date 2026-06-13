using System.Collections.Generic;
using UnityEngine;

namespace ArenaCraft
{
    /// <summary>Keeps both local players readable while preserving an elevated arena view.</summary>
    [RequireComponent(typeof(Camera))]
    public class ArenaCameraController : MonoBehaviour
    {
        [SerializeField] private Vector3 m_ViewOffset = new Vector3(0f, 18f, -17f);
        [SerializeField] private float m_MinDistanceScale = 1f;
        [SerializeField] private float m_MaxDistanceScale = 1.45f;
        [SerializeField] private float m_SmoothTime = 0.45f;
        [SerializeField] private float m_OcclusionRadius = 2.5f;
        [SerializeField] private float m_FieldOfView = 50f;

        private Transform m_PlayerOne;
        private Transform m_PlayerTwo;
        private Vector3 m_Velocity;
        private Renderer[] m_SceneRenderers;
        private readonly List<Renderer> m_HiddenRenderers = new List<Renderer>();

        private void Start()
        {
            this.FindPlayers();
            this.m_SceneRenderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            GetComponent<Camera>().fieldOfView = this.m_FieldOfView;
        }

        private void FindPlayers()
        {
            foreach (var provider in Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None))
            {
                if (provider.Slot == PlayerSlot.One) this.m_PlayerOne = provider.transform;
                else this.m_PlayerTwo = provider.transform;
            }
        }

        private void LateUpdate()
        {
            if (this.m_PlayerOne == null || this.m_PlayerTwo == null)
            {
                this.FindPlayers();
                if (this.m_PlayerOne == null || this.m_PlayerTwo == null) return;
            }

            Vector3 center = (this.m_PlayerOne.position + this.m_PlayerTwo.position) * 0.5f;
            center.y = 0f;
            float separation = Vector3.Distance(this.m_PlayerOne.position, this.m_PlayerTwo.position);
            float scale = Mathf.Lerp(this.m_MinDistanceScale, this.m_MaxDistanceScale, Mathf.InverseLerp(5f, 28f, separation));
            Vector3 destination = center + this.m_ViewOffset * scale;

            this.transform.position = Vector3.SmoothDamp(
                this.transform.position,
                destination,
                ref this.m_Velocity,
                this.m_SmoothTime);
            this.transform.rotation = Quaternion.Slerp(
                this.transform.rotation,
                Quaternion.LookRotation(center - this.transform.position, Vector3.up),
                6f * Time.deltaTime);

            this.UpdateOccluders(center + Vector3.up);
        }

        private void UpdateOccluders(Vector3 target)
        {
            this.RestoreOccluders();
            if (this.m_SceneRenderers == null) return;

            Vector3 cameraPosition = this.transform.position;
            Vector3 cameraToTarget = target - cameraPosition;
            float segmentLengthSquared = cameraToTarget.sqrMagnitude;
            if (segmentLengthSquared <= Mathf.Epsilon) return;

            foreach (Renderer sceneRenderer in this.m_SceneRenderers)
            {
                if (sceneRenderer == null || !sceneRenderer.enabled || !sceneRenderer.gameObject.activeInHierarchy) continue;
                if (sceneRenderer.transform.IsChildOf(this.m_PlayerOne) ||
                    sceneRenderer.transform.IsChildOf(this.m_PlayerTwo)) continue;

                Bounds bounds = sceneRenderer.bounds;
                float alongSegment = Vector3.Dot(bounds.center - cameraPosition, cameraToTarget) / segmentLengthSquared;
                if (alongSegment <= 0f || alongSegment >= 1f) continue;

                Vector3 closestPoint = cameraPosition + cameraToTarget * alongSegment;
                if (bounds.SqrDistance(closestPoint) > this.m_OcclusionRadius * this.m_OcclusionRadius) continue;

                sceneRenderer.forceRenderingOff = true;
                this.m_HiddenRenderers.Add(sceneRenderer);
            }
        }

        private void RestoreOccluders()
        {
            foreach (Renderer hiddenRenderer in this.m_HiddenRenderers)
            {
                if (hiddenRenderer != null) hiddenRenderer.forceRenderingOff = false;
            }

            this.m_HiddenRenderers.Clear();
        }

        private void OnDisable()
        {
            this.RestoreOccluders();
        }
    }
}
