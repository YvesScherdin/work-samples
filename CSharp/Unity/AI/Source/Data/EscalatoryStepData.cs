using MageGame.AI.Pathes;
using MageGame.Mechanics.Skills.Configurations;
using MageGame.Skills;
using UnityEngine;

namespace MageGame.AI.Data
{
    [System.Serializable]
    public class EscalatoryStepData
    {
        [Tooltip("Health in percent.")]
        [Range(0f, 1f)] public float threshold;

        public SkillData attack;
        public SkillData defend;
        public SkillData specialAttack;

        public SkillData closeEncounterSkill;
        public SkillData wanderSkill;
        public SkillData restSkill;

        public CheckPoint initialCheckPoint;
    }

    public class EscalatoryStepInfo
    {
        public ActionSkill attack;
        public ActionSkill defend;
        public ActionSkill specialAttack;
        
        public ActionSkill closeEncounterSkill;
        public ActionSkill wanderSkill;
        public ActionSkill restSkill;

        public CheckPoint initialCheckPoint;
    }
}
