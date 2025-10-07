using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.States.Simple;
using MageGame.AI.Validation;
using MageGame.AI.World;
using MageGame.Animation.Core.Data;
using MageGame.Behaviours.Ability;
using MageGame.Behaviours.Combat;
using MageGame.Behaviours.Relationships;
using MageGame.Common.Utils;
using MageGame.Entities.Local.Data;
using MageGame.Entities.Local.Filters;
using MageGame.Geometry.Utils;
using MageGame.Graphics.Models.Behaviours;
using MageGame.Graphics.Particles.Utils;
using MageGame.Graphics.SpriteShapes.Behaviours;
using MageGame.Movement.Behaviours;
using MageGame.Movement.Locomotion.Behaviours;
using MageGame.Skills.Simple;
using MageGame.World;
using MageGame.World.Scenes;
using MageGame.WorldPhysics.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using static MageGame.AI.States.Simple.AIInterestZone;

namespace MageGame.AI.Actors.Demons
{
    public class AIActor_LeyDjinn : MonoBehaviour, ISimpleAIContext
    {
        #region internal classes
        [System.Serializable]
        public class LeyDjinnSounds
        {
            public AudioClip alert;
            public AudioClip death;
            public AudioClip groan;
        }

        [System.Serializable]
        public class LeyDjinnActions
        {
            public SimpleSkill_Beam frontHandBeam;
            public SimpleSkill_Beam backHandBeam;
            public SimpleSkill_Cone mouthCone;
            public SimpleSkill_Summon summoning;
            public SimpleSkill_Explosion explosion;

            public AttackMode attackModeBeam;
            public AttackMode attackModeCone;

            [Range(.01f, 5f)]
            public float fadeOutOnDeathDuration = 1f;
        }

        #endregion

        #region configuration
        [Header("View-related")]
        public LeyDjinnSounds sounds;
        public Animator animator;
        public ModelComponent model;
        public SpriteShapeSkeleton skeleton;
        public GameObject face;
        public TurnableModel turnable;
        public ParticleSystem heavySmokeOnHead;
        public GameObject deadSpawnHeadTemplate;
        public GameObject deadSpawnHandTemplate;
        public GameObject deadSpawnSpineTemplate;

        [Header("Sub components")]
        public ToolActuator head;
        public ToolActuator handBack;
        public ToolActuator handFront;
        public SpriteShapeRenderer bodyRenderer;
        public ParticleSystem[] headSmoke;

        public Vulnerability vunerability;
        public Transform targettingOrigin;
        public SimpleCharacterMovement movement;
        public IdentificationFriendOrFoe IFF;

        [Header("AI")]
        public LeyDjinnActions actions;
        public SkillOptionGroup skillOptionsClose;
        public SkillOptionGroup skillOptionsMid;
        public SkillOptionGroup skillOptionsFar;
        public AIInterestZone interestZoneClose;
        public AIInterestZone interestZoneMid;
        public AIInterestZone interestZoneFar;

        #endregion

        // internal members
        internal SimpleAIStateMachine stm;
        internal SimpleAIAnalyzer aiAnalyzer;
        internal SimpleSkill currentSkill;

        internal float targetAngle; // in degrees
        internal sbyte targetSide;

        public MonoBehaviour CoroutineRegistry => this;

        public AITargetInfo CurrentTarget => aiAnalyzer.CurrentTarget;

        public bool FuriousMode
        {
            set
            {
                if (value)
                {
                    ParticleSystemUtil.SetEmissionEnabled(heavySmokeOnHead, true);
                    heavySmokeOnHead.Play();
                }
                else
                {
                    heavySmokeOnHead.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ParticleSystemUtil.SetEmissionEnabled(heavySmokeOnHead, false);
                }
            }
        }

