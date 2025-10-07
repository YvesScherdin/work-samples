namespace MageGame.AI.Data
{
    [System.Serializable]
    public class PerceptionData
    {
        public PerceptionType type;
        public float range;
        public float outOffset;
    }

    public enum PerceptionType
    {
        Unspecified = 0,
        Proximity = 1,
        Visual = 2,
        Auditive = 3
    }
}
