namespace ArenaCraft
{
    /// <summary>Armor tiers from the GDD (2.2.5). Higher tiers raise max HP.</summary>
    public enum ArmorType
    {
        None,
        Light,
        Heavy
    }

    public static class ArmorTypeExtensions
    {
        /// <summary>Extra max HP granted by this armor tier (Light +25, Heavy +50).</summary>
        public static int BonusHP(this ArmorType armor)
        {
            switch (armor)
            {
                case ArmorType.Light: return 25;
                case ArmorType.Heavy: return 50;
                default: return 0;
            }
        }
    }
}