        #region de-/init
        private void Awake()
        {
            stm = new SimpleAIStateMachine(this, new ISimpleAIActionState[] {
                 new LeyDjinnAIState_Idle()
                ,new LeyDjinnAIState_FireBeam()
                ,new LeyDjinnAIState_FireCone()
                ,new LeyDjinnAIState_Summon()
                ,new LeyDjinnAIState_Explosion()
                ,new LeyDjinnAIState_Aura()
                ,new LeyDjinnAIState_Dead()
            });
            
            targetSide = movement.GetFacingDir();
        }

        private void Start()
        {
            WorldSceneAIManager.AddAIActor(this);

            vunerability.RelatedEvent.AddListener(HandleVulnerabilityEvent);
            interestZoneClose.ObjectChangeEvent.AddListener(HandleInterestZoneChange);
            interestZoneMid.ObjectChangeEvent.AddListener(HandleInterestZoneChange);
            interestZoneFar.ObjectChangeEvent.AddListener(HandleInterestZoneChange);

            aiAnalyzer = new SimpleAIAnalyzer();
            aiAnalyzer.Init(gameObject);
            aiAnalyzer.TargetChangedEvent.AddListener(HandleNewTarget);
            aiAnalyzer.Myself.targettingOriginTRF = targettingOrigin;

            animator.ResetTrigger(AnimationParamID.PhaseReady);
            model.AnimationOverEvent.AddListener(HandleAnimationOver);

            EffectTargetFilter targetFilter = new EffectTargetFilter(gameObject, IFF);
            targetFilter.moreExcluded = new List<GameObject> { face };

            actions.backHandBeam.beam.Configure(IFF, gameObject);
            actions.frontHandBeam.beam.Configure(IFF, gameObject);
            actions.mouthCone.Configure(targetFilter, gameObject);
            actions.mouthCone.cone.objectsToIgnore.Add(head.gameObject);
            actions.explosion.targetFilter = targetFilter;

            actions.summoning.hitTargetFilter = targetFilter;
            actions.backHandBeam.beam.Ignore(face);
            actions.frontHandBeam.beam.Ignore(face);

            FuriousMode = false;

            stm.ChangeState(AIActionType.Idle);
        }
        #endregion

        #region ai
        public bool IsActive() => gameObject.activeSelf;
        public void UpdateAI(float timeDelta) { }

        public void ChooseAction(SimpleSkillOption skillOption)
        {
            if (skillOption == null)
            {
                Debug.LogWarning("No skill option chosen");
            }
            else if (skillOption.skill == null)
            {
                Debug.LogWarning("Skill option is lacking skill");
            }
            else
            {
                currentSkill = skillOption.skill;
                stm.ChangeState(skillOption.skill.ActionType);
                skillOption.lastTimeUsed = Time.time;
            }
        }

        private abstract class LeyDjinnAIState : SimpleAIActionState<AIActor_LeyDjinn>
        {
            virtual public string MainAnimationParamID => null;

            protected Coroutine stateCoroutine;

            public override void Enter()
            {
                if (MainAnimationParamID != null && context.animator != null)
                    context.animator.SetBool(MainAnimationParamID, true);

                stateCoroutine = context.StartCoroutine(UpdateState());
            }

            public override void Leave()
            {
                if (MainAnimationParamID != null && context.animator != null)
                {
                    context.animator.SetBool(MainAnimationParamID, false);
                }

                if (context.currentSkill != null && context.currentSkill.IsActive())
                {
                    context.currentSkill.Abort();
                }

                if (stateCoroutine != null && context != null)
                {
                    context.StopCoroutine(stateCoroutine);
                    stateCoroutine = null;
                }
            }

            virtual protected System.Collections.IEnumerator UpdateState()
            {
                yield return null;
            }

            protected SimpleAIAnalyzer Analyzer => context.aiAnalyzer;
        }

        private class LeyDjinnAIState_Dead : LeyDjinnAIState
        {
            public override AIActionType GetStateKey() => AIActionType.MakeDeathMove;

