using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArenaCraft
{
    /// <summary>
    /// One player's view of the item shop during the Shopping Phase (GDD 2.2.3 /
    /// FMR5/FMR6). Fully keyboard driven with that player's own keys:
    ///   up/down  – move the selection
    ///   attack   – buy the highlighted item
    ///   interact – mark yourself "Ready"
    /// Shows a "You didn't buy anything!" warning near the end if nothing was
    /// purchased (FMR7).
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        private PlayerConfig _config;
        private PlayerStats _stats;
        private List<ShopItem> _items;
        private readonly List<Text> _rows = new List<Text>();
        private int _selection;
        private bool _active;
        private Text _statusText;
        private Text _warningText;
        private GameObject _root;

        public bool Ready { get; private set; }

        public void Build(Transform canvas, PlayerConfig config, PlayerStats stats, List<ShopItem> items, bool leftSide)
        {
            _config = config;
            _stats = stats;
            _items = items;

            _root = UIFactory.Panel($"Shop_P{config.playerId}", canvas, new Color(0.12f, 0.10f, 0.08f, 0.95f));
            var rt = _root.GetComponent<RectTransform>();
            float anchorX = leftSide ? 0f : 1f;
            float posX = leftSide ? 230f : -230f;
            UIFactory.Anchor(rt, new Vector2(anchorX, 0.5f), new Vector2(anchorX, 0.5f), new Vector2(posX, -20f), new Vector2(380f, 420f));

            var title = UIFactory.Label("Title", _root.transform, $"{config.label}  —  SHOP", 22, config.accentColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -28f), new Vector2(-20f, 34f));

            for (int i = 0; i < _items.Count; i++)
            {
                var row = UIFactory.Label($"Item{i}", _root.transform, "", 17, Color.white, TextAnchor.MiddleLeft);
                UIFactory.Anchor(row.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -70f - i * 34f), new Vector2(-30f, 30f));
                _rows.Add(row);
            }

            _statusText = UIFactory.Label("Status", _root.transform, "", 14, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(_statusText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 64f), new Vector2(-20f, 50f));

            var help = UIFactory.Label("Help", _root.transform,
                $"[{KeyName(config.up)}/{KeyName(config.down)}] Select   [{KeyName(config.attack)}] Buy   [{KeyName(config.interact)}] Ready",
                12, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter);
            UIFactory.Anchor(help.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 30f), new Vector2(-20f, 24f));

            _warningText = UIFactory.Label("Warning", _root.transform, "", 16, new Color(1f, 0.4f, 0.3f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Anchor(_warningText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 96f), new Vector2(-20f, 26f));

            Refresh();
            SetActive(false);
        }

        public void SetActive(bool on)
        {
            _active = on;
            Ready = false;
            _selection = 0;
            if (_root != null) _root.SetActive(on);
            if (on) Refresh();
        }

        private void Update()
        {
            if (!_active) return;

            if (!Ready)
            {
                if (_config.UpPressed()) { _selection = (_selection - 1 + _items.Count) % _items.Count; Refresh(); }
                if (_config.DownPressed()) { _selection = (_selection + 1) % _items.Count; Refresh(); }
                if (_config.AttackPressed()) TryBuy();
            }

            if (_config.InteractPressed())
            {
                Ready = !Ready;
                Refresh();
            }
        }

        private void TryBuy()
        {
            var item = _items[_selection];
            if (_stats.TryBuy(item))
            {
                AudioManager.Instance?.PlayCollect();
                _statusText.text = $"Bought {item.displayName}!";
            }
            else
            {
                bool owned = (item.category == ItemCategory.Weapon && _stats.Weapon != null && _stats.Weapon.id == item.id)
                          || (item.category == ItemCategory.Armor && _stats.Armor != null && _stats.Armor.id == item.id);
                _statusText.text = owned ? "Already owned." : "Not enough gold!";
                AudioManager.Instance?.PlayHit();
            }
            Refresh();
        }

        /// <summary>Show the no-purchase warning (FMR7) when time is short.</summary>
        public void UpdateWarning(bool showIfNoPurchase)
        {
            if (_warningText == null) return;
            _warningText.text = (showIfNoPurchase && !_stats.MadeAnyPurchase) ? "You didn't buy anything!" : "";
        }

        private void Refresh()
        {
            _statusText.text = Ready ? "READY — waiting for opponent..." : _statusText.text;
            for (int i = 0; i < _rows.Count; i++)
            {
                var item = _items[i];
                bool owned = (item.category == ItemCategory.Weapon && _stats.Weapon != null && _stats.Weapon.id == item.id)
                          || (item.category == ItemCategory.Armor && _stats.Armor != null && _stats.Armor.id == item.id);
                bool afford = _stats.Gold >= item.cost;
                string cursor = (i == _selection && !Ready) ? "> " : "  ";
                string tag = item.category == ItemCategory.Weapon ? "[Wpn]" : "[Arm]";
                string detail = item.category == ItemCategory.Weapon ? $"{item.weaponDamage:0} dmg" : $"+{item.bonusMaxHp} HP";
                _rows[i].text = $"{cursor}{item.displayName}  {tag}  {item.cost}g  ({detail})" + (owned ? "  [OWNED]" : "");
                _rows[i].color = owned ? new Color(0.5f, 0.9f, 0.5f)
                              : (i == _selection && !Ready) ? Color.white
                              : afford ? new Color(0.85f, 0.85f, 0.85f)
                              : new Color(0.55f, 0.5f, 0.45f);
            }
        }

        private static string KeyName(Key k)
        {
            switch (k)
            {
                case Key.UpArrow: return "Up";
                case Key.DownArrow: return "Down";
                case Key.LeftArrow: return "Left";
                case Key.RightArrow: return "Right";
                case Key.Enter: return "Enter";
                case Key.RightShift: return "RShift";
                case Key.Space: return "Space";
                default: return k.ToString();
            }
        }
    }
}
