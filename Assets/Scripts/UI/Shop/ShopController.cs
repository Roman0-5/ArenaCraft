using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace ArenaCraft
{
    public class ShopController : MonoBehaviour
    {
        public static ShopController Instance { get; private set; }

        [Header("Assets")]
        public Weapon basicSword;
        public Weapon advancedSword;
        public AudioClip buySound;

        private UIDocument m_UIDocument;
        private VisualElement m_Root;
        private Label m_WarningLabel;
        private AudioSource m_AudioSource;
        
        private PlayerInventory m_ActiveInventory;
        private Health m_ActiveHealth;
        private MeleeAttack m_ActiveMelee;
        private bool m_BoughtAnything;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            this.m_UIDocument = GetComponent<UIDocument>();
            this.m_UIDocument.sortingOrder = 50;
            this.m_Root = this.m_UIDocument.rootVisualElement.Q<VisualElement>("shop-root");
            if (this.m_Root == null)
            {
                Debug.LogError("Shop UI is missing the 'shop-root' element.", this);
                return;
            }
            this.m_WarningLabel = this.m_Root.Q<Label>("warning-label");

            this.m_AudioSource = gameObject.AddComponent<AudioSource>();
            this.m_AudioSource.playOnAwake = false;

            BindButtons();
            this.m_Root.style.display = DisplayStyle.None;
        }

        public void OpenShop(PlayerInventory inventory, Health health, MeleeAttack melee)
        {
            if (this.m_Root == null) return;

            this.m_ActiveInventory = inventory;
            this.m_ActiveHealth = health;
            this.m_ActiveMelee = melee;
            this.m_BoughtAnything = false;

            if (this.m_WarningLabel != null)
                this.m_WarningLabel.style.visibility = Visibility.Hidden;

            this.m_Root.style.display = DisplayStyle.Flex;
            this.m_Root.BringToFront();
        }

        private void BindButtons()
        {
            var basicSwordButton = this.m_Root.Q<Button>("buy-basic-sword");
            if (basicSwordButton != null) basicSwordButton.clicked += BuyBasicSword;

            var advancedSwordButton = this.m_Root.Q<Button>("buy-advanced-sword");
            if (advancedSwordButton != null) advancedSwordButton.clicked += BuyAdvancedSword;

            var lightArmorButton = this.m_Root.Q<Button>("buy-light-armor");
            if (lightArmorButton != null) lightArmorButton.clicked += BuyLightArmor;

            var heavyArmorButton = this.m_Root.Q<Button>("buy-heavy-armor");
            if (heavyArmorButton != null) heavyArmorButton.clicked += BuyHeavyArmor;

            var closeButton = this.m_Root.Q<Button>("close-button");
            if (closeButton != null) closeButton.clicked += CloseShop;
        }

        private void BuyBasicSword() => BuyWeapon(this.basicSword, 50);
        private void BuyAdvancedSword() => BuyWeapon(this.advancedSword, 100);
        private void BuyLightArmor() => BuyArmor(ArmorType.Light, 50);
        private void BuyHeavyArmor() => BuyArmor(ArmorType.Heavy, 100);

        private void BuyWeapon(Weapon weapon, int price)
        {
            if (weapon != null && this.m_ActiveInventory != null && this.m_ActiveMelee != null && this.m_ActiveInventory.SpendGold(price))
            {
                this.m_ActiveMelee.EquipWeapon(weapon);
                this.m_BoughtAnything = true;
                if (this.buySound != null) this.m_AudioSource.PlayOneShot(this.buySound);
                Debug.Log($"Bought {weapon.displayName}");
            }
        }

        private void BuyArmor(ArmorType armor, int price)
        {
            if (this.m_ActiveInventory != null && this.m_ActiveHealth != null && this.m_ActiveInventory.SpendGold(price))
            {
                this.m_ActiveHealth.ApplyArmor(armor);
                this.m_BoughtAnything = true;
                if (this.buySound != null) this.m_AudioSource.PlayOneShot(this.buySound);
                Debug.Log($"Bought {armor}");
            }
        }

        private void CloseShop()
        {
            if (!this.m_BoughtAnything)
            {
                if (this.m_WarningLabel == null) return;
                this.m_WarningLabel.style.visibility = Visibility.Visible;
                StopAllCoroutines(); // Stop any previous shake
                StartCoroutine(ShakeWarningLabel());
                return;
            }

            this.m_Root.style.display = DisplayStyle.None;
        }

        private IEnumerator ShakeWarningLabel()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            float magnitude = 10f;
            Vector3 originalPos = this.m_WarningLabel.transform.position;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                this.m_WarningLabel.transform.position = originalPos + new Vector3(x, 0, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            this.m_WarningLabel.transform.position = originalPos;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
