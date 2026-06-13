using UnityEngine;

namespace ArenaCraft
{
    /// <summary>Small camera shake used for hits and blocks. It attaches itself on first use.</summary>
    [DefaultExecutionOrder(200)]
    public class ArenaCameraImpact : MonoBehaviour
    {
        private static ArenaCameraImpact s_Instance;

        private float m_Strength;
        private float m_Duration;
        private float m_TimeRemaining;

        private void Awake()
        {
            s_Instance = this;
        }

        public static void Shake(float strength, float duration)
        {
            if (s_Instance == null)
            {
                Camera camera = Object.FindAnyObjectByType<Camera>();
                if (camera == null) return;
                s_Instance = camera.GetComponent<ArenaCameraImpact>();
                if (s_Instance == null) s_Instance = camera.gameObject.AddComponent<ArenaCameraImpact>();
            }

            s_Instance.Play(strength, duration);
        }

        private void Play(float strength, float duration)
        {
            this.m_Strength = Mathf.Max(this.m_Strength, strength);
            this.m_Duration = Mathf.Max(this.m_Duration, duration);
            this.m_TimeRemaining = Mathf.Max(this.m_TimeRemaining, duration);
        }

        private void LateUpdate()
        {
            if (this.m_TimeRemaining <= 0f) return;
            float fade = this.m_Duration > 0f ? this.m_TimeRemaining / this.m_Duration : 0f;
            Vector2 offset = Random.insideUnitCircle * this.m_Strength * fade;
            this.transform.position += this.transform.right * offset.x + this.transform.up * offset.y;
            this.m_TimeRemaining -= Time.unscaledDeltaTime;
            if (this.m_TimeRemaining <= 0f)
            {
                this.m_Strength = 0f;
                this.m_Duration = 0f;
            }
        }
    }
}
