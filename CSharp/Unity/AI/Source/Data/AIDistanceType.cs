namespace MageGame.AI.Data
{
    /// <summary>
    /// Imply entirely different tactical behaviour.
    /// </summary>
    public enum AIDistanceType
    {
        Unspecified = 0,
        Close = 1 << 0,
        Mid = 1 << 1,
        Far = 1 << 2,
    }
}
