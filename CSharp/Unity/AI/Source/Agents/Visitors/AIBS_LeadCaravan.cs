using MageGame.AI.Data;
using MageGame.AI.Pathes;
using MageGame.AI.States;
using MageGame.AI.SubComponents;
using MageGame.AI.World;
using MageGame.AI.World.Nodes;
using MageGame.Behaviours.Ability;
using MageGame.Behaviours.Mechanisms;
using MageGame.Entities.Local.Data;
using MageGame.Environmental.Areas.Behaviours;
using System;
using UnityEngine;

namespace MageGame.AI.Agents.Visitors
{
    public class AILeadCaravanParameters : AIActionParameters
    {
        public POIArea targetArea;
        public float stayDuration = -1f;
        public GameObject exit;

        static public AILeadCaravanParameters Create(POIArea targetArea, float stayDuration, GameObject exit)
        {
            AILeadCaravanParameters parameters = new AILeadCaravanParameters();

            parameters.targetArea = targetArea;
            parameters.stayDuration = stayDuration;
            parameters.exit = exit;

            return parameters;
        }
    }

    [System.Serializable]
    public class AITraderCaravanInstructions : AIBasicInstructions
    {
        public TraderSpotPoint[] spots;

        // turn to a specific side?
        // use specific language?
        // throw any products out for testing?
    }

    public class AIBS_LeadCaravan : AIBehaviourState
    {
        // move along POIs, at each, hold position for a while
        // the end action might be leaving the scene. But this is not a clear visitor behaviour.

        override public AIBehaviourType BehaviourType => AIBehaviourType.LeadCaravan;

        private AILeadCaravanParameters parameters;
        private bool parametersInvalid;

        private UsableObject usableObject;
        private AIPOIRoute routeWalker;
        private AITraderCaravanVisit caravanVisit;

        public override void SetParameters(AIActionParameters parameters)
        {
            if (parameters is AILeadCaravanParameters)
            {
                this.parameters = (AILeadCaravanParameters)parameters;
                parametersInvalid = true;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            AISceneTraveller.ProvideWith(agent);
            usableObject = agent.GetComponentInChildren<UsableObject>();

            if (usableObject != null)
                usableObject.enabled = false;
        }

        public override void Enter()
        {
            base.Enter();

            if (parametersInvalid) // avoid starting all over again. Only start over, if parameters have changed. Otherwise, continue from previous state on.
            {
                routeWalker = new AIPOIRoute(agent, new POIArea[] { parameters.targetArea }, parameters.exit);
                parametersInvalid = false;
            }

            if (agent.aiGroup != null && agent.aiGroup is AITraderCaravanVisit)
            {
                caravanVisit = (AITraderCaravanVisit)agent.aiGroup;
                caravanVisit.maxTime = parameters.stayDuration * 60f;
                caravanVisit.CaravanEvent.AddListener(HandleCaravanEvent);
            }
            else
                Debug.Log("caravanVisit is null for " + agent.gameObject.name);

            if (usableObject != null)
                usableObject.enabled = false;

            routeWalker.Begin();
            executionCoroutine = agent.StartCoroutine(UpdateCoroutine());
        }

        public override void Execute()
        {
            base.Execute();

            if (context.targetInvalid)
            {
                context.behaviourInvalid = true;
                context.actionInvalid = true;
                Interrupt();
                //Debug.Log("prio of interruption reason: " + context.targetObjectInfo.gameObject.name + " | " + context.targetObjectInfo.priority);
            }
        }

        private System.Collections.IEnumerator UpdateCoroutine()
        {
            yield return null;
            yield return new WaitUntil(context.actions.CanAct);

            if (caravanVisit.WantsToDepart())
            {
                if (caravanVisit.IsReadyToMoveOn())
                {
                    ChooseExit();
                    yield break;
                }
            }
            else if (caravanVisit.IsOnItsWayHome())
            {
                agent.ChangeAction(AIActionType.LeaveScene, AILeaveSceneParameters.Create(parameters.exit));
                yield break;
            }

            // repeatable code start
            while (routeWalker.IsNotEmpty())
            {
                POIArea nextPosition = null;
                
                if (caravanVisit.currentTradingArea != null)
                {
                    nextPosition = caravanVisit.currentTradingArea;
                }
                else
                {
                    nextPosition = routeWalker.DetermineNearest(true);
                    if (caravanVisit != null)
                        caravanVisit.NotifyLeaderChoseTarget(nextPosition);
                }
                
                // go to POI
                if (nextPosition == null)
                {
                    Interrupt();
                    break;
                }

                routeWalker.SetWayPoint(nextPosition.gameObject);

                while (!checks.nearObject.Check())
                {
                    routeWalker.ChangePhase(AIActionSituation.Moving);
                    context.movement.Move(false);
                    yield return new WaitUntil(context.movement.HasStoppedMovement);

                    if (!routeWalker.ValidateWayPointTarget())
                    {
                        Interrupt();
                        yield break;
                    }
                }
                
                ArriveAtTradePoint();

                yield break;
            }
        }

        public override void Leave()
        {
            caravanVisit.CaravanEvent.RemoveListener(HandleCaravanEvent);

            if (usableObject != null)
                usableObject.enabled = true;

            base.Leave();
        }

        #region own decisions
        private void ArriveAtTradePoint()
        {
            if (caravanVisit != null)
                caravanVisit.NotifyLeaderArrived();

            if (usableObject != null)
                usableObject.enabled = true;

            // seek for customer
            agent.ChangeAction(AIActionType.Trade);
            context.actionInvalid = false;
        }

        private void CallToLeave()
        {
            if (usableObject != null)
                usableObject.enabled = false;

            agent.ChangeAction(AIActionType.HoldPosition);

            Speaker.Retrieve(agent.gameObject).Say(ActorMessageType.CaravanLeaving);
        }

        private void ChooseExit()
        {
            caravanVisit.NotifyLeaderChoseExit(parameters.exit);
        }
        #endregion

        #region events
        private void HandleCaravanEvent(CaravanEventType type)
        {
            switch(type)
            {
                case CaravanEventType.Departure:
                    CallToLeave();
                    break;

                case CaravanEventType.DeparturePreparationsReady:
                    ChooseExit();
                    break;
            }
        }
        #endregion
    }
}
