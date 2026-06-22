using System.Collections;
using UnityEngine;

namespace ArenaCraft
{
    public class AmbienceController : MonoBehaviour
    {
        [Header("Clips")]
        public AudioClip resourceClip;
        public AudioClip shopClip;
        public AudioClip battleClip;

        [Header("Settings")]
        [Range(0f, 1f)] public float volume = 0.35f;
        public float crossfadeDuration = 1.5f;

        private AudioSource m_SourceA;
        private AudioSource m_SourceB;
        private AudioSource m_Active;
        private Coroutine m_Fade;

        private void Awake()
        {
            this.m_SourceA = CreateSource();
            this.m_SourceB = CreateSource();
            this.m_Active = this.m_SourceA;
        }

        private void OnEnable()
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
            AudioClip clip = phase switch
            {
                GamePhase.Resource    => this.resourceClip,
                GamePhase.Shopping    => this.shopClip,
                GamePhase.BattleRoyale => this.battleClip,
                _ => null
            };

            CrossfadeTo(clip);
        }

        private void CrossfadeTo(AudioClip clip)
        {
            if (this.m_Fade != null) StopCoroutine(this.m_Fade);
            this.m_Fade = StartCoroutine(FadeRoutine(clip));
        }

        private IEnumerator FadeRoutine(AudioClip clip)
        {
            AudioSource outgoing = this.m_Active;
            AudioSource incoming = outgoing == this.m_SourceA ? this.m_SourceB : this.m_SourceA;
            this.m_Active = incoming;

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
                outgoing.volume = Mathf.Lerp(this.volume, 0f, t);
                if (clip != null) incoming.volume = Mathf.Lerp(0f, this.volume, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            outgoing.Stop();
            outgoing.volume = 0f;
            if (clip != null) incoming.volume = this.volume;

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
