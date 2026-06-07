using UnityEngine;
using System.Collections;

namespace ArenaCraft
{
    public class ResourceNode : MonoBehaviour
    {
        public ResourceType resourceType;
        public int maxHealth = 3;
        public int resourcesPerHit = 10;
        public float respawnTime = 10f;
        public GameObject visuals;
        public ParticleSystem harvestEffect;
        public AudioClip hitSound;

        private int m_CurrentHealth;
        private bool m_IsDestroyed;
        private AudioSource m_AudioSource;

        private void Awake()
        {
            this.m_CurrentHealth = this.maxHealth;
            this.m_AudioSource = gameObject.AddComponent<AudioSource>();
            this.m_AudioSource.playOnAwake = false;
            this.m_AudioSource.spatialBlend = 1.0f;
        }

        public void TakeDamage(int damage, PlayerInventory harvester)
        {
            if (this.m_IsDestroyed) return;

            this.m_CurrentHealth -= damage;
            
            if (harvester != null)
            {
                harvester.AddResource(this.resourceType, this.resourcesPerHit);
            }

            if (this.hitSound != null)
            {
                this.m_AudioSource.PlayOneShot(this.hitSound);
            }

            if (this.harvestEffect != null)
            {
                this.harvestEffect.Play();
            }

            StartCoroutine(HitShakeRoutine());
            StartCoroutine(HitScaleRoutine());

            if (this.m_CurrentHealth <= 0)
            {
                DestroyNode();
            }
        }

        private IEnumerator HitShakeRoutine()
        {
            if (this.visuals == null) yield break;
            
            Vector3 originalPos = this.visuals.transform.localPosition;
            float elapsed = 0f;
            float duration = 0.15f;
            float magnitude = 0.15f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                float z = UnityEngine.Random.Range(-1f, 1f) * magnitude;

                this.visuals.transform.localPosition = originalPos + new Vector3(x, 0, z);
                elapsed += Time.deltaTime;
                yield return null;
            }

            this.visuals.transform.localPosition = originalPos;
        }

        private IEnumerator HitScaleRoutine()
        {
            if (this.visuals == null) yield break;

            Vector3 originalScale = this.visuals.transform.localScale;
            Vector3 targetScale = originalScale * 1.15f;
            float elapsed = 0f;
            float duration = 0.1f;

            while (elapsed < duration)
            {
                this.visuals.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                this.visuals.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            this.visuals.transform.localScale = originalScale;
        }

        private void DestroyNode()
{
            this.m_IsDestroyed = true;
            if (this.visuals != null) this.visuals.SetActive(false);
            GetComponent<Collider>().enabled = false;
            
            if (this.respawnTime > 0)
            {
                StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(this.respawnTime);
            this.m_CurrentHealth = this.maxHealth;
            this.m_IsDestroyed = false;
            if (this.visuals != null) this.visuals.SetActive(true);
            GetComponent<Collider>().enabled = true;
        }
}
}
