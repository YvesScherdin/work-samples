using MageGame.AI.Agents.Default;
using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.States.Movement;
using MageGame.Data;
using MageGame.Skills;
using UnityEngine;

namespace MageGame.AI.Agents.Demons
{
    /// <summary>
    /// 'Boeff' is a thieve spirit. It does not attack, but it will try to steal from its target.
    /// </summary>
    public class AIAgent_Boeff : AIAgent_Default
    {
        public override string AgentID => "Boeff";

        public AITheftSettings theftSettings;
        public JobSkill_Steal stealSkill;

        private AIBS_Theft theftBehaviourState;

        protected override void Initialize()
        {
            analyzer = new AIAnalyzer_Boeff(stealSkill);

            behaviourFSM.AddState<AIBS_IdleBoeff>();
            behaviourFSM.AddState<AIBS_Retreat>();
            actionFSM.AddState<AIAS_FleeFromTarget>();

            behaviourFSM.AddState<AIBS_Theft>();
            behaviourFSM.GetState<AIBS_Theft>().Configure(theftSettings, stealSkill);
            ((AIAnalyzer_Boeff)analyzer).theftBehaviourState = behaviourFSM.GetState<AIBS_Theft>();


            actionFSM.AddState<AIAS_StealItems>();

            // movement
            actionFSM.AddState<AIAS_HoldPosition>();

            actionFSM.AddState<AIAS_ApproachTarget>();
            actionFSM.AddState<AIAS_ThinkAboutPath>();
            actionFSM.AddState<AIAS_MoveAlongPath>();

            // initial states
            if (behaviourFSM.InitialState == null)
                behaviourFSM.SetInitialState<AIBS_IdleBoeff>();

            if (actionFSM.InitialState == null)
                actionFSM.SetInitialState<AIAS_HoldPosition>();

            if (availableActions.wander)
            {
                actionFSM.AddState<AIAS_Wander>();
                actionFSM.AddState<AIAS_MoveAlongCheckPoints>();
            }

            theftBehaviourState = context.BehaviourFSM.GetState<AIBS_Theft>();
        }

        protected override bool DecideActionForTarget(AITargetInfo info)
        {
            switch (info.note)
            {
                case AIHandlingNote.StealFrom:
                    if (theftBehaviourState.CanStealNow())
                    {
                        context.BehaviourFSM.ChangeState<AIBS_Theft>();
                        return true;
                    }
                    break;
            }

            return base.DecideActionForTarget(info);
        }
    }

    public class AIAnalyzer_Boeff : AIAnalyzer_Default
    {
        internal AIBS_Theft theftBehaviourState;
        private JobSkill_Steal stealSkill;

        public AIAnalyzer_Boeff(JobSkill_Steal stealSkill) : base()
        {
            this.stealSkill = stealSkill;
        }

        public override AITargetInfo AnalyzeGameObject(GameObject gameObject, AITargetAnalysisParameter parameters)
        {
            AITargetInfo info = base.AnalyzeGameObject(gameObject, parameters);

            if (stealSkill.detector.MayHit(gameObject))
            {
                if(info == null)
                    info = new AITargetInfo().Analyze(gameObject);

                if (info.relation < 0)
                    info.priority += 100;

                if (info.gameObject.tag == GameObjectTag.Character)
                    info.priority += 50;

                info.note = AIHandlingNote.StealFrom;
            }

            return info;
        }

        protected override void ProcessNewTarget(AITargetInfo info)
        {
            if (info.note == AIHandlingNote.StealFrom
                && theftBehaviourState.CanStealNow()
                && (!context.HasTarget() || context.targetObjectInfo.priority < info.priority))
            {
                context.MakeCurrentTarget(info);
            }
            else
            {
                if(info.note == AIHandlingNote.StealFrom && info.IsAThreat())
                {
                    if (CanDestroy(info) && agent.combat.active)
                        info.note = AIHandlingNote.Destroy;
                    else
                        info.note = AIHandlingNote.FleeFrom;
                }
                
                base.ProcessNewTarget(info);
            }
        }
    }

    public class AIBS_IdleBoeff : AIBS_IdleDefault
    {
        protected override void DecideIdleAction()
        {
            base.DecideIdleAction();

            recheckTimerActive = true;
            recheckTimer = 3f;
        }
    }

}