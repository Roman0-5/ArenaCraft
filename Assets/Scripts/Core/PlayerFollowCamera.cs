using UnityEngine;

namespace ArenaCraft
{
    public class PlayerCameraFollow : MonoBehaviour
    {
        private const float CollisionRadius = 0.4f;
        private const float CollisionPadding = 0.3f;

        private Transform m_Target;
        private Vector3 m_Offset;
        private float m_SmoothTime;
        private Vector3 m_Velocity;
        private LayerMask m_CollisionMask;

        public void Configure(Vector3 offset, float smoothTime)
        {
            this.m_Offset = offset;
            this.m_SmoothTime = smoothTime;
            this.m_CollisionMask = ~0;
        }

        public void SetTarget(Transform target)
        {
            this.m_Target = target;
            if (target == null) return;
            this.transform.position = this.GetCollisionSafePosition(this.GetDesiredPosition());
            this.transform.LookAt(this.GetLookTarget(), Vector3.up);
        }

        private void LateUpdate()
        {
            if (this.m_Target == null) return;

            Vector3 desired = this.GetCollisionSafePosition(this.GetDesiredPosition());
            this.transform.position = Vector3.SmoothDamp(
                this.transform.position,
                desired,
                ref this.m_Velocity,
                this.m_SmoothTime);
            this.transform.rotation = Quaternion.LookRotation(
                this.GetLookTarget() - this.transform.position,
                Vector3.up);
        }

        private Vector3 GetDesiredPosition()
        {
            return this.m_Target.position + this.m_Offset;
        }

        private Vector3 GetLookTarget()
        {
            return this.m_Target.position + Vector3.up * 1.0f;
        }

        private static readonly RaycastHit[] s_HitBuffer = new RaycastHit[16];

        private Vector3 GetCollisionSafePosition(Vector3 desired)
        {
            Vector3 from = this.GetLookTarget();
            Vector3 direction = desired - from;
            float distance = direction.magnitude;
            if (distance < Mathf.Epsilon) return desired;

            Vector3 normalized = direction / distance;
            int count = Physics.SphereCastNonAlloc(
                from,
                CollisionRadius,
                normalized,
                s_HitBuffer,
                distance,
                this.m_CollisionMask,
                QueryTriggerInteraction.Ignore);

            float closest = distance;
            bool blocked = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = s_HitBuffer[i];
                if (hit.collider == null) continue;
                if (hit.collider.GetComponentInParent<PlayerInputProvider>() != null) continue;
                if (hit.distance < closest)
                {
                    closest = hit.distance;
                    blocked = true;
                }
            }

            if (blocked)
                return from + normalized * Mathf.Max(0.1f, closest - CollisionPadding);

            return desired;
        }
    }
}
