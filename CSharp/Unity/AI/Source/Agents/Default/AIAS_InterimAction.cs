using MageGame.AI.Data;
using MageGame.AI.States;
using MageGame.World;
using MageGame.AI.Core;
using UnityEngine;
using MageGame.Animation.Core.Data;
using MageGame.Common.Data;
using MageGame.Common.Utils;

namespace MageGame.AI.Agents.Default
{
    public class AIAS_InterimAction : AIActionState
    {
        public override AIActionType ActionType => AIActionType.Interim;

        protected AIInterimActionType interimActionType;
        private float time;
        private bool stateRelay;

        public override void Enter()
        {
            base.Enter();

            // reset
            stateRelay = false;

            if(parameters != null)
            {
                if (parameters is AIInterimActionParameters)
                {
                    interimActionType = ((AIInterimActionParameters)parameters).type;
                }
                else
                {
                    interimActionType = AIInterimActionType.Taunt;
                }

                time = parameters.duration.GetRandomValue();
            }
            else
            {
                interimActionType = AIInterimActionType.Taunt;
                time = AIDecisions.DefaultTiming.GetRandomValue();
            }

            // adjust
            switch (interimActionType)
            {
                case AIInterimActionType.Taunt:
                    context.animator.SetBool(AnimationParamID.Taunting, true);
                    break;

                case AIInterimActionType.CombatIdle:
                    context.animator.SetBool(AnimationParamID.CombatIdle, true);
                    break;

                default:
                    stateRelay = true;
                    break;
            }
        }

        public override void Execute()
        {
            base.Execute();

            if (stateRelay)
            {
                switch (interimActionType)
                {
                    case AIInterimActionType.Backen:
                        context.ChangeSubAction(AIActionType.FleeFrom, AIActionParameters.Create(RangeF.MinMax(time,time)));
                        return;

                    case AIInterimActionType.Defend:
                        context.ChangeSubAction(AIActionType.Defend, AIActionParameters.Create(RangeF.MinMax(time, time)));
                        return;

                    case AIInterimActionType.Cover:
                        context.ChangeSubAction(AIActionType.Cover, AIActionParameters.Create(RangeF.MinMax(time, time)));
                        return;

                    case AIInterimActionType.Watch:
                        context.ChangeSubAction(AIActionType.Watch, AIActionParameters.Create(RangeF.MinMax(time, time)));
                        return;

                    case AIInterimActionType.Wander:
                        context.actions.StartWandering();
                        return;
                }
            }

            actions.TargetSide = context.DetermineSideToTarget();
            context.movement.PointTo(actions.TargetSide);

            time -= GameTime.deltaTime;

            if (time <= 0f)
                Interrupt();
        }

        public override void Leave()
        {
            base.Leave();

            switch (interimActionType)
            {
                case AIInterimActionType.Backen:
                    break;

                case AIInterimActionType.Taunt:
                    context.animator.SetBool(AnimationParamID.Taunting, false);
                    break;

                case AIInterimActionType.CombatIdle:
                    context.animator.SetBool(AnimationParamID.CombatIdle, false);
                    break;
            }
        }
    }
}
