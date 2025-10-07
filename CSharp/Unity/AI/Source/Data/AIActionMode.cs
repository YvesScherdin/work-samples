namespace MageGame.AI.Data
{
    public enum AIActionMode
    {
        None         = 0,
        Attack       = 1 << 0,
        Defend       = 1 << 1,
        Dodge        = 1 << 2,
        Support      = 1 << 3,
        Weaken       = 1 << 4,
        CallForHelp  = 1 << 5,
        Wait         = 1 << 6,
        Retreat      = 1 << 7,
    }
}
