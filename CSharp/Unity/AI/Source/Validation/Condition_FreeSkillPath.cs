using MageGame.AI.Core;
using MageGame.Skills;

namespace MageGame.AI.Validation
{
    public class Condition_FreeSkillPath : Condition
    {
        public PathAnalysis analysis;
        public AITargetInfo actorInfo;
        public AITargetInfo targetInfo;
        public ActionSkill skill;
        public AISkillValidation validation;
        public bool movementOnly;

        public Condition_FreeSkillPath()
        {
            analysis = new PathAnalysis();
        }

        public Condition_FreeSkillPath(AITargetInfo actorInfo, AISkillValidation validation) : this()
        {
            this.actorInfo = actorInfo;
            this.validation = validation;
        }

        public override bool Check()
        {
            analysis.freePath = true; // reset

            if (skill != null)
            {
                if (movementOnly)
                {
                    analysis = validation.CheckPath(skill, actorInfo, targetInfo, analysis, validation.MovementParameters);
                }
                else
                {
                    if (skill is IClearShotRequirer)
                    {
                        if (skill is IAimingRequirer && ((IAimingRequirer)skill).GetTrajectoryData() != null)
                            analysis = validation.CheckTrajectoryPath(skill, ((IAimingRequirer)skill).GetTrajectoryData(), actorInfo, targetInfo, analysis, validation.SkillParametersShoot);
                        else
                            analysis = validation.CheckPath(skill, actorInfo, targetInfo, analysis, validation.SkillParametersShoot);
                    }
                    else if (skill is IMeleeSkill)
                    {
                        analysis = validation.CheckPath(skill, actorInfo, targetInfo, analysis, validation.SkillMeleePathParams);
                    }
                }
            }

            return analysis.freePath;
        }

        public override bool IsMet()
        {
            if (movementOnly || skill is IClearShotRequirer || skill is IMeleeSkill)
                return analysis.freePath;
            else
                return true;
        }
    }
}