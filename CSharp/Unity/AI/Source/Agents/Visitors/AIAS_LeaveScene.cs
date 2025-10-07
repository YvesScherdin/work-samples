using MageGame.AI.Data;
using MageGame.AI.States;
using UnityEngine;

namespace MageGame.AI.Agents.Visitors
{
    public class AILeaveSceneParameters : AIActionParameters
    {
        public GameObject exit;

        static public AILeaveSceneParameters Create(GameObject exit)
        {
            AILeaveSceneParameters parameters = new AILeaveSceneParameters();
            parameters.exit = exit;
            return parameters;
        }
    }

    public class AIAS_LeaveScene : AIActionState
    {
        public override AIActionType ActionType => AIActionType.LeaveScene;

        private GameObject exit;

        public override void SetParameters(AIActionParameters parameters)
        {
            if(parameters is AILeaveSceneParameters)
            {
                AILeaveSceneParameters concreteParameters = (AILeaveSceneParameters)parameters;
                this.exit = concreteParameters.exit;
            }
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Enter()
        {
            base.Enter();

            context.targetLocked = true;
            context.MakeCurrentTarget(exit);
            checks.UpdateTarget();
            context.targetInvalid = false;

            executionCoroutine = agent.StartCoroutine(UpdateCoroutine());
        }

        public override void Execute()
        {
            base.Execute();

        }

        private System.Collections.IEnumerator UpdateCoroutine()
        {
            yield return null;

            while (!checks.nearObject.Check())
            {
                context.movement.Move();
                yield return new WaitUntil(context.movement.HasStoppedMovement);
            }

            agent.AIEvent.Invoke(Events.AIEventType.SceneExitReached);
            Interrupt();
        }

        public override void Leave()
        {
            if(executionCoroutine != null)
            {
                agent.StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }

            base.Leave();

        }
    }
}