            protected override IEnumerator UpdateState()
            {
                float speed = 1 / context.actions.fadeOutOnDeathDuration;
                float state = context.bodyRenderer.color.a;

                while (true)
                {
                    float time = Time.time;
                    yield return new WaitForEndOfFrame();
                    time = Time.time - time;

                    state = Mathf.MoveTowards(state, 0f, time * speed);

                    context.bodyRenderer.color = context.bodyRenderer.color.WithAlpha(state);

                    if (state == 0f)
                        break;
                }

                yield return new WaitForSeconds(.5f);

                Destroy(context.gameObject);

            }
        }

        private class LeyDjinnAIState_Idle : LeyDjinnAIState
        {
            public override AIActionType GetStateKey() => AIActionType.Idle;

            protected override IEnumerator UpdateState()
            {
                yield return null;

                yield return new WaitForSeconds(1f);

                while (true)
                {
                    if(context.aiAnalyzer.CurrentTarget != null)
                    {
                        Analyzer.Myself.Update();
                        Analyzer.CurrentTarget.Update();
                        Analyzer.CurrentTarget.AnticipateBounds();

                        context.targetSide = AITargetInfoUtil.DetermineSideToTarget(Analyzer.Myself, Analyzer.CurrentTarget);

                        if (context.targetSide != context.movement.GetFacingDir())
                        {
                            yield return context.turnable.Turn();
                        }

                        if (context.aiAnalyzer.CurrentTarget == null)
                        {
                            continue;
                        }

                        if (context.CurrentTarget.zone.HasFlag(AIInterestZoneID.Close))
                        {
                            // summon or explosion
                            context.ChooseAction(context.skillOptionsClose.ChooseRandomOption());
                        }
                        else if(context.CurrentTarget.zone.HasFlag(AIInterestZoneID.Mid))
                        {
                            // beam or cone or shield
                            context.ChooseAction(context.skillOptionsMid.ChooseRandomOption());
                        }
                        else if (context.CurrentTarget.zone.HasFlag(AIInterestZoneID.Far))
                        {
                            // beam or projectile or shield
                            context.ChooseAction(context.skillOptionsFar.ChooseRandomOption());
                        }
                    }

                    yield return new WaitForSeconds(.5f);
                }
            }
        }
        
        private abstract class LeyDjinnAIState_DirectedAttack : LeyDjinnAIState
        {
            protected VelocityUpater velocityUpdater = new VelocityUpater();
            protected AttackRoute route = new AttackRoute();
            protected AttackMode mode;

            protected float timeBeforeActivate = .5f;
            protected float timeStillAfterActivate = .5f;

            protected void ChooseDirectionalMode(AttackMode mode)
            {
                this.mode = mode;
                velocityUpdater.acceleration = mode.acceleration;
                velocityUpdater.deceleration = mode.acceleration;
                velocityUpdater.maxSpeed = mode.maxSpeed;
                velocityUpdater.currentSpeed = 0f;
            }

            public override void Enter()
            {
                context.movement.SetMaxTargetDirections(7);

                Analyzer.Myself.Update();
                Analyzer.CurrentTarget.Update();
                Analyzer.CurrentTarget.AnticipateBounds();

                context.targetAngle = AITargetInfoUtil.DetermineAngleToTarget(Analyzer.Myself, Analyzer.CurrentTarget);

                context.FuriousMode = true;

                if (AngleUtil.IsLeft(context.targetAngle))
                {
                    if (AngleUtil.IsTop(context.targetAngle))
                        MakeRoute(90f, 210f);
                    else
                        MakeRoute(-90f, -210f);
                }
                else // AngleUtil.IsRight
                {
                    if (AngleUtil.IsTop(context.targetAngle))
                        MakeRoute(90f, -30f);
                    else
                        MakeRoute(-90f, 30f);
                }

                bool horizontal = AngleUtil.IsRatherHorizontal(context.targetAngle);

                // route starts with vertical target (top or down) by default
                if (horizontal)
                {
                    // target is rather on the horizontal, let's not start at the vertical, but at the horizontal and move to the vertical
                    route.Swap();
                }

                context.targetAngle = horizontal ? (AngleUtil.IsLeft(context.targetAngle)
                    ? 180f * (AngleUtil.IsTop(context.targetAngle) ? 1f : -1f)
                    : 0f) : route.start;

                context.movement.PointIntoDirection(context.targetAngle, 7, false);
                context.animator.ResetTrigger(AnimationParamID.PhaseReady);
                context.animator.SetTrigger(AnimationParamID.Started);

                base.Enter();
            }

