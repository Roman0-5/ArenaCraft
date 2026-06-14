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
        private Label m_P1HPText;
        private Label m_P1ResourceText;
        private Label m_P1HitFeedback;
        
        // P2 Elements
        private VisualElement m_P2HPFill;
        private VisualElement m_P2ResourceFill;
        private Label m_P2Gold;
        private VisualElement m_P2WeaponIcon;
        private VisualElement m_P2ArmorIcon;
        private Label m_P2HPText;
        private Label m_P2ResourceText;
        private Label m_P2HitFeedback;

        private Label m_TimerLabel;
        private Label m_PhaseLabel;
        private Label m_PhaseBanner;

        private Health m_P1Health;
        private Health m_P2Health;
        private PlayerInventory m_P1Inventory;
        private PlayerInventory m_P2Inventory;
        private MeleeAttack m_P1Melee;
        private MeleeAttack m_P2Melee;

        public void SetVisible(bool visible)
        {
            if (this.m_Root != null)
                this.m_Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnEnable()
        {
            this.m_UIDocument = GetComponent<UIDocument>();
            this.m_UIDocument.sortingOrder = 0;
            this.m_Root = this.m_UIDocument.rootVisualElement;
            ResponsiveUILayout.Attach(this.m_Root);
            DisablePicking(this.m_Root);

            this.m_P1HPFill = this.m_Root.Q<VisualElement>("p1-hp-fill");
            this.m_P1ResourceFill = this.m_Root.Q<VisualElement>("p1-resource-fill");
            this.m_P1Gold = this.m_Root.Q<Label>("p1-gold");
            this.m_P1WeaponIcon = this.m_Root.Q<VisualElement>("p1-weapon-icon");
            this.m_P1ArmorIcon = this.m_Root.Q<VisualElement>("p1-armor-icon");
            this.m_P1HPText = this.m_Root.Q<Label>("p1-hp-text");
            this.m_P1ResourceText = this.m_Root.Q<Label>("p1-resource-text");
            this.m_P1HitFeedback = this.m_Root.Q<Label>("p1-hit-feedback");

            this.m_P2HPFill = this.m_Root.Q<VisualElement>("p2-hp-fill");
            this.m_P2ResourceFill = this.m_Root.Q<VisualElement>("p2-resource-fill");
            this.m_P2Gold = this.m_Root.Q<Label>("p2-gold");
            this.m_P2WeaponIcon = this.m_Root.Q<VisualElement>("p2-weapon-icon");
            this.m_P2ArmorIcon = this.m_Root.Q<VisualElement>("p2-armor-icon");
            this.m_P2HPText = this.m_Root.Q<Label>("p2-hp-text");
            this.m_P2ResourceText = this.m_Root.Q<Label>("p2-resource-text");
            this.m_P2HitFeedback = this.m_Root.Q<Label>("p2-hit-feedback");

            this.m_TimerLabel = this.m_Root.Q<Label>("timer-label");
            this.m_PhaseLabel = this.m_Root.Q<Label>("phase-label");
            this.m_PhaseBanner = this.m_Root.Q<Label>("phase-banner");

            FindPlayers();
            if (GamePhaseManager.Instance != null) GamePhaseManager.Instance.OnPhaseChanged += this.HandlePhaseChanged;
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
                    this.BindHealthFeedback(this.m_P1Health, this.m_P1HitFeedback);
                    this.BindResourceFeedback(this.m_P1Inventory, this.m_P1HitFeedback);
                }
                else
                {
                    this.m_P2Health = p.GetComponent<Health>();
                    this.m_P2Inventory = p.GetComponent<PlayerInventory>();
                    this.m_P2Melee = p.GetComponent<MeleeAttack>();
                    this.BindHealthFeedback(this.m_P2Health, this.m_P2HitFeedback);
                    this.BindResourceFeedback(this.m_P2Inventory, this.m_P2HitFeedback);
                }
            }
        }

        private void BindHealthFeedback(Health health, Label feedback)
        {
            if (health == null || feedback == null) return;
            health.OnDamaged += (_, damage) => this.ShowFeedback(feedback, $"-{damage}", false);
            health.OnBlocked += _ => this.ShowFeedback(feedback, "BLOCKED", true);
        }

        private void BindResourceFeedback(PlayerInventory inventory, Label feedback)
        {
            if (inventory == null || feedback == null) return;
            inventory.OnResourceCollected += (type, amount, gold) =>
                this.ShowFeedback(feedback, $"+{amount} {type.ToString().ToUpper()}  +{gold}G", false);
        }

        private void Update()
        {
            UpdatePlayer(this.m_P1Health, this.m_P1Inventory, this.m_P1Melee, this.m_P1HPFill, this.m_P1ResourceFill, this.m_P1Gold, this.m_P1WeaponIcon, this.m_P1ArmorIcon, this.m_P1HPText, this.m_P1ResourceText);
            UpdatePlayer(this.m_P2Health, this.m_P2Inventory, this.m_P2Melee, this.m_P2HPFill, this.m_P2ResourceFill, this.m_P2Gold, this.m_P2WeaponIcon, this.m_P2ArmorIcon, this.m_P2HPText, this.m_P2ResourceText);
            UpdateTimer();
        }

        private void UpdatePlayer(Health health, PlayerInventory inventory, MeleeAttack melee, VisualElement hpFill, VisualElement resFill, Label goldLabel, VisualElement weaponIcon, VisualElement armorIcon, Label hpText, Label resourceText)
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

                if (hpText != null) hpText.text = $"{health.CurrentHP} / {health.MaxHP}";

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

                if (resourceText != null)
                {
                    resourceText.text =
                        $"W {inventory.GetResourceCount(ResourceType.Wood)}  " +
                        $"S {inventory.GetResourceCount(ResourceType.Stone)}  " +
                        $"M {inventory.GetResourceCount(ResourceType.Metal)}  " +
                        $"| {inventory.CurrentResources}/{inventory.MaxResources}";
                }
            }

            if (melee != null && weaponIcon != null)
            {
                weaponIcon.style.backgroundImage = (melee.weapon != null && melee.weapon.icon != null) ? new StyleBackground(melee.weapon.icon) : null;
                weaponIcon.style.display = (melee.weapon != null && melee.weapon.icon != null) ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (this.m_PhaseBanner == null) return;
            string text = phase switch
            {
                GamePhase.Resource => "GATHER. BUILD. PREPARE.",
                GamePhase.Shopping => "THE ARMORY IS OPEN",
                GamePhase.BattleRoyale => "FIGHT!",
                _ => ""
            };
            this.StartCoroutine(this.ShowBanner(text));
        }

        private IEnumerator ShowBanner(string text)
        {
            this.m_PhaseBanner.text = text;
            this.m_PhaseBanner.style.opacity = 1f;
            yield return new WaitForSecondsRealtime(1.6f);
            this.m_PhaseBanner.style.opacity = 0f;
        }

        private void ShowFeedback(Label label, string text, bool blocked)
        {
            this.StartCoroutine(this.FeedbackRoutine(label, text, blocked));
        }

        private IEnumerator FeedbackRoutine(Label label, string text, bool blocked)
        {
            label.text = text;
            label.EnableInClassList("blocked", blocked);
            label.style.opacity = 1f;
            label.style.translate = new Translate(0f, 0f);

            float elapsed = 0f;
            const float duration = 0.7f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                label.style.opacity = 1f - t;
                label.style.translate = new Translate(0f, -18f * t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            label.style.opacity = 0f;
        }

        private void OnDisable()
        {
            if (GamePhaseManager.Instance != null) GamePhaseManager.Instance.OnPhaseChanged -= this.HandlePhaseChanged;
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
