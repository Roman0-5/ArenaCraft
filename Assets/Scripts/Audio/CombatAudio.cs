using UnityEngine;

namespace ArenaCraft
{
    [RequireComponent(typeof(AudioSource))]
    public class CombatAudio : MonoBehaviour
    {
        [Header("Attack")]
        public AudioClip swingClip;
        [Range(0f, 1f)] public float swingVolume = 0.7f;

        public AudioClip hitClip;
        [Range(0f, 2f)] public float hitVolume = 1.5f;

        [Header("Movement")]
        public AudioClip dashClip;
        [Range(0f, 1f)] public float dashVolume = 0.8f;

        [Header("Shield")]
        public AudioClip blockClip;
        [Range(0f, 1f)] public float blockVolume = 0.4f;

        public AudioClip shieldBreakClip;
        [Range(0f, 1f)] public float shieldBreakVolume = 1f;

        [Header("Death")]
        public AudioClip deathClip;
        [Range(0f, 1f)] public float deathVolume = 1f;

        private AudioSource source;

        private void Awake()
        {
            this.source = GetComponent<AudioSource>();
        }

        public void PlaySwing()       => Play(this.swingClip,       this.swingVolume);
        public void PlayHit()         => Play(this.hitClip,         this.hitVolume);
        public void PlayDash()        => Play(this.dashClip,        this.dashVolume);
        public void PlayBlock()       => Play(this.blockClip,       this.blockVolume);
        public void PlayShieldBreak() => Play(this.shieldBreakClip, this.shieldBreakVolume);
        public void PlayDeath()       => Play(this.deathClip,       this.deathVolume);

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null) this.source.PlayOneShot(clip, volume);
        }
    }
}
