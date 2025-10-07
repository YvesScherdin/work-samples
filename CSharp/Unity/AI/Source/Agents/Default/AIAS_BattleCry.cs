using MageGame.AI.Data;
using MageGame.AI.Data.Modules;
using MageGame.AI.States;
using MageGame.Animation.Core.Data;
using MageGame.Common.Utils;
using MageGame.Skills;

namespace MageGame.AI.Agents.Default
{
    public class AIAS_BattleCry : AIActionState
    {
        private const float defaultDuration = 1.5f;

        public override AIActionType ActionType => AIActionType.MakeBattleCry;

        private ActionSkill_Watch watch;
        private bool tauntMode;

        public override void Initialize()
        {
            base.Initialize();

            AIActionModuleBattleCry settings = agent.combat.actions.battleCry;

            tauntMode = settings.taunt;

            if (tauntMode)
                tauntMode = true;
            else if (settings.watch)
                watch = context.gameObject.GetComponent<ActionSkill_Watch>();
        }

        public override void Enter()
        {
            base.Enter();
            
            if(parameters != null)
                Wait(parameters.duration.GetRandomValue());
            else
                Wait(defaultDuration);

            context.movement.StandStill();
            context.movement.PointTo(context.DetermineSideToTarget());

            actions.TargetAngle = context.DetermineAngleToTarget();

            if (watch != null)
            {
                actions.CurrentSkill = watch;
                watch.Intention.hold = true;
            }
            else if (tauntMode)
            {
                context.animator.SetBool(AnimationParamID.Taunting, true);
            }
        }

        public override void Execute()
        {
            base.Execute();

            actions.TargetAngle = context.DetermineAngleToTarget();

            if (waiting)
                return;

            if (watch != null)
            {
                watch.Intention.hold = false;

                if(actions.CurrentSkill == watch)
                    actions.CurrentSkill = null;
            }

            Interrupt();
        }

        public override void Leave()
        {
            if (tauntMode)
                context.animator.SetBool(AnimationParamID.Taunting, false);

            base.Leave();
        }
    }
}
