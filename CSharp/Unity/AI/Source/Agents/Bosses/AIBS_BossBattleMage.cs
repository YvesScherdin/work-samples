using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.Pathes;
using MageGame.AI.States;
using MageGame.AI.Validation;
using MageGame.Common.Data;
using MageGame.Common.Utils;
using MageGame.Events;
using MageGame.Mechanics.Skills.Configurations;
using MageGame.Mechanics.Statuses.Data;
using MageGame.Players;
using MageGame.Skills;
using QPathFinder;
using UnityEngine;
using static MageGame.AI.Pathes.BossCheckPoint;

namespace MageGame.AI.Agents.Bosses
{
    public class AIBS_BossBattleMage : AIBehaviourState
    {
        override public AIBehaviourType BehaviourType => AIBehaviourType.BossBattle;

        private BattleParameters parameters;
        private BossCheckPointSettings defaultCheckPointSettings;

        private bool canWanderCheckPoints;
        private bool healthInvalid;
        private bool actionOver;
        private int escalationLevel;
        private float currentHealthThreshold;

        private Coroutine escalationRoutine;
        private Coroutine checkPointRoutine;
        private Coroutine sideActionsRoutine;
        private Coroutine actionRoutine;
        private BossCheckPoint currentCheckPoint;
        private CheckPointRoute route;
        private GameObject opponent;
        private EscalatoryStepInfo stepInfo;

        #region de-/init
        public override void SetParameters(AIActionParameters parameters)
        {
            this.parameters = (BattleParameters)parameters;
        }

        public override void Initialize()
        {
            base.Initialize();

            escalationLevel = 0;

            defaultCheckPointSettings = new BossCheckPointSettings();
            defaultCheckPointSettings.mode = BossActionType.Defend;
            defaultCheckPointSettings.duration = new RangeF(3f, 5f);
        }

        public override void Enter()
        {
            base.Enter();

            stepInfo = new EscalatoryStepInfo();

            canWanderCheckPoints = false;
            healthInvalid = false;
            actionOver = false;
            currentHealthThreshold = 0f;
            currentCheckPoint = null;
            escalationRoutine = agent.StartCoroutine(UpdateBattlePhases());
            checkPointRoutine = agent.StartCoroutine(UpdateCheckPointWandering(true));

            opponent = PlayerControl.GetHeroGameObject(); // this is not clean at all.
            agent.CurrentOrder = new AIOrder(BehaviourType, parameters).WithPriority(1000);

            context.movement.StartHovering();
            actions.constantHovering = true;

            agent.AIEvent.AddListener(HandleAIEvent);

            route = new CheckPointRoute(agent, parameters.checkPoints.ToArray());
            route.Begin(false);
        }

        public override void Leave()
        {
            context.checks.nearObject.threshold = 1f;

            //actions.constantHovering = false;
            //context.movement.StopHovering();

            if (escalationRoutine  != null) { agent.StopCoroutine(escalationRoutine);  escalationRoutine  = null; }
            if (checkPointRoutine  != null) { agent.StopCoroutine(checkPointRoutine);  checkPointRoutine  = null; }
            if (actionRoutine      != null) { agent.StopCoroutine(actionRoutine);      actionRoutine      = null; }
            if (sideActionsRoutine != null) { agent.StopCoroutine(sideActionsRoutine); sideActionsRoutine = null; }

            base.Leave();
        }
        #endregion

        public override void Execute()
        {
            base.Execute();

            if (opponent == null)
            {
                agent.CurrentOrder = null;
                Interrupt();
            }
        }

        #region phases and escalation
        private System.Collections.IEnumerator UpdateBattlePhases()
        {
            yield return null;

            SetCurrentEscalationStep(escalationLevel, false); // this will continue at where the battle was left.

            // iterate steps
            while (escalationLevel < parameters.escalationLevels.Length)
            {
                currentHealthThreshold = parameters.escalationLevels[escalationLevel].threshold;

                yield return new WaitUntil(IsNextEscalationReached);

                // next escalation

                if (context.myObjectInfo.health.IsMin())
                    break;

                SetCurrentEscalationStep(escalationLevel+1, true);

                if (context.behaviourInvalid)
                    break;
            }

            escalationRoutine = null;
        }

