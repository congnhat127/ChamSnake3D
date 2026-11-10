namespace RongRanChamCham
{
    public enum GameState
    {
        MainMenu,
        Showcase,
        Ready,
        Chasing,
        Duel,
        RoundOver,
        Victory,
        GameOver
    }

    public enum FoodType
    {
        BanhMi,     // 🥖 Shield 1-hit for Snake / Pierce Shield for Hunter (7s)
        Pho,        // 🍜 Super Speed Boost (+35%) for 7s
        Kimbap,     // 🍙 Giant Size (+30% scale/reach) for 7s
        Tteokbokki  // 🥘 Decoy Clone (5s duration, fades after 2s, rare 10%)
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
