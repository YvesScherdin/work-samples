using MageGame.Actions;
using MageGame.Actions.Data;
using MageGame.AI.Data;
using MageGame.AI.States;
using MageGame.Animation.Core.Data;
using MageGame.Behaviours.Ability;
using MageGame.Behaviours.EntityType.Professions;
using MageGame.Behaviours.Mechanisms;
using MageGame.Common.Utils;
using MageGame.Entities.Local.Data;
using UnityEngine;

namespace MageGame.AI.Agents.Citizen
{
    public class AITradeParameters : AIActionParameters
    {
        public GameObject locator;
        public bool silent;

        static public AITradeParameters Create(GameObject locator, bool silent)
        {
            AITradeParameters parameters = new AITradeParameters();
            parameters.locator = locator;
            parameters.silent = silent;
            return parameters;
        }
    }

    public class AIAS_Trade : AIActionState
    {
        public override AIActionType ActionType => AIActionType.Trade;

        private GameObject targetLocation;
        private bool silent;
        private Speaker speaker;
        private ITrader trader;
        private AnimationContext actionContext;

        public override void SetParameters(AIActionParameters parameters)
        {
            if (parameters is AITradeParameters)
            {
                AITradeParameters concreteParameters = (AITradeParameters)parameters;
                targetLocation = concreteParameters.locator;
                silent = concreteParameters.silent;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            trader = agent.GetComponent<ITrader>();
        }

        public override void Enter()
        {
            base.Enter();

            if (speaker == null)
            {
                speaker = Speaker.Retrieve(agent.gameObject);
                speaker.actor = ActorType.Trader;
            }

            UsableObject usableObject = agent.GetComponent<UsableObject>();
            if (usableObject != null && !usableObject.enabled)
                usableObject.enabled = true;

            actionContext = new AnimationContext(context.myObjectInfo.movement.model);
            executionCoroutine = agent.StartCoroutine(ExecuteCoroutine());
        }

        public override void Execute()
        {
            base.Execute();
        }

        private System.Collections.IEnumerator ExecuteCoroutine()
        {
            yield return null;

            if (targetLocation != null)
            {
                context.MakeCurrentTarget(targetLocation);
                context.checks.UpdateTarget();
                context.targetInvalid = false;

                while (!checks.nearObject.Check())
                {
                    context.movement.Move();
                    yield return new WaitForSeconds(.5f); // give a bit time before checking if reached
                    yield return new WaitUntil(context.movement.HasStoppedMovement);

                    if (targetLocation == null)
                        break;
                }
            }

            while (true)
            {
                if (!silent)
                {
                    // advertise
                    actionContext.Begin(AnimationParamID.Advertising);
                    yield return new WaitForSeconds(.2f);

                    speaker.Say(ActorMessageType.AdvertiseGoods);
                    yield return new WaitForSeconds(2.5f);

                    actionContext.End();
                }

                if (!trader.IsIdle()) yield return new WaitUntil(trader.IsIdle);

                actionContext.Begin(AnimationParamID.Facing);

                for (int i=0; i<3; i++)
                {
                    LookSomewhereElse();
                    yield return new WaitForSeconds(Random.Range(2f, 4f));
                    if (!trader.IsIdle()) yield return new WaitUntil(trader.IsIdle);
                    yield return new WaitForSeconds(.5f);

                    if (i > 1 && RandomUtil.Bool(.25f))
                        break;
                }

                actionContext.End();
            }
        }

        private void LookSomewhereElse()
        {
            context.movement.LookIntoOppositeDirection();
            actionContext.model.animator.SetFloat(AnimationParamID.TargetDirectionH, UnityEngine.Random.Range(0f, 1f));
        }

        public override void Leave()
        {
            targetLocation = null;

            if (!actionContext.IsOver())
                actionContext.End();

            if (executionCoroutine != null)
            {
                agent.StopCoroutine(executionCoroutine);
                executionCoroutine = null;
            }

            base.Leave();
        }
    }
}
