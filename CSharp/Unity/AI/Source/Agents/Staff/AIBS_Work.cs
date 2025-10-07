using MageGame.AI.Agents.Default;
using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.JobSystem;
using MageGame.AI.States;
using MageGame.AI.SubComponents;
using MageGame.Behaviours.Ability;
using MageGame.World.Residences;

namespace MageGame.AI.Agents.Staff
{
    public class AIBS_Work : AIBehaviourState
    {
        override public AIBehaviourType BehaviourType => AIBehaviourType.Work;

        private AIJobReceiver jobReceiver;
        private CharacterJob currentJob;
        private ItemCollector itemCollector;
        
        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Enter()
        {
            base.Enter();

            itemCollector = context.gameObject.GetComponent<ItemCollector>();
            jobReceiver = context.gameObject.GetComponent<AIJobReceiver>();
            currentJob = jobReceiver.CurrentJob;

            context.MakeCurrentTarget(new AITargetInfo().Analyze(currentJob.target));
            context.targetInvalid = false;

            context.actionInvalid = true;
            context.situationInvalid = true;
            context.currentSituation = AIActionSituation.None;
        }

        public override void Execute()
        {
            base.Execute();

            if (context.situationInvalid || context.actionInvalid)
            {
                switch (context.currentSituation)
                {
                    case AIActionSituation.None:
                        StartToWork();
                        break;

                    case AIActionSituation.Execute:
                        context.behaviourInvalid = true;
                        break;

                    case AIActionSituation.Failed:
                        jobReceiver.CancelJob();
                        context.behaviourInvalid = true;
                        break;

                    case AIActionSituation.End:
                        if (itemCollector != null && !itemCollector.Inventory.IsEmpty())
                        {
                            StoreItems();
                        }
                        else
                        {
                            EndWork();
                        }
                        break;

                    case AIActionSituation.Aftermath:
                        EndWork();
                        break;

                    default:
                        context.ChangeSituation(AIActionSituation.None);
                        break;
                }
            }
        }

        private void EndWork()
        {
            context.ChangeSituation(AIActionSituation.None);
            
            context.actionInvalid = true;
            context.behaviourInvalid = true;
        }

        private void StartToWork()
        {
            context.ChangeSituation(AIActionSituation.Execute);
            context.ChangeSubAction(AIActionType.Work);
            context.RevalidateSituation();
            context.actionInvalid = false;
        }

        private void StoreItems()
        {
            AIStorageParameters storageParameters = AIStorageParameters.Create(
                WorldResidenceUtil.FindStorageContainer(context.myObjectInfo.IFF.factionMembership, agent.transform.position),
                true,
                null
            );

            context.ChangeSituation(AIActionSituation.Aftermath);
            context.RevalidateSituation();
            context.actionInvalid = false;
            context.ChangeSubAction(AIActionType.Store, storageParameters);
        }

        public override void Leave()
        {
            base.Leave();

        }
    }
}
