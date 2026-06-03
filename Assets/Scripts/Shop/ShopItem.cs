namespace ArenaCraft
{
    public enum ItemCategory { Weapon, Armor }

    /// <summary>
    /// A purchasable upgrade in the item shop (GDD 2.2.6 / FMR5).
    /// Weapons set the player's per-hit damage; armor raises max HP.
    /// </summary>
    [System.Serializable]
    public class ShopItem
    {
        public string id;
        public string displayName;
        public ItemCategory category;
        public int cost;

        // Weapon stats.
        public float weaponDamage;
        public float attackCooldown; // seconds between swings (resolves issue C.3)

        // Armor stats.
        public int bonusMaxHp;

        public ShopItem(string id, string displayName, ItemCategory category, int cost,
            float weaponDamage = 0f, float attackCooldown = 0f, int bonusMaxHp = 0)
        {
            this.id = id;
            this.displayName = displayName;
            this.category = category;
            this.cost = cost;
            this.weaponDamage = weaponDamage;
            this.attackCooldown = attackCooldown;
            this.bonusMaxHp = bonusMaxHp;
        }
    }
}
