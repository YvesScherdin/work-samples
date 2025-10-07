using MageGame.AI.Core;
using MageGame.AI.Validation;
using MageGame.Common.Data;

namespace MageGame.AI.Data.Modules
{
    [System.Serializable]
    public class AIModule_KeepDistance : AIDecisionModule
    {
        public int numTicksTolerance;
        public RangeF fleeDistance;

        private ReachabilityAnalysis proximityAnalysis;
        private int numTicksToNear;

        internal override void Initialize()
        {
            base.Initialize();
            proximityAnalysis = context.checks.nearObject.analysis;
        }

        public override bool Check()
        {
            if (context.agent.movement.fixPosition)
                return false;

            switch(decisions.currentActionMode)
            {
                case AIActionMode.Retreat:
                    if (active && proximityAnalysis.distance >= fleeDistance.max)
                    {
                        // end
                        active = false;
                        decisions.InvalidateMode(AIActionMode.None);
                        return false;
                    }
                    break;

                default:
                    if (!active)
                    {
                        if (proximityAnalysis.distance <= fleeDistance.min)
                        {
                            numTicksToNear++;
                            if (numTicksToNear >= numTicksTolerance)
                            {
                                // start
                                active = true;
                                decisions.InvalidateMode(AIActionMode.Retreat);
                                return true;
                            }
                        }
                        else if (active)
                            active = false;
                    }
                    break;
            }
            
            return false;
        }

        public override void Activate()
        {
            numTicksToNear = 0;
            active = false;
        }

        public override void Deactivate()
        {
            
        }

        public override bool HasActionParameters() => true;
        public override AIActionParameters GetActionParameters() => AIConditionParameters.Create(ShallKeepRetreating);

        private bool ShallKeepRetreating()
        {
            if (fleeDistance.max <= 0f)
                return false;

            return proximityAnalysis.distance < fleeDistance.max;
        }
    }
}
