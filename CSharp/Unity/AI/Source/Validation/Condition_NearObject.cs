using MageGame.AI.Core;

namespace MageGame.AI.Validation
{
    public class Condition_NearObject : Condition, IReachabilityCondition
    {
        private readonly AITargetInfo origin;
        private readonly AISkillValidation validation;

        public AITargetInfo target;
        public ReachabilityAnalysis analysis;
        public ReachabilityAnalysis GetReachabilityAnalysis() => analysis;

        public float threshold = 1f;

        public Condition_NearObject(AITargetInfo origin, AISkillValidation validation)
        {
            this.origin = origin;
            this.validation = validation;

            analysis = new ReachabilityAnalysis();
        }

        public override bool Check()
        {
            if (target.gameObject == null)
            {
                throw new System.Exception(origin.gameObject.name + " has no target to check proximity to.");
            }

            validation.CheckMoveRange(origin, target.Position, threshold, analysis, true);
            return analysis.reached;
        }

        public override bool IsMet()
        {
            return analysis.reached;
        }
    }
}