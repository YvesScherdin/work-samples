using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.States;
using MageGame.AI.Validation.Utils;
using MageGame.Common.Utils;
using MageGame.Skills;
using UnityEngine;

namespace MageGame.AI.Agents.Default
{
    public class AIAS_Defend : AIActionState
    {
        public override AIActionType ActionType => AIActionType.Defend;

        private ActionSkill blockSkill;

        private bool conditionBased;
        private AISimpleCheck condition;

        public override void Enter()
        {
            base.Enter();

            if(actions.Option != null && actions.Option.skill != null && actions.Option.skill is IDefensiveSkill)
            {
                blockSkill = actions.Option.skill;
            }
            else
            {
                IDefensiveSkill[] defensiveSkills = context.gameObject.GetComponents<IDefensiveSkill>();

                switch(defensiveSkills.Length)
                {
                    case 0: Debug.LogWarning("No defensive skill found");break;
                    case 1: blockSkill = (ActionSkill)defensiveSkills[0];break;
                    default: blockSkill = (ActionSkill)defensiveSkills[UnityEngine.Random.Range(0, defensiveSkills.Length)];break;
                }

            }

            if(blockSkill != null)
                blockSkill.Intention.hold = true;

            // reset settings
            conditionBased = false;
            condition = null;

            if (parameters != null)
            {
                if (parameters is AIConditionParameters)
                {
                    conditionBased = true;
                    condition = ((AIConditionParameters)parameters).check;
                }

                Wait(Mathf.Max(AISettings.minTimeAction, parameters.duration.GetRandomValue()));
            }
        }

        public override void Execute()
        {
            base.Execute();

            actions.TargetSide = context.DetermineSideToTarget();
            actions.TargetAngle = context.DetermineAngleToTarget();

            if(WasInterrupted())
            {
                return;
            }

            if (waiting)
                return;

            if (conditionBased && condition())
                return;
            
            if (blockSkill != null)
                blockSkill.Intention.hold = false;

            Interrupt();
        }

        public override void Leave()
        {
            if(blockSkill != null)
                blockSkill.Intention.hold = false;

            base.Leave();
        }

    }
}
