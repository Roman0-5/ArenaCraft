using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace ArenaCraft
{
    public class HUDController : MonoBehaviour
    {
        [Header("Icons")]
        public Sprite lightArmorIcon;
        public Sprite heavyArmorIcon;

        private UIDocument m_UIDocument;
        private VisualElement m_Root;

        // P1 Elements
        private VisualElement m_P1HPFill;
        private VisualElement m_P1ResourceFill;
        private Label m_P1Gold;
        private VisualElement m_P1WeaponIcon;
        private VisualElement m_P1ArmorIcon;
        
        // P2 Elements
        private VisualElement m_P2HPFill;
        private VisualElement m_P2ResourceFill;
        private Label m_P2Gold;
        private VisualElement m_P2WeaponIcon;
        private VisualElement m_P2ArmorIcon;

        private Label m_TimerLabel;
        private Label m_PhaseLabel;

        private Health m_P1Health;
        private Health m_P2Health;
        private PlayerInventory m_P1Inventory;
        private PlayerInventory m_P2Inventory;
        private MeleeAttack m_P1Melee;
        private MeleeAttack m_P2Melee;

        private void OnEnable()
        {
            this.m_UIDocument = GetComponent<UIDocument>();
            this.m_UIDocument.sortingOrder = 0;
            this.m_Root = this.m_UIDocument.rootVisualElement;
            DisablePicking(this.m_Root);

            this.m_P1HPFill = this.m_Root.Q<VisualElement>("p1-hp-fill");
            this.m_P1ResourceFill = this.m_Root.Q<VisualElement>("p1-resource-fill");
            this.m_P1Gold = this.m_Root.Q<Label>("p1-gold");
            this.m_P1WeaponIcon = this.m_Root.Q<VisualElement>("p1-weapon-icon");
            this.m_P1ArmorIcon = this.m_Root.Q<VisualElement>("p1-armor-icon");

            this.m_P2HPFill = this.m_Root.Q<VisualElement>("p2-hp-fill");
            this.m_P2ResourceFill = this.m_Root.Q<VisualElement>("p2-resource-fill");
            this.m_P2Gold = this.m_Root.Q<Label>("p2-gold");
            this.m_P2WeaponIcon = this.m_Root.Q<VisualElement>("p2-weapon-icon");
            this.m_P2ArmorIcon = this.m_Root.Q<VisualElement>("p2-armor-icon");

            this.m_TimerLabel = this.m_Root.Q<Label>("timer-label");
            this.m_PhaseLabel = this.m_Root.Q<Label>("phase-label");

            FindPlayers();
        }

        private static void DisablePicking(VisualElement element)
        {
            element.pickingMode = PickingMode.Ignore;
            foreach (var child in element.Children())
                DisablePicking(child);
        }

        private void FindPlayers()
        {
            var players = Object.FindObjectsByType<PlayerInputProvider>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.Slot == PlayerSlot.One)
                {
                    this.m_P1Health = p.GetComponent<Health>();
                    this.m_P1Inventory = p.GetComponent<PlayerInventory>();
                    this.m_P1Melee = p.GetComponent<MeleeAttack>();
                }
                else
                {
                    this.m_P2Health = p.GetComponent<Health>();
                    this.m_P2Inventory = p.GetComponent<PlayerInventory>();
                    this.m_P2Melee = p.GetComponent<MeleeAttack>();
                }
            }
        }

        private void Update()
        {
            UpdatePlayer(this.m_P1Health, this.m_P1Inventory, this.m_P1Melee, this.m_P1HPFill, this.m_P1ResourceFill, this.m_P1Gold, this.m_P1WeaponIcon, this.m_P1ArmorIcon);
            UpdatePlayer(this.m_P2Health, this.m_P2Inventory, this.m_P2Melee, this.m_P2HPFill, this.m_P2ResourceFill, this.m_P2Gold, this.m_P2WeaponIcon, this.m_P2ArmorIcon);
            UpdateTimer();
        }

        private void UpdatePlayer(Health health, PlayerInventory inventory, MeleeAttack melee, VisualElement hpFill, VisualElement resFill, Label goldLabel, VisualElement weaponIcon, VisualElement armorIcon)
        {
            if (health != null)
            {
                if (hpFill != null)
                {
                    float hpPercent = health.Normalized * 100f;
                    hpFill.style.width = Length.Percent(hpPercent);
                    
                    if (health.Normalized < 0.3f) hpFill.AddToClassList("low");
                    else hpFill.RemoveFromClassList("low");
                }

                if (armorIcon != null)
                {
                    Sprite s = health.armor switch
                    {
                        ArmorType.Light => this.lightArmorIcon,
                        ArmorType.Heavy => this.heavyArmorIcon,
                        _ => null
                    };
                    armorIcon.style.backgroundImage = s != null ? new StyleBackground(s) : null;
                    armorIcon.style.display = s != null ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            if (inventory != null)
            {
                if (resFill != null)
                {
                    resFill.style.height = Length.Percent(inventory.ResourceFillAmount * 100f);
                }

                if (goldLabel != null)
                {
                    goldLabel.text = $"{inventory.Gold} GOLD";
                }
            }

            if (melee != null && weaponIcon != null)
            {
                weaponIcon.style.backgroundImage = (melee.weapon != null && melee.weapon.icon != null) ? new StyleBackground(melee.weapon.icon) : null;
                weaponIcon.style.display = (melee.weapon != null && melee.weapon.icon != null) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void UpdateTimer()
        {
            if (GamePhaseManager.Instance != null && this.m_TimerLabel != null)
            {
                float time = GamePhaseManager.Instance.PhaseTimer;
                int minutes = Mathf.FloorToInt(time / 60);
                int seconds = Mathf.FloorToInt(time % 60);
                this.m_TimerLabel.text = string.Format("{0:00}:{1:00}", minutes, seconds);
                if (this.m_PhaseLabel != null)
                {
                    this.m_PhaseLabel.text = GamePhaseManager.Instance.CurrentPhase switch
                    {
                        GamePhase.Resource => "RESOURCE PHASE",
                        GamePhase.Shopping => "SHOP PHASE",
                        GamePhase.BattleRoyale => "BATTLE ROYALE",
                        _ => "GET READY"
                    };
                }
            }
        }
    }
}