        private void SetCurrentEscalationStep(int index, bool makeFollowerAction)
        {
            escalationLevel = index;
            //Debug.Log("Next escalation step: " + index);
            EscalatoryStepData stepData = (parameters.escalationLevels != null && parameters.escalationLevels.Length > index) ? parameters.escalationLevels[escalationLevel] : parameters.stepDefaults;
            BossUtil.InitEscalatoryStep(agent, stepInfo, stepData, parameters.stepDefaults);
            
            if (makeFollowerAction)
            {
                if (stepInfo.initialCheckPoint != null)
                {
                    //Debug.Log("...go to main check point");

                    if (context.movement.IsMoving())
                    {
                        context.movement.AbortMovement();
                    }

                    if (checkPointRoutine != null)
                    {
                        agent.StopCoroutine(checkPointRoutine);
                    }

                    //currentPhase = BossBattlePhase.Wandering;
                    MakeCurrentCheckPoint((BossCheckPoint)stepInfo.initialCheckPoint);
                    checkPointRoutine = agent.StartCoroutine(UpdateCheckPointWandering(false));
                }
            }
        }
        #endregion

        #region check point wandering
        private void MakeCurrentCheckPoint(BossCheckPoint checkPoint)
        {
            currentCheckPoint = checkPoint;
            canWanderCheckPoints = true;

            FocusOnCheckPoint();
        }

        private void FocusOnCheckPoint()
        {
            context.MakeCurrentTarget(currentCheckPoint.gameObject);
            context.checks.UpdateTarget();
            context.targetInvalid = false;

            context.checks.nearObject.threshold = currentCheckPoint.radius;
        }

        private System.Collections.IEnumerator UpdateCheckPointWandering(bool initial)
        {
            yield return null;

            if (initial)
            {
                MakeCurrentCheckPoint((BossCheckPoint)route.DetermineNearest(false));
            }

            while (true)
            {
                yield return new WaitUntil(CanWanderCheckPoints);

                //Debug.Log("Now going to " + currentCheckPoint.gameObject.name);

                if (!context.IsCurrentTarget(currentCheckPoint.gameObject))
                {
                    FocusOnCheckPoint();
                }

                //currentPhase = BossBattlePhase.Wandering;
                StartMovement();
                context.movement.Move();

                yield return new WaitUntil(context.movement.HasStoppedMovement);

                if (!context.IsCurrentTarget(currentCheckPoint.gameObject))
                {
                    //Debug.Log("Current target is not checkpoint anymore: " + context.targetObjectInfo);
                    FocusOnCheckPoint();
                    continue;
                }

                if (!route.IsAt(currentCheckPoint))
                {
                    //Debug.Log("Not totally there yet: " + (currentCheckPoint.transform.position - agent.transform.position).magnitude + " /" + currentCheckPoint.radius);
                    continue; // try again to get there.
                }

                EndMovement();

                // now do actions
                BossCheckPointSettings settings = (currentCheckPoint is BossCheckPoint) ? ((BossCheckPoint)currentCheckPoint).bossSettings : defaultCheckPointSettings;
                ArriveAtCheckPoint(currentCheckPoint);

                // before action
                if (settings.modeBefore != BossActionType.None)
                {
                    if (ChooseAction(null, settings.modeBefore, settings.delayBefore.GetRandomValue()))
                        yield return new WaitUntil(IsActionOver);
                    else
                        yield return new WaitForSeconds(settings.delayBefore.GetRandomValue());

                    StopMainSkillAction();
                }

                // mid action
                ChooseAction(settings.skillToUse, settings.mode);
                yield return new WaitUntil(IsActionOver);
                StopMainSkillAction();

                // post afterwards
                if (settings.modeAfterwards != BossActionType.None)
                {
                    if (ChooseAction(null, settings.modeAfterwards, settings.delayAfterwards.GetRandomValue()))
                        yield return new WaitUntil(IsActionOver);
                    else
                        yield return new WaitForSeconds(settings.delayAfterwards.GetRandomValue());

                    StopMainSkillAction();
                }

                // wait small amount of time before going to next point
                yield return new WaitForSeconds(.1f);

                MakeCurrentCheckPoint((BossCheckPoint)route.DetermineNext(currentCheckPoint));

                if (context.behaviourInvalid)
                    break;
            }

            checkPointRoutine = null;
        }

        #endregion

        #region movement and side actions
        private void StartMovement()
        {
            //Debug.Log("Side actions start");

            actions.SkillUnrelatedFromMovement = true;

            if (sideActionsRoutine != null)
                agent.StopCoroutine(sideActionsRoutine);

            sideActionsRoutine = agent.StartCoroutine(UpdateSideActions());
        }

