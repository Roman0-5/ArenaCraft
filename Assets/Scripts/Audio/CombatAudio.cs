using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Plays combat SFX (swing, hit) via an AudioSource. Triggered by <see cref="MeleeAttack"/>
    /// and <see cref="AttackHitbox"/> (GDD FSR3).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CombatAudio : MonoBehaviour
    {
        #region Public Fields
        [Tooltip("Played when a swing starts.")]
        public AudioClip swingClip;

        [Tooltip("Played when a swing connects with an opponent.")]
        public AudioClip hitClip;

        [Range(0f, 1f)]
        public float volume = 1f;
        #endregion

        #region Private Fields
        private AudioSource source;
        #endregion

        private void Awake()
        {
            this.source = GetComponent<AudioSource>();
        }

        public void PlaySwing()
        {
            if (this.swingClip != null) this.source.PlayOneShot(this.swingClip, this.volume);
        }

        public void PlayHit()
        {
            if (this.hitClip != null) this.source.PlayOneShot(this.hitClip, this.volume);
        }
    }
}
