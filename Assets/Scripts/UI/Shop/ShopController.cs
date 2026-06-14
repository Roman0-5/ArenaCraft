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
        private Label m_PlayerLabel;
        private Label m_GoldLabel;
        private AudioSource m_AudioSource;
        private Button m_BasicSwordButton;
        private Button m_AdvancedSwordButton;
        private Button m_LightArmorButton;
        private Button m_HeavyArmorButton;
        
        private PlayerInventory m_ActiveInventory;
        private Health m_ActiveHealth;
        private MeleeAttack m_ActiveMelee;
        private PlayerInventory m_PendingInventory;
        private Health m_PendingHealth;
        private MeleeAttack m_PendingMelee;
        private bool m_HasPurchased;
        private bool m_ConfirmedEmptyLoadout;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            this.m_UIDocument = GetComponent<UIDocument>();
            this.m_UIDocument.sortingOrder = 50;
            ResponsiveUILayout.Attach(this.m_UIDocument.rootVisualElement);
            this.m_Root = this.m_UIDocument.rootVisualElement.Q<VisualElement>("shop-root");
            if (this.m_Root == null)
            {
                Debug.LogError("Shop UI is missing the 'shop-root' element.", this);
                return;
            }
            this.m_WarningLabel = this.m_Root.Q<Label>("warning-label");
            this.m_PlayerLabel = this.m_Root.Q<Label>("shop-player-label");
            this.m_GoldLabel = this.m_Root.Q<Label>("shop-gold-label");

            this.m_AudioSource = gameObject.AddComponent<AudioSource>();
            this.m_AudioSource.playOnAwake = false;

            BindButtons();
            this.m_Root.style.display = DisplayStyle.None;
        }

        public void OpenShop(PlayerInventory inventory, Health health, MeleeAttack melee)
        {
            if (this.m_Root == null) return;
            if (inventory == this.m_ActiveInventory || inventory == this.m_PendingInventory) return;
            if (this.m_Root.style.display == DisplayStyle.Flex && inventory != this.m_ActiveInventory)
            {
                this.m_PendingInventory = inventory;
                this.m_PendingHealth = health;
                this.m_PendingMelee = melee;
                return;
            }

            this.m_ActiveInventory = inventory;
            this.m_ActiveHealth = health;
            this.m_ActiveMelee = melee;
            this.m_HasPurchased = false;
            this.m_ConfirmedEmptyLoadout = false;

            if (this.m_ActiveInventory != null)
                this.m_ActiveInventory.OnGoldChanged += this.HandleGoldChanged;

            var provider = inventory != null ? inventory.GetComponent<PlayerInputProvider>() : null;
            if (this.m_PlayerLabel != null)
                this.m_PlayerLabel.text = provider != null && provider.Slot == PlayerSlot.Two ? "PLAYER 2 LOADOUT" : "PLAYER 1 LOADOUT";

            this.m_Root.style.display = DisplayStyle.Flex;
            this.m_Root.BringToFront();
            SetHudVisible(false);
            this.SetStatus("Choose your equipment.", false);
            this.RefreshShopState();
        }

        private void BindButtons()
        {
            this.m_BasicSwordButton = this.m_Root.Q<Button>("buy-basic-sword");
            if (this.m_BasicSwordButton != null) this.m_BasicSwordButton.clicked += BuyBasicSword;

            this.m_AdvancedSwordButton = this.m_Root.Q<Button>("buy-advanced-sword");
            if (this.m_AdvancedSwordButton != null) this.m_AdvancedSwordButton.clicked += BuyAdvancedSword;

            this.m_LightArmorButton = this.m_Root.Q<Button>("buy-light-armor");
            if (this.m_LightArmorButton != null) this.m_LightArmorButton.clicked += BuyLightArmor;

            this.m_HeavyArmorButton = this.m_Root.Q<Button>("buy-heavy-armor");
            if (this.m_HeavyArmorButton != null) this.m_HeavyArmorButton.clicked += BuyHeavyArmor;

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
                this.m_HasPurchased = true;
                if (this.buySound != null) this.m_AudioSource.PlayOneShot(this.buySound);
                this.SetStatus($"{weapon.displayName.ToUpper()} EQUIPPED", false);
                this.RefreshShopState();
                Debug.Log($"Bought {weapon.displayName}");
            }
            else this.ShowPurchaseError(price);
        }

        private void BuyArmor(ArmorType armor, int price)
        {
            if (this.m_ActiveInventory != null && this.m_ActiveHealth != null && this.m_ActiveInventory.SpendGold(price))
            {
                this.m_ActiveHealth.ApplyArmor(armor);
                this.m_HasPurchased = true;
                if (this.buySound != null) this.m_AudioSource.PlayOneShot(this.buySound);
                this.SetStatus($"{armor.ToString().ToUpper()} ARMOR EQUIPPED", false);
                this.RefreshShopState();
                Debug.Log($"Bought {armor}");
            }
            else this.ShowPurchaseError(price);
        }

        private void CloseShop()
        {
            if (!this.m_HasPurchased && !this.m_ConfirmedEmptyLoadout)
            {
                this.m_ConfirmedEmptyLoadout = true;
                this.SetStatus("NO UPGRADE PURCHASED - CLOSE AGAIN TO CONFIRM", true);
                return;
            }

            if (this.m_ActiveInventory != null)
                this.m_ActiveInventory.OnGoldChanged -= this.HandleGoldChanged;
            this.m_Root.style.display = DisplayStyle.None;

            this.m_ActiveInventory = null;
            this.m_ActiveHealth = null;
            this.m_ActiveMelee = null;
            this.m_HasPurchased = false;
            this.m_ConfirmedEmptyLoadout = false;

            if (this.m_PendingInventory != null)
            {
                PlayerInventory inventory = this.m_PendingInventory;
                Health health = this.m_PendingHealth;
                MeleeAttack melee = this.m_PendingMelee;
                this.m_PendingInventory = null;
                this.m_PendingHealth = null;
                this.m_PendingMelee = null;
                this.OpenShop(inventory, health, melee);
            }
            else
            {
                SetHudVisible(true);
            }
        }

        public void ForceCloseAll()
        {
            if (this.m_ActiveInventory != null)
                this.m_ActiveInventory.OnGoldChanged -= this.HandleGoldChanged;
            this.m_ActiveInventory = null;
            this.m_ActiveHealth = null;
            this.m_ActiveMelee = null;
            this.m_PendingInventory = null;
            this.m_PendingHealth = null;
            this.m_PendingMelee = null;
            this.m_HasPurchased = false;
            this.m_ConfirmedEmptyLoadout = false;
            if (this.m_Root != null) this.m_Root.style.display = DisplayStyle.None;
            SetHudVisible(true);
        }

        private static void SetHudVisible(bool visible)
        {
            var hud = Object.FindAnyObjectByType<HUDController>();
            if (hud != null) hud.SetVisible(visible);
        }

        private void HandleGoldChanged(int _) => this.RefreshShopState();

        private void RefreshShopState()
        {
            int gold = this.m_ActiveInventory != null ? this.m_ActiveInventory.Gold : 0;
            if (this.m_GoldLabel != null) this.m_GoldLabel.text = $"{gold} GOLD";

            this.m_BasicSwordButton?.SetEnabled(gold >= 50 && this.m_ActiveMelee != null && this.m_ActiveMelee.weapon != this.basicSword);
            this.m_AdvancedSwordButton?.SetEnabled(gold >= 100 && this.m_ActiveMelee != null && this.m_ActiveMelee.weapon != this.advancedSword);
            this.m_LightArmorButton?.SetEnabled(gold >= 50 && this.m_ActiveHealth != null && this.m_ActiveHealth.armor == ArmorType.None);
            this.m_HeavyArmorButton?.SetEnabled(gold >= 100 && this.m_ActiveHealth != null && this.m_ActiveHealth.armor != ArmorType.Heavy);
        }

        private void ShowPurchaseError(int price)
        {
            int gold = this.m_ActiveInventory != null ? this.m_ActiveInventory.Gold : 0;
            this.SetStatus(gold < price ? $"NEED {price - gold} MORE GOLD" : "ITEM ALREADY EQUIPPED", true);
            if (this.m_WarningLabel == null) return;
            this.StopAllCoroutines();
            this.StartCoroutine(this.ShakeWarningLabel());
        }

        private void SetStatus(string message, bool error)
        {
            if (this.m_WarningLabel == null) return;
            this.m_WarningLabel.text = message;
            this.m_WarningLabel.style.color = error ? new Color(1f, 0.39f, 0.33f) : new Color(0.95f, 0.78f, 0.36f);
            this.m_WarningLabel.style.visibility = Visibility.Visible;
        }

        private IEnumerator ShakeWarningLabel()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            float magnitude = 10f;

            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
                this.m_WarningLabel.style.translate = new Translate(x, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            this.m_WarningLabel.style.translate = new Translate(0f, 0f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
