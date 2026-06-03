using UnityEngine;
using UnityEngine.UI;

namespace ArenaCraft
{
    /// <summary>
    /// One side panel of the shared-screen HUD (GDD 5.1): player label, HP bar
    /// (turns red below 30% – FSR4), vertical resource bar, gold counter and
    /// equipped weapon/armor text. Left panel for P1, right panel for P2.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        private PlayerStats _stats;
        private GameSettings _settings;
        private GameObject _panel;
        private Image _hpFill;
        private Text _hpText;
        private Image _resourceFill;
        private Text _goldText;
        private Text _equipText;

        private static readonly Color HpGreen = new Color(0.30f, 0.80f, 0.35f);
        private static readonly Color HpRed = new Color(0.85f, 0.20f, 0.18f);

        public void Build(Transform canvas, PlayerConfig config, PlayerStats stats, GameSettings settings, bool leftSide)
        {
            _stats = stats;
            _settings = settings;

            var panel = UIFactory.Panel($"HUD_P{config.playerId}", canvas, new Color(0.08f, 0.07f, 0.06f, 0.78f));
            _panel = panel;
            var rt = panel.GetComponent<RectTransform>();
            if (leftSide)
                UIFactory.Anchor(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -90f), new Vector2(290f, 150f));
            else
                UIFactory.Anchor(rt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-160f, -90f), new Vector2(290f, 150f));

            // Accent stripe.
            var stripe = UIFactory.Image("Accent", panel.transform, config.accentColor);
            UIFactory.Anchor(stripe.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(5f, 0f), new Vector2(8f, 0f));

            // Player label.
            var label = UIFactory.Label("Label", panel.transform, config.label, 20, config.accentColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -20f), new Vector2(-30f, 28f));

            // HP bar.
            _hpFill = UIFactory.Bar("HP", panel.transform, new Color(0.2f, 0.05f, 0.05f), HpGreen);
            UIFactory.Anchor(_hpFill.transform.parent.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -52f), new Vector2(-150f, 22f));
            _hpText = UIFactory.Label("HPText", panel.transform, "", 14, Color.white, TextAnchor.MiddleLeft);
            UIFactory.Anchor(_hpText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -52f), new Vector2(-30f, 22f));

            // Gold counter.
            _goldText = UIFactory.Label("Gold", panel.transform, "Gold: 0", 18, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UIFactory.Anchor(_goldText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -80f), new Vector2(-30f, 22f));

            // Equipped items.
            _equipText = UIFactory.Label("Equip", panel.transform, "", 13, new Color(0.85f, 0.85f, 0.85f), TextAnchor.MiddleLeft);
            UIFactory.Anchor(_equipText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(28f, -106f), new Vector2(-30f, 22f));

            // Vertical resource bar on the inner edge.
            _resourceFill = UIFactory.Bar("Resource", panel.transform, new Color(0.1f, 0.12f, 0.08f), new Color(0.45f, 0.75f, 0.35f), vertical: true);
            var resRt = _resourceFill.transform.parent.GetComponent<RectTransform>();
            float x = leftSide ? -22f : 22f;
            UIFactory.Anchor(resRt, new Vector2(leftSide ? 1f : 0f, 0.5f), new Vector2(leftSide ? 1f : 0f, 0.5f), new Vector2(x, 0f), new Vector2(26f, 120f));
            var resLabel = UIFactory.Label("ResLbl", resRt, "R", 11, Color.white, TextAnchor.UpperCenter);
            UIFactory.Anchor(resLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 8f), new Vector2(0f, 16f));

            // Subscribe.
            stats.OnHealthChanged += RefreshHealth;
            stats.OnResourcesChanged += RefreshResources;
            stats.OnGoldChanged += RefreshGold;
            stats.OnEquipmentChanged += RefreshEquip;

            RefreshHealth();
            RefreshResources();
            RefreshGold();
            RefreshEquip();
        }

        public void SetVisible(bool visible)
        {
            if (_panel != null) _panel.SetActive(visible);
        }

        private void RefreshHealth()
        {
            if (_hpFill == null) return;
            float frac = _stats.HpFraction;
            _hpFill.fillAmount = frac;
            _hpFill.color = frac <= _settings.lowHpFraction ? HpRed : HpGreen;
            _hpText.text = $"{Mathf.CeilToInt(_stats.CurrentHp)}/{_stats.MaxHp}";
        }

        private void RefreshResources()
        {
            if (_resourceFill == null) return;
            _resourceFill.fillAmount = _stats.ResourceFraction;
        }

        private void RefreshGold()
        {
            if (_goldText == null) return;
            _goldText.text = $"Gold: {_stats.Gold}";
        }

        private void RefreshEquip()
        {
            if (_equipText == null) return;
            _equipText.text = $"Wpn: {_stats.WeaponName}   Arm: {_stats.ArmorName}";
        }

        private void OnDestroy()
        {
            if (_stats == null) return;
            _stats.OnHealthChanged -= RefreshHealth;
            _stats.OnResourcesChanged -= RefreshResources;
            _stats.OnGoldChanged -= RefreshGold;
            _stats.OnEquipmentChanged -= RefreshEquip;
        }
    }
}
