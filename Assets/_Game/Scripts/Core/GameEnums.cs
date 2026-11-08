namespace RongRanChamCham
{
    public enum GameState
    {
        Ready,
        Chasing,
        Duel,
        RoundOver,
        Victory,
        GameOver
    }

    public enum FoodType
    {
        BanhMi,     // 🥖 Golden Shield (Immunity 7s)
        Pho,        // 🍜 Heal Con Út + Shield
        Kimbap,     // 🍙 Super Speed Boost (+35%)
        Kimchi      // 🥬 Giant Size (+40% scale/reach)
    }

    public enum DuelDirection
    {
        Left,
        Straight,
        Right
    }

    public enum HunterState
    {
        Stalking,
        Flanking,
        Lunging,
        Stunned,
        Celebrating
    }
}
