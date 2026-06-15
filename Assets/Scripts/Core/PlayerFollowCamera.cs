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
            this.transform.position = target.position + this.m_Offset;
            this.transform.LookAt(target.position + Vector3.up, Vector3.up);
        }

        private void LateUpdate()
        {
            if (this.m_Target == null) return;

            // World-space Offset → Kamera bleibt immer von oben, dreht nicht mit
            Vector3 destination = this.m_Target.position + this.m_Offset;

            this.transform.position = Vector3.SmoothDamp(
                this.transform.position,
                destination,
                ref this.m_Velocity,
                this.m_SmoothTime);

            this.transform.rotation = Quaternion.LookRotation(
                (this.m_Target.position + Vector3.up) - this.transform.position,
                Vector3.up);
        }
    }
}