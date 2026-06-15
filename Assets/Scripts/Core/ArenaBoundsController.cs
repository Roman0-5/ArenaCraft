using UnityEngine;

namespace ArenaCraft
{
    public sealed class ArenaBoundsController : MonoBehaviour
    {
        private const float PlayerRadius = 0.65f;
        private Bounds m_Bounds;
        private bool m_IsConfigured;

        public Bounds CurrentBounds => this.m_Bounds;

        public void Configure(Bounds bounds, string areaName)
        {
            this.m_Bounds = bounds;
            this.m_IsConfigured = true;
            ConstrainPlayers();
            Debug.Log($"[ArenaCraft] Active bounds: {areaName} at {bounds.center}, size {bounds.size}.");
        }

        private void FixedUpdate()
        {
            ConstrainPlayers();
        }

        public void ConstrainPlayers()
        {
            if (!this.m_IsConfigured) return;

            float minX = this.m_Bounds.min.x + PlayerRadius;
            float maxX = this.m_Bounds.max.x - PlayerRadius;
            float minZ = this.m_Bounds.min.z + PlayerRadius;
            float maxZ = this.m_Bounds.max.z - PlayerRadius;

            foreach (PlayerInputProvider player in
                     Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None))
            {
                Rigidbody body = player.GetComponent<Rigidbody>();
                Vector3 position = body != null ? body.position : player.transform.position;
                Vector3 constrained = new Vector3(
                    Mathf.Clamp(position.x, minX, maxX),
                    position.y,
                    Mathf.Clamp(position.z, minZ, maxZ));
                if ((constrained - position).sqrMagnitude < 0.0001f) continue;

                if (body != null)
                {
                    body.position = constrained;
                    body.linearVelocity = Vector3.zero;
                }
                else
                {
                    player.transform.position = constrained;
                }
            }
        }
    }
}
