using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.States;
using MageGame.World;

namespace MageGame.AI.Agents.Default
{
    public class AIBS_IdleDefault : AIBehaviourState
    {
        override public AIBehaviourType BehaviourType => AIBehaviourType.Idle;

        protected bool recheckTimerActive = false;
        protected float recheckTimer = 0f;

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Enter()
        {
            base.Enter();

            context.moveParams.hurry = false;
            context.targetInvalid = true;
            context.actionInvalid = true;

            recheckTimer = 0f;
            recheckTimerActive = false;
        }

        public override void Execute()
        {
            base.Execute();

            if (recheckTimerActive)
            {
                recheckTimer -= GameTime.deltaTime;
                if(recheckTimer < 0f)
                {
                    recheckTimerActive = false;
                    context.actionInvalid = true;
                }
            }

            if (context.targetInvalid || context.actionInvalid)
            {
                if (agent.CurrentOrder != null)
                {
                    Interrupt();
                    return;
                }
                
                if (context.HasTarget())
                {
                    if (context.targetObjectInfo.note == AIHandlingNote.Unknown)
                        context.ForgetCurrentTarget();
                    else if(context.targetObjectInfo.note != AIHandlingNote.Ignore)
                    {
                        Interrupt();
                        return;
                    }
                }
                
                DecideIdleAction();
                context.targetInvalid = false;
            }
        }

        public override void Leave()
        {
            base.Leave();
            context.actionInvalid = true;
        }

        virtual protected void DecideIdleAction()
        {
            bool nothingToDo = false;

            if (context.awareness.lastActionOutcome != null)
            {
                switch (context.awareness.lastActionOutcome.type)
                {
                    //case AIActionOutcomeType.DroveAway:
												// TODO: see ticket #42
                        //break;

                    default:
                        nothingToDo = true;
                        break;
                }
            }
            else
                nothingToDo = true;

            if (nothingToDo)
            {
                if (agent.availableActions.wander && !agent.movement.fixPosition)
                    context.actions.StartWandering();
                else
                    context.ChangeSubAction(AIActionType.HoldPosition);
            }

            context.actionInvalid = false;
        }

    }
}
