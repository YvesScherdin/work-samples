using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.States;
using MageGame.AI.Validation;
using UnityEngine;

namespace MageGame.AI.Agents.Default
{
    public class AIAS_Follow : AIActionState
    {
        public override AIActionType ActionType => AIActionType.Follow;

        private AIFollowerParameters followerParameters;

        private Condition_NearObject nearObject;
        private Condition_FreeSkillPath skillPathFree;
        private AITargetInfo targetToFollow;
        private bool following;

        public override void SetParameters(AIActionParameters parameters)
        {
            base.SetParameters(parameters);

            if (parameters is AIFollowerParameters)
                followerParameters = (AIFollowerParameters)parameters;
        }

        public override void Initialize()
        {
            base.Initialize();

            skillPathFree = checks.skillPathFree;
            nearObject = checks.nearObject;
        }

        public override void Enter()
        {
            base.Enter();

            targetToFollow = new AITargetInfo();
            targetToFollow.Analyze(followerParameters.whom);
            targetToFollow.note = AIHandlingNote.Follow;
            targetToFollow.priority = 300;
            context.awareness.NoticeSafe(targetToFollow.gameObject);

            if (targetToFollow.IsDefeated())
            {
                context.targetInvalid = true;

                if (agent.CurrentOrder != null && agent.CurrentOrder.behaviourType == AIBehaviourType.Follow)
                    agent.CurrentOrder = null;
            }

            FocusTargetToFollow();

            following = false;

            executionCoroutine = agent.StartCoroutine(ExecuteCoroutine());
        }

        private System.Collections.IEnumerator ExecuteCoroutine()
        {
            yield return null;

            while (true)
            {
                // idle

                while (nearObject.analysis.distance <= followerParameters.range.max)
                {
                    yield return new WaitForSeconds(.1f);
                }
                
                // start to move
                context.movement.Move();
                following = true;

                while (nearObject.analysis.distance > followerParameters.range.min)
                {
                    if (!context.movement.IsMoving())
                    {
                        context.movement.Move();
                    }
                    yield return new WaitForSeconds(.1f);
                }

                // Thou' shallst stoppeth!
                context.movement.AbortMovement();
                following = false;
            }
        }

        public override void Execute()
        {
            base.Execute();

            context.targetObjectInfo.AnticipateBounds(.1f, 1f);

            if (context.HasTarget())
            {
                if (!following)
                {
                    nearObject.Check();
                    checks.CheckTargetPosition();
                }
                else
                {
                    // nearObject.Check(); is called automatically by movement handler
                    context.moveParams.hurry = Mathf.Abs(nearObject.analysis.distance) >= followerParameters.hurryThreshold;
                }
            }
            else
            {
                context.targetInvalid = true;
            }

            if (context.targetInvalid)
            {
                if (targetToFollow.IsDefeated())
                {
                    Interrupt();
                    return;
                }

                if (!context.HasTarget())
                {
                    Interrupt();
                    return;
                }
                else
                {
                    if (context.targetObjectInfo.gameObject != targetToFollow.gameObject)
                    {
                        // target changed
                        Interrupt();
                        return;
                    }
                }
            }
        }

        private void FocusTargetToFollow()
        {
            context.targetLocked = true;
            context.MakeCurrentTarget(targetToFollow);
            context.awareness.primaryTarget = targetToFollow.gameObject;

            skillPathFree.targetInfo = targetToFollow;
            nearObject.target = targetToFollow;
        }
        
        public override void Exit()
        {
            context.movement.AbortMovement();
            base.Exit();
        }
    }
}
