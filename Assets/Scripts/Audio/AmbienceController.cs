using System.Collections;
using UnityEngine;

namespace ArenaCraft
{
    public class AmbienceController : MonoBehaviour
    {
        [Header("Resource Phase")]
        public AudioClip resourceClip;
        [Range(0f, 1f)] public float resourceVolume = 0.35f;

        [Header("Shopping Phase")]
        public AudioClip shopClip;
        [Range(0f, 1f)] public float shopVolume = 0.35f;

        [Header("Battle Phase")]
        public AudioClip battleClip;
        [Range(0f, 1f)] public float battleVolume = 0.35f;

        [Header("Settings")]
        public float crossfadeDuration = 1.5f;

        private AudioSource m_SourceA;
        private AudioSource m_SourceB;
        private AudioSource m_Active;
        private float m_TargetVolume;
        private Coroutine m_Fade;

        private void Awake()
        {
            this.m_SourceA = CreateSource();
            this.m_SourceB = CreateSource();
            this.m_Active = this.m_SourceA;
        }

        private void Start()
        {
            if (GamePhaseManager.Instance != null)
                GamePhaseManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            if (GamePhaseManager.Instance != null)
                GamePhaseManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            (AudioClip clip, float vol) = phase switch
            {
                GamePhase.Resource     => (this.resourceClip, this.resourceVolume),
                GamePhase.Shopping     => (this.shopClip,     this.shopVolume),
                GamePhase.BattleRoyale => (this.battleClip,   this.battleVolume),
                _                      => (null, 0f)
            };

            CrossfadeTo(clip, vol);
        }

        public void FadeOut() => CrossfadeTo(null, 0f);

        private void CrossfadeTo(AudioClip clip, float targetVolume)
        {
            this.m_TargetVolume = targetVolume;
            if (this.m_Fade != null) StopCoroutine(this.m_Fade);
            this.m_Fade = StartCoroutine(FadeRoutine(clip, targetVolume));
        }

        private IEnumerator FadeRoutine(AudioClip clip, float targetVolume)
        {
            AudioSource outgoing = this.m_Active;
            AudioSource incoming = outgoing == this.m_SourceA ? this.m_SourceB : this.m_SourceA;
            this.m_Active = incoming;

            float outgoingStart = outgoing.volume;

            if (clip != null)
            {
                incoming.clip = clip;
                incoming.volume = 0f;
                incoming.Play();
            }

            float elapsed = 0f;
            float duration = this.crossfadeDuration;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                outgoing.volume = Mathf.Lerp(outgoingStart, 0f, t);
                if (clip != null) incoming.volume = Mathf.Lerp(0f, targetVolume, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;
            if (clip != null) incoming.volume = targetVolume;

            this.m_Fade = null;
        }

        private AudioSource CreateSource()
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 0f;
            return src;
        }
    }
}
