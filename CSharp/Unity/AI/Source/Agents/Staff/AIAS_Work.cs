using MageGame.AI.Data;
using MageGame.AI.JobSystem;
using MageGame.AI.JobSystem.Data;
using MageGame.AI.States;
using MageGame.AI.SubComponents;
using MageGame.Behaviours.Mechanisms;
using System.Collections;
using UnityEngine;

namespace MageGame.AI.Agents.Staff
{
    public class AIAS_Work : AIActionState
    {
        public override AIActionType ActionType => AIActionType.Work;

        private AIJobReceiver jobReceiver;
        private IPermanentlyUsable usableTarget;
        private IJobProvider jobProvider;
        private ObjectUser objectUser;

        public override void Initialize()
        {
            base.Initialize();

            jobReceiver = context.gameObject.GetComponent<AIJobReceiver>();
        }

        public override void Enter()
        {
            base.Enter();

            jobProvider = (IJobProvider)jobReceiver.CurrentJob.targetBehaviour;
            usableTarget = (IPermanentlyUsable)jobReceiver.CurrentJob.targetBehaviour;
            objectUser = context.gameObject.GetComponent<ObjectUser>();

            if (objectUser == null)
            {
                Debug.LogWarning("Missing ObjectUser on " + context.gameObject);
            }

            if (usableTarget == null)
            {
                Debug.LogWarning("job target not usable: " + jobProvider);
                Interrupt(AIActionSituation.Failed);
            }

            executionCoroutine = agent.StartCoroutine(UpdateCoroutine());
        }

        private IEnumerator UpdateCoroutine()
        {
            yield return null;

            while (!checks.nearObject.Check())
            {
                context.movement.Move(false);
                yield return new WaitUntil(context.movement.HasStoppedMovement);
            }

            UseTarget();
        }

        public override void Execute()
        {
            base.Execute();

            if (jobReceiver.CurrentJob == null || !jobProvider.HasPendingJobs())
            {
                Interrupt(AIActionSituation.End);

                if (jobReceiver.CurrentJob != null)
                    jobReceiver.FinishJob();

                if (usableTarget.IsUsedBy(context.gameObject))
                    usableTarget.AbortUse();
            }
        }

        public override void Leave()
        {
            if (context.movement.IsMoving())
                context.movement.AbortMovement();

            if (usableTarget.IsUsedBy(context.gameObject))
                usableTarget.AbortUse();

            base.Leave();
        }

        private void UseTarget()
        {
            if (usableTarget.IsInUse())
            {
                if (!usableTarget.IsUsedBy(context.gameObject))
                {
                    Interrupt(AIActionSituation.Failed);
                }
                else
                {
                    Debug.LogWarning("Object use has started to early already: " + jobReceiver.CurrentJob.target + " by " + context.gameObject.name);
                }
            }
            else
            {

            }

            UsableStatusType result = usableTarget.Use(objectUser);
            if (result != UsableStatusType.Started)
            {
                Debug.LogWarning("Could not use job target: " + jobReceiver.CurrentJob.target + " by " + context.gameObject.name);
                Interrupt(AIActionSituation.Failed);
            }
            else
            {
                switch (jobReceiver.CurrentJob.actionType)
                {
                    case AIJobActionType.Brew:
                        //((AlchemyTable)jobReceiver).
                        break;
                }
            }
        }
    }
}
