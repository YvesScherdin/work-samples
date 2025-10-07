using MageGame.AI.Data;
using MageGame.Mechanics.Skills.Configurations;
using MageGame.Skills;

namespace MageGame.AI.Agents.Bosses
{
    static public class BossUtil
    {

        static public ActionSkill ResolveSkill(AIAgent agent, SkillData skillData, AIOptionTag fallbackTag)
        {
            ActionSkill skill = skillData != null ? agent.context.skills.GetSkillByData(skillData) : null;

            if (skill == null)
                skill = agent.combat.FindAppropriateOptionSkill(fallbackTag);

            return skill;
        }

        static public void InitEscalatoryStep(AIAgent agent, EscalatoryStepInfo stepInfo, EscalatoryStepData stepData, EscalatoryStepData stepDefaults)
        {
            stepInfo.attack = ResolveSkill(agent, stepData.attack ?? stepDefaults.attack, AIOptionTag.Attack);
            stepInfo.defend = ResolveSkill(agent, stepData.defend ?? stepDefaults.defend, AIOptionTag.Defend);
            stepInfo.specialAttack = ResolveSkill(agent, stepData.specialAttack ?? stepDefaults.specialAttack, AIOptionTag.Attack | AIOptionTag.Type_Special);
            stepInfo.closeEncounterSkill = ResolveSkill(agent, stepData.closeEncounterSkill ?? stepDefaults.closeEncounterSkill, AIOptionTag.Attack | AIOptionTag.Type_Melee);
            stepInfo.wanderSkill = ResolveSkill(agent, stepData.wanderSkill ?? stepDefaults.wanderSkill, AIOptionTag.Attack | AIOptionTag.Type_Mobile);
            stepInfo.restSkill = ResolveSkill(agent, stepData.restSkill ?? stepDefaults.restSkill, AIOptionTag.Rest | AIOptionTag.Defend | AIOptionTag.Support);
            stepInfo.initialCheckPoint = stepData.initialCheckPoint ?? stepDefaults.initialCheckPoint;

        }
    }
}