        /// <summary>
        /// Side skills.
        /// Endless, until stopped from outside.
        /// </summary>
        /// <returns></returns>
        private System.Collections.IEnumerator UpdateSideActions()
        {
            AITargetInfo secondaryTarget = new AITargetInfo().Analyze(opponent);
            bool closeMode = false;
            bool skillChangeNeeded = true;

            float lastTimeSkillChanged = Time.time - parameters.closeDistance - 1f; // make sure that the close mode check happens right away.

            while (true)
            {
                secondaryTarget.Update();

                if ((Time.time - lastTimeSkillChanged) > parameters.minSkillUseTime)
                {
                    if ((AITargetInfoUtil.DetermineDistance(context.myObjectInfo, secondaryTarget) < parameters.closeDistance) != closeMode)
                    {
                        closeMode = !closeMode;
                        skillChangeNeeded = true;

                        //Debug.Log("Close mode changed: " + closeMode);
                    }
                }

                if (skillChangeNeeded)
                {
                    if (closeMode)
                        SetCurrentSideSkill(stepInfo.closeEncounterSkill);
                    else
                        SetCurrentSideSkill(stepInfo.wanderSkill);

                    skillChangeNeeded = false;
                    lastTimeSkillChanged = Time.time;
                }

                if (actions.CurrentSkill != null)
                {
                    if (actions.CurrentSkill is IDefensiveSkill)
                    {
                        actions.TargetAngle = AITargetInfoUtil.DetermineAngleToTarget(context.myObjectInfo, secondaryTarget);
                    }
                    else if (actions.CurrentSkill is IAttackSkill)
                    {
                        secondaryTarget.AnticipateBounds(.5f, context.currentAccuracy);
                        actions.TargetAngle = AITargetInfoUtil.DetermineFutureAngleToTarget(context.myObjectInfo, secondaryTarget, context.skillProjectileSpeed, context.currentAccuracy);
                    }

                    if (!actions.IntentsToUseCurrentSkill())
                    {
                        actions.StartUsingCurrentSkill();
                    }
                }

                yield return new WaitForSeconds(.25f);
            }
        }

        private void EndMovement()
        {
            //Debug.Log("Side actions stop.");

            if (actions.IntentsToUseCurrentSkill())
            {
                actions.StopUsingCurrentSkill();
            }

            if (sideActionsRoutine != null)
            {
                agent.StopCoroutine(sideActionsRoutine);
                sideActionsRoutine = null;
            }

            actions.SkillUnrelatedFromMovement = false;
        }

        private void SetCurrentSideSkill(ActionSkill skill)
        {
            if (actions.CurrentSkill != null)
            {
                actions.StopUsingCurrentSkill();
            }

            actions.CurrentSkill = skill;

            if (skill is IAimingRequirer)
            {
                context.skillProjectileSpeed = ((IAimingRequirer)skill).GetTrajectoryData().speed;
            }

            //Debug.Log("Side skill changed to " + skill);
        }

        #endregion

        #region checkpoint action
        private void ArriveAtCheckPoint(CheckPoint currentCheckPoint)
        {
            //Debug.Log("Arrived at " + currentCheckPoint.name);

            context.checks.nearObject.threshold = 1f;

            if (!currentCheckPoint.flags.HasFlag(QPathNodeFlags.Grounded))
            {
                //Debug.Log("Shall hover");
                if (!context.movement.IsHovering())
                {
                    // this is unfortunately needed, probably because certain statuses can end hover mode (external influence)
                    context.movement.StartHovering();
                }
            }
            else
            {
                //Debug.Log("Shall stop hovering");
                //context.movement.StopHovering();
            }
        }

