namespace MageGame.AI.Data
{
    public enum BossActionType
    {
        None = 0,
        Attack = 1,
        Defend = 2,
        SpecialAttack = 4,
        Heal = 8,
        Rest = 16,
        CallForHelp = 32,
        Escalate = 64,
        FreeStyle = 128,

    }
}
