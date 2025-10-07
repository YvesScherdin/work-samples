using MageGame.Actions.Data;
using MageGame.AI.Data;
using MageGame.AI.States.Movement;
using MageGame.AI.Validation;
using MageGame.AI.World;
using MageGame.Behaviours.Ability;
using MageGame.Geometry.Utils;
using MageGame.Movement.Locomotion;
using MageGame.Movement.Locomotion.Behaviours;
using UnityEngine;
using static MageGame.AI.States.Movement.AIMovementStrategy;

namespace MageGame.AI.Core
{
    /// <summary>
    /// Offers checks for own movement.
    /// </summary>
    public class AIMovement
    {
        private const float stuckThresholdByTimeDefault = 0.005f;
        private const float stuckThresholdByTimeBig = .5f;
        private const float stuckThresholdByAcceleration = 10f;
        private const float stuckThresholdByDistance = .01f;
        private const float bypassObstacleUpDuration = .4f;
        private const float bypassObstacleSideDuration = 1.0f;

        private AIAgentContext context;
        private AIMovementHandler movementHandler;

        private CharacterMovement charMovement;
        private CharacterController2D charControl;
        private ActionController actions;
        private Swimmer swimmer;
        private Climber climber;

        private MovementAnalysis movementAnalysis;
        public MovementAnalysis MovementAnalysis => movementAnalysis;

        private IEnhancedAviaticLocomotion levitation;
        public ILocomotionSkill CurrentLocomotion => actions.locomotion;

        public AIMovement(AIAgentContext context)
        {
            this.context = context;
            this.charMovement = context.agent.GetComponent<CharacterMovement>();
            this.charControl = context.agent.GetComponent<CharacterController2D>();
            this.actions = context.agent.GetComponent<ActionController>();
            this.swimmer = context.agent.GetComponent<Swimmer>();
            this.climber = context.agent.GetComponent<Climber>();

            movementAnalysis = new MovementAnalysis();
            movementAnalysis.objectTransform = context.gameObject.transform;

            levitation = context.agent.GetComponent<IEnhancedAviaticLocomotion>();
        }

        #region checks
        public bool IsStanding() => actions.dirX == 0 && actions.dirY != 1;

        public bool IsHovering()
        {
            return actions.shallHover;
        }

        public bool IsTryingToWalkUpTooSteepSlope()
        {
            return charMovement.IsTryingToWalkUpTooSteepSlope();
        }

        internal void PostAnalyzeReachability(ReachabilityAnalysis analysis)
        {
            if (actions.dirY > 0 && !analysis.differentLevels)
            {
                if (analysis.distanceVector.y > -1f)
                {
                    analysis.differentLevels = true;
                    analysis.reached = false;
                    analysis.reachedY = false;
                    analysis.status = RangeStatus.TooFar;
                }
            }
        }

