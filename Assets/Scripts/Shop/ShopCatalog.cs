using System.Collections.Generic;

namespace ArenaCraft
{
    /// <summary>
    /// The fixed list of upgrades offered in the Shopping Phase. Prices and
    /// effects follow GDD 2.2.6:
    ///   Light Armor  50g  +25 HP
    ///   Heavy Armor 100g  +50 HP
    ///   Basic Sword  50g  moderate damage
    ///   Advanced Sword 100g high damage
    /// FCR2 extra weapons (spear / dagger) are included as optional stretch items.
    /// </summary>
    public static class ShopCatalog
    {
        public static List<ShopItem> BuildDefault()
        {
            return new List<ShopItem>
            {
                new ShopItem("basic_sword",    "Basic Sword",    ItemCategory.Weapon, 50,  weaponDamage: 18f, attackCooldown: 0.55f),
                new ShopItem("advanced_sword", "Advanced Sword", ItemCategory.Weapon, 100, weaponDamage: 30f, attackCooldown: 0.60f),
                new ShopItem("light_armor",    "Light Armor",    ItemCategory.Armor,  50,  bonusMaxHp: 25),
                new ShopItem("heavy_armor",    "Heavy Armor",    ItemCategory.Armor,  100, bonusMaxHp: 50),
                // FCR2 – additional weapons. Cheap, fast dagger / long-range spear feel.
                new ShopItem("dagger",         "Swift Dagger",   ItemCategory.Weapon, 40,  weaponDamage: 12f, attackCooldown: 0.30f),
                new ShopItem("spear",          "Long Spear",     ItemCategory.Weapon, 80,  weaponDamage: 22f, attackCooldown: 0.70f),
            };
        }
    }
}