            private AttackRoute MakeRoute(float start, float end)
            {
                route.start = start;
                route.end = end;
                return route;
            }

            protected override IEnumerator UpdateState()
            {
                yield return null;

                if (context.targetSide != context.movement.GetFacingDir())
                {
                    // safety check (side)
                    context.movement.FacingLeft = context.targetSide < 0;
                }

                if (timeBeforeActivate > 0f)
                    yield return new WaitForSeconds(timeBeforeActivate);

                StartAction();

                if (timeStillAfterActivate > 0f)
                    yield return new WaitForSeconds(timeStillAfterActivate);

                int amount = 2;

                while (true)
                {
                    context.targetAngle = velocityUpdater.Approach(context.targetAngle, route.end, Time.deltaTime);
                    context.movement.PointIntoDirection(context.targetAngle, 7, false);

                    if (context.targetAngle == route.end)
                    {
                        amount--;

                        if (amount < 1)
                        {
                            yield return new WaitForSeconds(.25f);
                            StopAction();
                            yield return new WaitForSeconds(.1f);
                            context.stm.GotoDefault();
                            break;
                        }
                        else
                        {
                            route.Swap();
                            yield return new WaitForSeconds(.25f);
                        }
                    }
                    else
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
            }

            public override void Leave()
            {
                base.Leave();
                StopAction();
            }

            abstract protected void StartAction();
            abstract protected void StopAction();
        }

        private class LeyDjinnAIState_FireBeam : LeyDjinnAIState_DirectedAttack
        {
            public override AIActionType GetStateKey() => AIActionType.FireBeam;
            public override string MainAnimationParamID => AnimationParamID.Firing;

            public override void Initialize()
            {
                base.Initialize();

                timeBeforeActivate = .25f;
                timeStillAfterActivate = .1f;

                ChooseDirectionalMode(context.actions.attackModeBeam);
            }

            protected override void StartAction()
            {
                context.animator.ResetTrigger(AnimationParamID.PhaseReady);
                context.currentSkill.Activate();
            }

            protected override void StopAction()
            {
                context.currentSkill.Deactivate();
            }
        }
        
        private class LeyDjinnAIState_FireCone : LeyDjinnAIState_DirectedAttack
        {
            public override AIActionType GetStateKey() => AIActionType.FireCone;
            public override string MainAnimationParamID => AnimationParamID.Conjuring;

            public override void Initialize()
            {
                base.Initialize();

                timeBeforeActivate = 0f;
                timeStillAfterActivate = 0f;

                ChooseDirectionalMode(context.actions.attackModeCone);
            }

            protected override void StartAction()
            {
                context.animator.ResetTrigger(AnimationParamID.PhaseReady);
                context.currentSkill.Activate();
            }

            protected override void StopAction()
            {
                context.currentSkill.Deactivate();
            }
        }

        private abstract class LeyDjinnAIState_StationaryAction : LeyDjinnAIState
        {
            public override string MainAnimationParamID => AnimationParamID.Summoning;

            protected float timeBeforeActivate = .5f;
            protected float timeStillAfterwards = .25f;

            public override void Enter()
            {
                base.Enter();

                Analyzer.Myself.Update();
                Analyzer.CurrentTarget.Update();
            }

