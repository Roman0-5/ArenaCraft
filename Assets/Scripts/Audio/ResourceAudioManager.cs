using UnityEngine;
using ArenaCraft;

namespace ArenaCraft
{
    [RequireComponent(typeof(AudioSource))]
    public class ResourceAudioManager : MonoBehaviour
    {
        [Header("Wood")]
        public AudioClip[] woodClips;
        [Range(0f, 1f)] public float woodVolume = 0.8f;

        [Header("Stone")]
        public AudioClip[] stoneClips;
        [Range(0f, 1f)] public float stoneVolume = 0.8f;

        [Header("Metal")]
        public AudioClip[] metalClips;
        [Range(0f, 1f)] public float metalVolume = 0.8f;

        private AudioSource m_Source;

        private void Awake()
        {
            this.m_Source = GetComponent<AudioSource>();
            this.m_Source.spatialBlend = 0f;
            this.m_Source.playOnAwake = false;
        }

        private void OnEnable()  => ResourceNode.OnAnyHarvested += HandleHarvested;
        private void OnDisable() => ResourceNode.OnAnyHarvested -= HandleHarvested;

        private void HandleHarvested(ResourceNode node, PlayerInventory harvester, int amount)
        {
            AudioClip[] clips;
            float volume;

            switch (node.resourceType)
            {
                case ResourceType.Wood:
                    clips = this.woodClips;
                    volume = this.woodVolume;
                    break;
                case ResourceType.Stone:
                    clips = this.stoneClips;
                    volume = this.stoneVolume;
                    break;
                case ResourceType.Metal:
                    clips = this.metalClips;
                    volume = this.metalVolume;
                    break;
                default:
                    return;
            }

            if (clips == null || clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip != null) this.m_Source.PlayOneShot(clip, volume);
        }
    }
}
