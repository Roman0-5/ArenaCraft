using System;
using UnityEngine;

namespace ArenaCraft
{
    /// <summary>
    /// Holds the mutable state for one gladiator: health, gold, harvested
    /// resources and the currently equipped weapon / armor. Raises events so the
    /// HUD can refresh without polling.
    /// </summary>
    public class PlayerStats
    {
        private readonly GameSettings _settings;

        public PlayerStats(GameSettings settings)
        {
            _settings = settings;
            MaxHp = settings.baseMaxHp;
            CurrentHp = MaxHp;
            ResourceCapacity = settings.resourceBarCapacity;
        }

        // ---- Health ----
        public int MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0f;
        public float HpFraction => MaxHp <= 0 ? 0f : Mathf.Clamp01(CurrentHp / MaxHp);

        // ---- Economy ----
        public int Gold { get; private set; }
        public int ResourceUnits { get; private set; }   // total raw units carried
        public int ResourceCapacity { get; private set; }
        public bool ResourceBarFull => ResourceUnits >= ResourceCapacity;
        public float ResourceFraction => ResourceCapacity <= 0 ? 0f : Mathf.Clamp01((float)ResourceUnits / ResourceCapacity);

        // ---- Equipment ----
        public ShopItem Weapon { get; private set; }     // null = bare fists
        public ShopItem Armor { get; private set; }      // null = no armor
        public bool MadeAnyPurchase { get; private set; }

        public float WeaponDamage => Weapon != null ? Weapon.weaponDamage : _settings.baseFistDamage;
        public float AttackCooldown => Weapon != null ? Weapon.attackCooldown : _settings.baseFistCooldown;
        public string WeaponName => Weapon != null ? Weapon.displayName : "Fists";
        public string ArmorName => Armor != null ? Armor.displayName : "None";

        // Events for the HUD.
        public event Action OnHealthChanged;
        public event Action OnResourcesChanged;
        public event Action OnGoldChanged;
        public event Action OnEquipmentChanged;
        public event Action<PlayerStats> OnDied;

        /// <summary>
        /// Collect resources from a node. Returns the units actually accepted
        /// (capped by the resource bar). Gold is awarded immediately for the
        /// accepted units (auto-conversion, GDD 2.2.6 / FMR3).
        /// </summary>
        public int CollectResource(ResourceTypeDef def, int offeredUnits)
        {
            int room = Mathf.Max(0, ResourceCapacity - ResourceUnits);
            int accepted = Mathf.Min(room, offeredUnits);
            if (accepted <= 0) return 0;

            ResourceUnits += accepted;
            Gold += accepted * def.goldPerUnit;
            OnResourcesChanged?.Invoke();
            OnGoldChanged?.Invoke();
            return accepted;
        }

        public bool TrySpend(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            OnGoldChanged?.Invoke();
            return true;
        }

        public bool TryBuy(ShopItem item)
        {
            if (item == null) return false;
            // Disallow re-buying an identical item or a strictly worse one of the same slot.
            if (item.category == ItemCategory.Weapon && Weapon != null && Weapon.id == item.id) return false;
            if (item.category == ItemCategory.Armor && Armor != null && Armor.id == item.id) return false;
            if (!TrySpend(item.cost)) return false;

            if (item.category == ItemCategory.Weapon)
            {
                Weapon = item;
            }
            else
            {
                Armor = item;
                RecomputeMaxHp();
            }
            MadeAnyPurchase = true;
            OnEquipmentChanged?.Invoke();
            return true;
        }

        private void RecomputeMaxHp()
        {
            int newMax = _settings.baseMaxHp + (Armor != null ? Armor.bonusMaxHp : 0);
            // Buying armor between phases also tops you up to the new maximum.
            float missing = MaxHp - CurrentHp;
            MaxHp = newMax;
            CurrentHp = Mathf.Clamp(MaxHp - missing, 1f, MaxHp);
            OnHealthChanged?.Invoke();
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;
            CurrentHp = Mathf.Max(0f, CurrentHp - amount);
            OnHealthChanged?.Invoke();
            if (CurrentHp <= 0f) OnDied?.Invoke(this);
        }

        public void Heal(float amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            OnHealthChanged?.Invoke();
        }

        /// <summary>Reset health to full at the start of the Battle Royale phase.</summary>
        public void FullHeal()
        {
            CurrentHp = MaxHp;
            OnHealthChanged?.Invoke();
        }

        /// <summary>
        /// Wipe all per-match progress (each round starts fresh – GDD 2.1.1 / 2.4).
        /// Kept on the same instance so HUD/shop event subscriptions stay valid.
        /// </summary>
        public void ResetForNewMatch()
        {
            Weapon = null;
            Armor = null;
            MadeAnyPurchase = false;
            MaxHp = _settings.baseMaxHp;
            CurrentHp = MaxHp;
            Gold = 0;
            ResourceUnits = 0;
            ResourceCapacity = _settings.resourceBarCapacity;

            OnEquipmentChanged?.Invoke();
            OnHealthChanged?.Invoke();
            OnGoldChanged?.Invoke();
            OnResourcesChanged?.Invoke();
        }
    }
}