            protected override IEnumerator UpdateState()
            {
                yield return null;

                // wait upfront
                yield return new WaitForSeconds(timeBeforeActivate);

                context.currentSkill.Activate();

                // deactivates itself
                yield return new WaitUntil(context.currentSkill.IsNotActive);

                // wait a bit more...
                yield return new WaitForSeconds(timeStillAfterwards);

                context.stm.GotoDefault();
            }
        }

        private class LeyDjinnAIState_Summon : LeyDjinnAIState_StationaryAction
        {
            public override AIActionType GetStateKey() => AIActionType.Summon;
        }

        private class LeyDjinnAIState_Explosion : LeyDjinnAIState_StationaryAction
        {
            public override AIActionType GetStateKey() => AIActionType.Explode;
        }

        private class LeyDjinnAIState_Aura : LeyDjinnAIState_StationaryAction
        {
            public override AIActionType GetStateKey() => AIActionType.Aura;
        }
        #endregion

        #region events
        private void OnDestroy()
        {
            WorldSceneAIManager.RemoveAIActor(this);
        }

        private void OnEnable()
        {
            interestZoneFar.gameObject.SetActive(true);
            interestZoneMid.gameObject.SetActive(true);
            interestZoneClose.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            interestZoneFar.gameObject.SetActive(false);
            interestZoneMid.gameObject.SetActive(false);
            interestZoneClose.gameObject.SetActive(false);
        }

        private void HandleVulnerabilityEvent(VulnerabilityEventType type)
        {
            switch (type)
            {
                case VulnerabilityEventType.HitModeBegan:
                    animator.SetFloat(AnimationParamID.Mood, (int)BossEmotionalMood.Bothered);
                    animator.SetLayerWeight(2, 1f);
                    break;

                case VulnerabilityEventType.HitModeEnded:
                    animator.SetFloat(AnimationParamID.Mood, (int)BossEmotionalMood.Default);
                    animator.SetLayerWeight(2, 0f);
                    break;

                case VulnerabilityEventType.Died:
                    for(int i=0; i< headSmoke.Length; i++)
                        ParticleSystemUtil.SetEmissionEnabled(headSmoke[i], false);
                    
                    stm.ChangeState(AIActionType.MakeDeathMove);
                    break;
            }
        }

        private void HandleInterestZoneChange(GameObject target, AIInterestZone.AIInterestZoneID zoneID, bool within)
        {
            aiAnalyzer.NotifyPossibleTarget(target, zoneID, within);
        }

        private void HandleNewTarget(AITargetInfo targetInfo)
        {
            Debug.Log("HandleNewTarget " + targetInfo);
        }

        private void HandleAnimationOver(string id)
        {
            switch(id)
            {
                case AnimationID.Start:
                    animator.SetTrigger(AnimationParamID.PhaseReady);
                    break;

                case AnimationID.Die:
                    SpawnElement(handFront.gameObject, deadSpawnHandTemplate, 0f);
                    SpawnElement(handBack.gameObject, deadSpawnHandTemplate, 0f);
                    SpawnElement(face, deadSpawnHeadTemplate, 0f);

                    for (int i=0; i<skeleton.joints.Length; i++)
                    {
                        SpawnElement(skeleton.joints[i].gameObject, deadSpawnSpineTemplate, .1f * i);
                    }

                    break;
            }
        }

        private void SpawnElement(GameObject element, GameObject template,float delay)
        {
            StartCoroutine(DissolveIntoLeySources(element, template, delay));
        }

        private IEnumerator DissolveIntoLeySources(GameObject element, GameObject template, float delay)
        {
            yield return new WaitForSeconds(delay);

            element.SetActive(false);

            GameObject spawned = EntityFactory.Create(template);

            spawned.transform.position = element.transform.position;
            WorldScene.SortObject(spawned);
        }

        #endregion
    }
}
