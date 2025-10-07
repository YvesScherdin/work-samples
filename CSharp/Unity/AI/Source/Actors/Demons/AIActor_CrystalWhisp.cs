using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.States.Simple;
using MageGame.AI.Validation;
using MageGame.AI.World;
using MageGame.Behaviours.Ability;
using MageGame.Behaviours.Combat;
using MageGame.Behaviours.Relationships;
using MageGame.Entities.Local.Filters;
using MageGame.Geometry.Utils;
using MageGame.Graphics.Models.Behaviours;
using MageGame.Graphics.Particles.Utils;
using MageGame.Skills.Simple;
using System.Collections;
using UnityEngine;

namespace MageGame.AI.Actors.Demons
{
    public class AIActor_CrystalWhisp : MonoBehaviour, IConvenienceAIContext
    {
        #region internal classes
        [System.Serializable]
        public class CrystalWhispActions
        {
            public float attackConePathAngle = 60;
            public float attackConePathSpeed = 15;

            public ToolActuator toolActuator;
            public SimpleSkill_Cone leyConeSkill;
        }
        #endregion

        #region configuration
        [Header("View")]
        public Animator animator;
        public ModelComponent model;
        public ParticleSystem aggressionParticles;

        [Header("Sub components")]
        public Vulnerability vunerability;
        public IdentificationFriendOrFoe IFF;

        [Header("AI")]
        public AIInterestZone interestZoneClose;
        public CrystalWhispActions actions;
        #endregion

        // internal members
        internal SimpleAIStateMachine stm;
        internal SimpleAIAnalyzer aiAnalyzer;
        internal SimpleSkill currentSkill;
        internal float targetAngle; // in degrees

        public MonoBehaviour CoroutineRegistry => this;
        public AITargetInfo CurrentTarget => aiAnalyzer.CurrentTarget;
        public Animator Animator => animator;

        #region de-/init
        private void Awake()
        {
            stm = new SimpleAIStateMachine(this, new ISimpleAIActionState[] {
                new CrystalWhispAIState_Idle(),
                new CrystalWhispAIState_Attack(),
                new CrystalWhispAIState_MakeDeathMove()
            });
        }

        private void Start()
        {
            WorldSceneAIManager.AddAIActor(this);

            vunerability.RelatedEvent.AddListener(HandleVulnerabilityEvent);
            interestZoneClose.ObjectChangeEvent.AddListener(HandleInterestZoneChange);

            aiAnalyzer = new SimpleAIAnalyzer();
            aiAnalyzer.Init(gameObject);
            aiAnalyzer.TargetChangedEvent.AddListener(HandleNewTarget);
            aiAnalyzer.Myself.targettingOriginTRF = actions.toolActuator.transform;

            EffectTargetFilter targetFilter = new EffectTargetFilter(gameObject, IFF);
            actions.leyConeSkill.Configure(targetFilter, gameObject);

            currentSkill = actions.leyConeSkill;

            stm.ChangeState(AIActionType.Idle);
        }
        #endregion

        #region ai
        public bool IsActive() => gameObject.activeSelf;

        public void UpdateAI(float timeDelta) { }

        private abstract class CrystalWhispAIState : BasicConvenienceAIActionState<AIActor_CrystalWhisp>
        {
            public override void Leave()
            {
                if (context.currentSkill != null && context.currentSkill.IsActive())
                {
                    context.currentSkill.Abort();
                }

                base.Leave();
            }

            protected SimpleAIAnalyzer Analyzer => context.aiAnalyzer;
        }

        private class CrystalWhispAIState_Idle : CrystalWhispAIState
        {
            public override AIActionType GetStateKey() => AIActionType.Idle;
        }
        
        private class CrystalWhispAIState_Attack : CrystalWhispAIState
        {
            public override AIActionType GetStateKey() => AIActionType.Attack;

            private float endAngle;
            private SimpleSkill_Cone skill;

            private bool odd;
            private float lastTargetAngle;

            public override void Initialize()
            {
                base.Initialize();

                this.skill = context.actions.leyConeSkill;
            }

            public override void Enter()
            {
                base.Enter();
                
                ParticleSystemUtil.SetEmissionEnabled(context.aggressionParticles, true);
                ScanTargetAngle();
            }

            private void ScanTargetAngle()
            {
                if (context.CurrentTarget.gameObject != null)
                    lastTargetAngle = GeomUtil.GetAngleBetweenGOs(context.gameObject, context.CurrentTarget.gameObject);
                else
                    lastTargetAngle = 0;
            }

            private void DetermineAttackParameters()
            {
                odd = !odd;
                float targetAngle = lastTargetAngle;
                float sideMultiplier = odd ? 1 : -1;

                endAngle = targetAngle - sideMultiplier * context.actions.attackConePathAngle;

                context.targetAngle = targetAngle + sideMultiplier * context.actions.attackConePathAngle;
                context.actions.toolActuator.targetAngleOffset = context.targetAngle;
            }

            protected override IEnumerator UpdateState()
            {
                yield return null;

                yield return new WaitForSeconds(.5f);

                ScanTargetAngle();
                DetermineAttackParameters();

                skill.Activate();

                while (true)
                {
                    context.targetAngle = Mathf.MoveTowardsAngle(
                        context.targetAngle, endAngle, context.actions.attackConePathSpeed * Time.deltaTime);

                    context.actions.toolActuator.targetAngleOffset = context.targetAngle;

                    yield return new WaitForEndOfFrame();

                    if (context.targetAngle == endAngle)
                    {
                        yield return new WaitForSeconds(.5f);
                        break;
                    }
                }

                skill.Deactivate();

                if (context.CurrentTarget == null || context.CurrentTarget.IsDefeated())
                {
                    context.stm.GotoDefault();
                }
                else
                {
                    //Debug.Log("Re-enter");
                    stateCoroutine = context.CoroutineRegistry.StartCoroutine(UpdateState());
                }
            }

            public override void Leave()
            {
                base.Leave();

                if (skill.IsActive())
                    skill.Deactivate();

                ParticleSystemUtil.SetEmissionEnabled(context.aggressionParticles, false);
            }
        }

        private class CrystalWhispAIState_MakeDeathMove : CrystalWhispAIState
        {
            public override AIActionType GetStateKey() => AIActionType.MakeDeathMove;

            public override void Enter()
            {
                base.Enter();
            }

            public override void Leave()
            {
                base.Leave();
            }
        }

        #endregion

        #region events

        private void OnDestroy()
        {
            WorldSceneAIManager.RemoveAIActor(this);
        }

        private void OnEnable()
        {
            interestZoneClose.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            interestZoneClose.gameObject.SetActive(false);
        }

        private void HandleInterestZoneChange(GameObject target, AIInterestZone.AIInterestZoneID zoneID, bool within)
        {
            aiAnalyzer.NotifyPossibleTarget(target, zoneID, within);
        }

        private void HandleNewTarget(AITargetInfo target)
        {
            if (target != null)
                this.stm.ChangeState(AIActionType.Attack);
            else
                this.stm.ChangeState(AIActionType.Idle);
        }

        private void HandleVulnerabilityEvent(VulnerabilityEventType type)
        {
            switch (type)
            {
                case VulnerabilityEventType.Died:
                    stm.ChangeState(AIActionType.MakeDeathMove);
                    break;
            }
        }
        #endregion
    }
}