        private bool ChooseAction(SkillData skillData, BossActionType actionType, float maxDuration=0f)
        {
            actionOver = false;

            ActionSkill skill = null;
            GameObject target = null;
            Condition endCondition = null;

            if (skillData != null)
            {
                skill = context.skills.GetSkillByData(skillData);
            }

            if (skill == null)
            {
                switch (actionType)
                {
                    case BossActionType.Attack:
                        skill = stepInfo.attack;
                        break;

                    case BossActionType.Defend:
                        skill = stepInfo.defend;
                        break;

                    case BossActionType.SpecialAttack:
                        skill = stepInfo.specialAttack;
                        break;

                    case BossActionType.CallForHelp:
                        //skill = stepInfo.defend;
                        break;

                    case BossActionType.Escalate:
                        if (escalationLevel < parameters.escalationLevels.Length - 1)
                        {
                            SetCurrentEscalationStep(escalationLevel + 1, true);
                        }
                        //skill = stepInfo.defend;
                        break;

                    case BossActionType.Heal:
                    case BossActionType.Rest:
                        //skill = ResolveSkill(null, AIOptionTag.Support);
                        // check and remove bad statuses
                        // optherwise defend

                        if (agent.FixSkillSet != null)
                        {
                            if (context.awareness.HasProblematicStatus(StatusFlags.Burning))
                            {
                                //Debug.Log("Burns");
                                skill = agent.FixSkillSet.extinguish;
                                endCondition = new Condition_HasStatusFlag(context.awareness, StatusFlags.Burning, true);
                                maxDuration = 0f; // ignore max duration in this important case
                            }
                            else if(!context.myObjectInfo.health.IsMax())
                            {
                                //Debug.Log("Could heal");
                                skill = agent.FixSkillSet.heal;
                            }

                            if (skill != null)
                                target = agent.gameObject;
                        }

                        if (skill == null && maxDuration > 0f)
                            skill = stepInfo.defend;

                        break;

                    case BossActionType.FreeStyle:
                        context.BehaviourFSM.ChangeState(AIBehaviourType.Combat);
                        break;
                }
            }

            if (skill != null)
            {
                StartMainSkillAction(skill, target, endCondition, maxDuration);
                return true;
            }

            return false;
        }

        private void StartMainSkillAction(ActionSkill skill, GameObject targetObject, Condition endCondition = null, float maxDuration=0f)
        {
            actionOver = false;

            if (targetObject == null)
                targetObject = opponent;

            actions.CurrentSkill = skill;
            checks.UpdateSkill(skill);

            context.MakeCurrentTarget(targetObject);
            context.checks.UpdateTarget();
            context.targetObjectInfo.AnticipateBounds();

            if (skill is IAttackSkill)
            {
                context.ChangeSubAction(AIActionType.Attack, GenParameters());
            }
            else if(skill is IDefensiveSkill)
            {
                context.ChangeSubAction(AIActionType.Defend, GenParameters());
            }
            else if (skill != null)
            {
                context.ChangeSubAction(skill);
            }

            actionRoutine = agent.StartCoroutine(UpdateCurrentAction(endCondition, maxDuration));
            context.actionInvalid = false;
        }

        private AIActionParameters GenParameters()
        {
            AIActionParameters parameters = new AIActionParameters();
            if (currentCheckPoint != null)
            {
                parameters.duration = currentCheckPoint.bossSettings.duration;
                parameters.amount = currentCheckPoint.bossSettings.amount;
            }
            return parameters;
        }

        private void StopMainSkillAction()
        {
            //Debug.Log("stop main skill action");
            context.LeaveCurrentSubAction();
            actions.StopUsingCurrentSkill();
            actions.CurrentSkill = null;
            actionOver = true;

            if (actionRoutine != null)
            {
                agent.StopCoroutine(actionRoutine);
                actionRoutine = null;
            }

            context.actionInvalid = false;
        }

        private System.Collections.IEnumerator UpdateCurrentAction(Condition endCondition, float maxDuration)
        {
            yield return null;

            if (endCondition != null)
            {
                yield return new WaitUntil(endCondition.IsMet);
                StopMainSkillAction();
            }
            else if (maxDuration > 0f)
            {
                yield return new WaitForSeconds(maxDuration);
                StopMainSkillAction();
            }
            actionRoutine = null;
        }
        #endregion

        #region checks
        private bool IsActionOver() => actionOver || context.actionInvalid;

        private bool IsNextEscalationReached()
        {
            if (healthInvalid)
            {
                healthInvalid = false;
                if (agent == null || agent.context.myObjectInfo.health.InPercent < currentHealthThreshold)
                    return true;

                return false;
            }

            return false;
        }

        private bool CanWanderCheckPoints() => canWanderCheckPoints;
        #endregion

        #region events
        public override void OnAgentDeactivated()
        {
            base.OnAgentDeactivated();
            //Debug.Log("OnAgentDeactivated");
            agent.ChangeBehaviour(AIBehaviourType.Empty);
            context.behaviourInvalid = true;
        }

        private void HandleAIEvent(AIEventType type)
        {
            switch(type)
            {
                case AIEventType.ReceivedDamage:
                    healthInvalid = true;
                    break;
            }
        }
        #endregion
    }
}
