using MageGame.AI.Data;
using MageGame.AI.States;
using MageGame.AI.World;
using MageGame.Behaviours.Mechanisms;

namespace MageGame.AI.Agents.Visitors
{
    public class AIBS_WorkAtCaravan : AIBehaviourState
    {
        override public AIBehaviourType BehaviourType => AIBehaviourType.WorkAtCaravan;

        private AITraderCaravanVisit caravanVisit;
        private UsableObject usableObject;

        public override void Initialize()
        {
            base.Initialize();

            usableObject = agent.GetComponent<UsableObject>();

            if (usableObject != null)
                usableObject.enabled = false;
        }

        public override void Enter()
        {
            base.Enter();

            if(usableObject != null)
                usableObject.enabled = false;

            caravanVisit = agent.aiGroup is AITraderCaravanVisit ? (AITraderCaravanVisit)agent.aiGroup : null;
            context.actionInvalid = true;
        }

        public override void Execute()
        {
            base.Execute();

            if (caravanVisit != null)
            {
                if (context.actionInvalid)
                {
                    caravanVisit.NotifyIdleWorker(agent);
                    context.actionInvalid = false;
                }
            }  
        }

        public override void Leave()
        {
            base.Leave();

        }
    }
}
