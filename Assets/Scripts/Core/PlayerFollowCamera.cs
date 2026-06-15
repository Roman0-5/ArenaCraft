using UnityEngine;

public class PlayerCameraFollow : MonoBehaviour
{
    private Transform m_Target;
    private Vector3 m_Offset;
    private float m_SmoothTime;
    private Vector3 m_Velocity;
    private float m_CurrentYaw;
    private float m_YawVelocity;

    [SerializeField] private float m_YawSmoothTime = 0.3f;

    public void Configure(Vector3 offset, float smoothTime)
    {
        this.m_Offset = offset;
        this.m_SmoothTime = smoothTime;
    }

    public void SetTarget(Transform target)
    {
        this.m_Target = target;
        if (target == null) return;
        this.m_CurrentYaw = target.eulerAngles.y;
        this.transform.position = this.GetDesiredPosition();
        this.transform.LookAt(target.position + Vector3.up * 1.5f, Vector3.up);
    }

    private void LateUpdate()
    {
        if (this.m_Target == null) return;

        // Nur horizontale Rotation (Yaw) smooth folgen
        float targetYaw = this.m_Target.eulerAngles.y;
        this.m_CurrentYaw = Mathf.SmoothDampAngle(
            this.m_CurrentYaw,
            targetYaw,
            ref this.m_YawVelocity,
            this.m_YawSmoothTime);

        this.transform.position = Vector3.SmoothDamp(
            this.transform.position,
            this.GetDesiredPosition(),
            ref this.m_Velocity,
            this.m_SmoothTime);

        this.transform.rotation = Quaternion.LookRotation(
            (this.m_Target.position + Vector3.up * 1.5f) - this.transform.position,
            Vector3.up);
    }

    private Vector3 GetDesiredPosition()
    {
        Quaternion yawRotation = Quaternion.Euler(0f, this.m_CurrentYaw, 0f);
        return this.m_Target.position + yawRotation * this.m_Offset;
    }
}