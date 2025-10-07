using MageGame.Common.Data;

namespace MageGame.AI.Data.Modules
{
    [System.Serializable]
    public class AIActionModule : AIModule
    {
        public RangeF duration;
        public float reusableAfter = 0f;
    }
}
