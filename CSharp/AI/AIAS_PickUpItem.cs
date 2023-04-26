using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.States;
using MageGame.Behaviours.EntityType;
using MageGame.Utils;
using UnityEngine;
using static MageGame.AI.States.Movement.AIMovementStrategy;

namespace MageGame.AI.Agents.Default
{
    [System.Serializable]
    public class AIPickUpItemParamaeters : AIActionParameters
    {
        public CollectableItem item;

        static public AIPickUpItemParamaeters Create(CollectableItem item)
        {
            AIPickUpItemParamaeters parameters = new AIPickUpItemParamaeters();
            parameters.item = item;
            return parameters;
        }
    }

    public class AIAS_PickUpItem : AIActionState
    {
        public override AIActionType ActionType => AIActionType.PickUp;

        private AIPickUpItemParamaeters concreteParams;
        private bool success;

        public override void Initialize()
        {
            base.Initialize();

            InventoryUtil.GetOrCreateFor(context.gameObject);
        }

        public override void Enter()
        {
            base.Enter();

            success = false;

            if (parameters != null && parameters is AIPickUpItemParamaeters)
            {
                concreteParams = (AIPickUpItemParamaeters)parameters;
            }
            else
            {
                concreteParams = AIPickUpItemParamaeters.Create(null);
            }

            if (concreteParams.item != null)
            {
                context.MakeCurrentTarget(new AITargetInfo().Analyze(concreteParams.item.gameObject));
                context.checks.UpdateTarget();
                context.targetInvalid = false;
                context.checks.boundsOverlap.targetCollider = concreteParams.item.triggerCollider;
            }
            else
            {
                Interrupt();
            }

            if (!WasInterrupted())
            {
                executionCoroutine = agent.StartCoroutine(ExecuteCoroutine());
            }
        }

        public override void Execute()
        {
            base.Execute();

            if (context.targetInvalid)
                Interrupt(success ? AIActionSituation.None : AIActionSituation.Failed);
        }

        private System.Collections.IEnumerator ExecuteCoroutine()
        {
            yield return null;

            while (!checks.nearObject.Check())
            {
                //context.movement.Move(true, true);
                context.movement.Move(MovementTargetType.Bounds, true);
                yield return new WaitUntil(context.movement.HasStoppedMovement);

                if (context.targetObjectInfo.gameObject != concreteParams.item)
                {
                    Interrupt();
                    yield break;
                }

                if(concreteParams.item.triggerCollider.IsTouching(context.myObjectInfo.collider))
                {
                    break;
                }
            }

            concreteParams.item.GetCollected(agent.gameObject, null);
            yield return new WaitForSeconds(.1f);
            Interrupt();
        }

        public override void Exit()
        {
            if(context.movement.IsMoving())
                context.movement.AbortMovement();

            base.Exit();

        }
    }
}
