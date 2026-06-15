using UnityEngine;

namespace ArenaCraft
{
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
            this.transform.position = this.GetDesiredPosition();
            this.transform.LookAt(this.GetLookTarget(), Vector3.up);
        }

        private void LateUpdate()
        {
            if (this.m_Target == null) return;

            this.transform.position = Vector3.SmoothDamp(
                this.transform.position,
                this.GetDesiredPosition(),
                ref this.m_Velocity,
                this.m_SmoothTime);
            this.transform.rotation = Quaternion.LookRotation(
                this.GetLookTarget() - this.transform.position,
                Vector3.up);
        }

        private Vector3 GetDesiredPosition()
        {
            Vector3 targetPosition = this.m_Target.position;
            targetPosition.y = 0f;
            return targetPosition + this.m_Offset;
        }

        private Vector3 GetLookTarget()
        {
            Vector3 targetPosition = this.m_Target.position;
            targetPosition.y = 0f;
            return targetPosition;
        }
    }
}
