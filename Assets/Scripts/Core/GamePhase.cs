namespace ArenaCraft
{
    /// <summary>
    /// The high-level states the match can be in. The flow follows the GDD
    /// screen flow: Main Menu -> Resource -> Shopping -> Battle Royale -> Victory.
    /// </summary>
    public enum GamePhase
    {
        MainMenu,
        ResourcePhase,
        ShoppingPhase,
        BattleRoyale,
        GameOver
    }
}