        public bool HasNoRelevantOrders(ReachabilityAnalysis analysis)
        {
            if (actions.dirX == 0 && actions.dirY == 0)
            {
                if (analysis.status == RangeStatus.TooFar)
                {
                    if ((Time.time - movementAnalysis.timeTrackingStarted) > stuckThresholdByTimeBig)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsStuck()
        {
            return IsStuck(stuckThresholdByTimeDefault);
        }
        
        public bool IsStuck(float stuckThreshold)
        {
            if (context.movement.IsClimbing())
            {
                if ((Time.time - movementAnalysis.timeTrackingStarted) > stuckThresholdByTimeBig)
                {
                    if (actions.dirY > 0)
                    {
                        if (charControl.collisionState.above)
                            return true;
                    }
                    else if (actions.dirY < 0)
                    {
                        if (charControl.collisionState.below)
                            return true;
                    }
                }

                if (climber.animationDriven)
                    return false;
            }

            if (actions.dirX == 0)
            {
                if (actions.dirY == 0)
                    return false;

                if (IsClimbing() || IsSwimming() || IsLevitating())
                    return false;

                if (movementAnalysis.GetAveragePositionDeltaValue() < stuckThresholdByDistance)
                {
                    if ((Time.time - movementAnalysis.timeTrackingStarted) > stuckThresholdByTimeBig)
                    {
                        Debug.Log("stuck while trying to move " + (actions.dirY < 0f ? "downwards" : "upwards"));
                        return true;
                    }
                }
                
                return false;
            }

            if ((movementAnalysis.distanceMoved * movementAnalysis.moveSpeedScale) < stuckThreshold)
            {
                if ((Time.time - movementAnalysis.timeTrackingStarted) > stuckThresholdByTimeBig)
                {
                    if (actions.locomotion != null && actions.locomotion.GetCurrentAcceleration().magnitude < stuckThresholdByAcceleration)
                    {
                        // handle small acceleration case
                        return false; // give more time to accelerate
                    }
                    return true;
                }
            }

            return false;
        }

        public bool IsSwimming()
        {
            return swimmer != null && swimmer.IsInUse();
        }
        
        public bool IsClimbing()
        {
            return climber != null && climber.IsInUse();
        }

        public bool CanLevitate()
        {
            return levitation != null;
        }

        public bool IsLevitating()
        {
            return levitation != null && levitation.IsInUse();
        }

        public bool HitsObstacleHorizontally()
        {
            if (actions.dirX < 0)
                return charControl.collisionState.left;
            else if (actions.dirX > 0)
                return charControl.collisionState.right;
            else
                return false;
        }

        public bool CouldEverReachTargetDirectly(bool targetStatic, PathAnalysis path, ReachabilityAnalysis reachability)
        {
            float threshold = .5f;

            if (Mathf.Abs(reachability.distanceVector.x) < 1f)
            {
                if (reachability.distanceVector.y < -threshold)
                {
                    if (context.charControl.IsGroundedWithTolerance(.5f) && !context.charControl.CanJumpDownFrom())
                        return false;
                }
                else if (reachability.distanceVector.y > threshold)
                {
                    if (!path.freePath && path.obstacleType == AIObstacleType.Static)
                        return false;
                    else if (reachability.distanceVector.y > 3f)
                    {
                        if (!IsSwimming() && !IsClimbing() && !CanLevitate())
                            return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region actions

        public void Move(bool freeStyle=true, bool closeContactNeeded=true)
        {
            if (movementHandler == null)
            {
                movementHandler = WorldSceneAIManager.Instance.Movements.RegisterAgent(context.agent);
            }

            movementHandler.failed = false;
            movementHandler.closeContactNeeded = closeContactNeeded;
            movementHandler.targetType = MovementTargetType.Unspecified;

            // TODO: exact movement instruction
            if (freeStyle)
                movementHandler.Approach();
            else
                movementHandler.MoveAlongPath();

            WorldSceneAIManager.Instance.Movements.Activate(movementHandler);
        }


        public void Move(MovementTargetType targetType, bool freeStyle=true)
        {
            if (movementHandler == null)
            {
                movementHandler = WorldSceneAIManager.Instance.Movements.RegisterAgent(context.agent);
            }

            movementHandler.failed = false;
            movementHandler.closeContactNeeded = true;
            movementHandler.targetType = targetType;

            if (freeStyle)
                movementHandler.Approach();
            else
                movementHandler.MoveAlongPath();

            WorldSceneAIManager.Instance.Movements.Activate(movementHandler);
        }

        public bool IsGrounded() => charControl.IsGroundedWithTolerance(.1f);
        public bool IsMoving() => movementHandler != null && movementHandler.needed;
        public AIMovementStrategyType MovementStrategyType => movementHandler != null ? movementHandler.CurrentStrategyType : AIMovementStrategyType.Unspecified;

        public void AbortMovement()
        {
            if (movementHandler != null)
                movementHandler.Interrupt();
        }

        public bool HasStoppedMovement()
        {
            if (actions.blockedMask.HasFlag(ActionBlockMaskType.SceneTransition))
                return false;

            return movementHandler == null || (!movementHandler.needed && !movementHandler.failed);
        }

        internal void StandStill()
        {
            actions.dirX = 0;
            actions.dirY = 0;
        }

        internal void LookIntoOppositeDirection()
        {
            charMovement.FacingLeft = !charMovement.FacingLeft;
            actions.targetSide = (sbyte)(charMovement.FacingLeft ? -1 : 1);
        }
        
        internal void LookIntoTargetsDirection()
        {
            if (context.targetObjectInfo.gameObject != null)
            {
                actions.targetSide = GeomUtil.GetPointingSide(context.myObjectInfo.Position, context.targetObjectInfo.Position);
                charMovement.PointToSide(actions.targetSide);
            }
            else
                Debug.LogWarning("Cannot look into direction of nothing.");
        }

        internal sbyte GetFacingSide()
        {
            return charMovement.GetFacingDir();
        }

        internal void PointTo(int targetSide)
        {
            charMovement.PointToSide(targetSide);
        }

        internal void PointTo(Vector3 targetPosition)
        {
            charMovement.actions.targetSide = GeomUtil.GetPointingSide(context.myObjectInfo.Position, targetPosition);
        }

        public void StartHovering()
        {
            actions.shallHover = true;
        }
        
        public void StopHovering()
        {
            actions.shallHover = false;
        }

        public void StartTracking()
        {
            movementAnalysis.StartTracking();
            movementAnalysis.moveSpeedScale = CalculateSpeedMultiplier(charMovement.GetDampingModifier());
        }

        internal void UpdateTracking()
        {
            movementAnalysis.UpdateTracking();
            movementAnalysis.moveSpeedScale = CalculateSpeedMultiplier(charMovement.GetDampingModifier());
        }

        internal void StopTracking()
        {
            movementAnalysis.StopTracking();
        }

        static private float CalculateSpeedMultiplier(float basicMlutiplier)
        {
            if (basicMlutiplier == 0f)
                return 0f;

            return 1f / basicMlutiplier;
        }

        public bool TryBypassObstacle()
        {
            bool result = false;

            if (charControl.collisionState.above || actions.dirY == 1)
            {
                sbyte side = context.DetermineSideToTarget();

                if (side == 0)
                {
                    // rare case...
                    if (charControl.collisionState.left)
                        side = -1;
                    else if (charControl.collisionState.right)
                        side = 1;
                }

                if (side != 0)
                {
                    context.actions.TryToGetSidewards(side, bypassObstacleSideDuration);
                    result = true;
                }
                
            }
            else if (IsTryingToWalkUpTooSteepSlope() || HitsObstacleHorizontally())
            {
                context.actions.TryToGetUp(bypassObstacleUpDuration);
                result = true;
            }

            if (result)
                StartTracking();

            return result;
        }

        #endregion
    }
}