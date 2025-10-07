using MageGame.Events;
using MageGame.AI.Core;
using MageGame.Skills;
using UnityEngine;
using MageGame.Common.Utils;

namespace MageGame.AI.Data.Modules
{
    [System.Serializable]
    public class AIModule_Support : AIDecisionModule, ISkillProvidingModule, ITargetAlteringModule
    {
        [Range(0,100)] public byte chanceToIgnore = 80;
        [Range(0,5)] public byte numTicksSustainOnEnd = 1;

        public ActionSkill skill;
        public ActionSkill GetSkill() => skill;

        private byte counter;
        protected bool needed;
        protected byte usedCounter;
        protected float timeStartedAt;

        internal override void Initialize()
        {
            base.Initialize();

            available = skill != null && skill.enabled;
        }

        protected bool CanUseSkill()
        {
            return skill.CanBeUsed() && skill.HasEnoughPower();
        }

        public override bool Check()
        {
            if (!available) return false;

            if (needed && CanUseSkill())
            {
                if (!active)
                {
                    if (RandomUtil.Chance100() <= chanceToIgnore)
                        return false;

                    if (!CanUseSkill())
                        return false;

                    if (skill.enabled && skill.IsCoolDownReady())
                    {
                        //tactics.context.target
                        context.MakeCurrentTarget(context.myObjectInfo.Clone());
                        context.targetLocked = true;
                        active = true;
                        usedCounter++;
                        timeStartedAt = Time.time;
                        counter = 0;

                        decisions.InvalidateMode(AIActionMode.Support);
                    }
                }
                else
                {
                    if (!skill.IsInUse())
                        active = false;

                }
            }
            else
            {
                if (active)
                {
                    counter++;
                    if (counter > numTicksSustainOnEnd || !CanUseSkill())
                    {
                        active = false;
                        context.targetLocked = false;
                        context.ForgetCurrentTarget();
                    }
                }
            }

            return active;
        }

        public override void Activate()
        {
            base.Activate();

            context.agent.AIEventEnDetail.AddListener(HandleAIEvent);
        }

        public override void Deactivate()
        {
            base.Deactivate();

            context.agent.AIEventEnDetail.RemoveListener(HandleAIEvent);
        }

        virtual protected bool ShallUse() => needed;
        public override bool HasActionParameters() => true;
        public override AIActionParameters GetActionParameters() => AIConditionParameters.Create(ShallUse);
        
        virtual protected void HandleAIEvent(AIEventType eventType, object value)
        {
            
        }
    }
}
