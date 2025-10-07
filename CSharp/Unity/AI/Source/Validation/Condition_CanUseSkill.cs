using MageGame.AI.Core;
using MageGame.Skills;

namespace MageGame.AI.Validation
{
    public class Condition_SkillIsUsable : Condition
    {
        public AITargetInfo actorInfo;
        public ActionSkill skill;
        public AISkillValidation validation;
        public SkillAnalysis analysis;
        public bool possible;

        public Condition_SkillIsUsable()
        {
            analysis = new SkillAnalysis();
        }

        public Condition_SkillIsUsable(AITargetInfo actorInfo, AISkillValidation validation) : this()
        {
            this.actorInfo = actorInfo;
            this.validation = validation;
        }

        public override bool Check()
        {
            possible = validation.CheckGeneral(actorInfo, skill, analysis).usable;
            return possible;
        }
    }
}