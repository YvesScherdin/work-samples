using MageGame.Actions;
using MageGame.Actions.ToolBased;
using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.States;
using MageGame.Animation.Core.Data;
using MageGame.Behaviours.EntityType.Furniture;
using UnityEngine;

namespace MageGame.AI.Agents.Citizen
{
    public class AIHandleCargoParameters : AIActionParameters
    {
        public GameObject targetObject;
        public bool unload;

        static public AIHandleCargoParameters Create(GameObject targetObject, bool unload)
        {
            AIHandleCargoParameters parameters = new AIHandleCargoParameters();
            parameters.targetObject = targetObject;
            parameters.unload = unload;
            return parameters;
        }
    }
    
    public class AIAS_HandleCargo : AIActionState
    {
        public override AIActionType ActionType => AIActionType.HandleCargo;

        private AIHandleCargoParameters concreteParameters;
        private AnimationContext animationContext;
        private CargoHandlingContext cargoContext;

        public override void SetParameters(AIActionParameters parameters)
        {
            if (parameters is AIHandleCargoParameters)
                this.concreteParameters = (AIHandleCargoParameters)parameters;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Enter()
        {
            base.Enter();

            if (animationContext == null)
            {
                animationContext = new AnimationContext(context.myObjectInfo.movement.model);
            }

            cargoContext = new CargoHandlingContext(agent.gameObject, concreteParameters.targetObject);

            actions.CurrentSkill = null;
            executionCoroutine = agent.StartCoroutine(UpdateCoroutine());
        }

        private System.Collections.IEnumerator UpdateCoroutine()
        {
            yield return null;

            if (concreteParameters.targetObject == null)
            {
                Interrupt();
                yield break;
            }

            // focus target
            context.MakeCurrentTarget(concreteParameters.targetObject);
            context.checks.UpdateTarget();
            context.targetInvalid = false;

            // wait until target is parked

            for (int i = 0; i < 3; i++)
            {
                while (IsTargetNotStill())
                {
                    yield return new WaitForSeconds(.5f);

                    if (context.targetInvalid || !context.targetObjectInfo.Exists())
                    {
                        Interrupt();
                        yield break;
                    }
                }

                yield return new WaitForSeconds(.1f);
            }

            if (context.targetInvalid || !context.targetObjectInfo.Exists())
            {
                Interrupt();
                yield break;
            }

            ICargoCarrier cargoCarrier = context.targetObjectInfo.gameObject.GetComponent<ICargoCarrier>();
            
            if (cargoCarrier == null || cargoCarrier.GetCargo() == null)
            {
                Interrupt();
                yield break;
            }

            context.MakeCurrentTarget(cargoCarrier.GetCargoCarrierLocator());
            context.checks.UpdateTarget();
            context.targetInvalid = false;

            // move to target
            while (!context.checks.nearObject.Check())
            {
                context.movement.Move();
                yield return new WaitUntil(context.movement.HasStoppedMovement);

                if (concreteParameters.targetObject == null)
                {
                    Interrupt();
                    yield break;
                }
            }

            if (cargoContext.carrier.CargoWorker != null)
            {
                Debug.Log(cargoContext.carrier + " has already cargo worker: " + cargoContext.carrier.CargoWorker.name);
                Interrupt();
                yield break;
            }

            // start action

            if (concreteParameters.unload)
            {
                if (cargoContext.cargo.CanBeUnpackedAnyFurther())
                {
                    cargoContext.Begin();
                    animationContext.Begin(AnimationParamID.Unloading);
                    animationContext.ListenForAction(AnimationID.Detach, cargoContext.DetachNext);
                    animationContext.ListenForAction(AnimationID.TurnToFront, cargoContext.TurnCarriedCargoToFront);
                    animationContext.ListenForAction(AnimationID.PutDown, cargoContext.PutDown);

                    yield return new WaitForSeconds(.5f);
                    yield return new WaitUntil(cargoContext.IsUnloadedCompletely);
                    //Debug.Log("Is unloaded completely | " + animationContext.IsOver());

                    animationContext.ListenForOver(AnimationID.Unload);
                    yield return new WaitUntil(animationContext.IsOver);
                }
            }
            else
            {
                if (cargoContext.cargo.CanBePackedAnyFurther())
                {
                    cargoContext.Begin();
                    animationContext.Begin(AnimationParamID.Loading);
                    animationContext.ListenForAction(AnimationID.PickUp, cargoContext.PickUpAndPutBackNext);
                    animationContext.ListenForAction(AnimationID.TurnToBack, cargoContext.TurnCarriedCargoToBack);
                    animationContext.ListenForAction(AnimationID.PutBack, cargoContext.PutBack);

                    yield return new WaitForSeconds(.5f);
                    yield return new WaitUntil(cargoContext.IsLoadedCompletely);

                    //Debug.Log("Is loaded completely | " + animationContext.IsOver());
                    animationContext.ListenForOver(AnimationID.Load);
                    yield return new WaitUntil(animationContext.IsOver);
                }
            }

            cargoContext.EndInteractionWithTarget();

            if (!animationContext.IsOver())
                animationContext.End();

            Interrupt();
        }

        private bool IsTargetNotStill()
        {
            return context.targetObjectInfo.movement.Velocity.sqrMagnitude != 0f;
        }

        public override void Execute()
        {
            if (context.targetInvalid)
            {
                Interrupt();
            }
        }

        public override void Leave()
        {
            if (cargoContext.IsBeingHandled())
            {
                cargoContext.EndInteractionWithTarget();
            }

            if (!animationContext.IsOver())
            {
                animationContext.End();
            }

            if (executionCoroutine != null)
            {
                agent.StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }

            base.Leave();
        }
    }
}
