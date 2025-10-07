using MageGame.AI.Data;
using MageGame.AI.Validation;
using MageGame.Collections.Utils;
using MageGame.Common.Utils;
using MageGame.Skills;
using System.Collections.Generic;

namespace MageGame.AI.Core
{
    static public class AIOptionProcessor
    {
        static public List<AIActionOptionStatus> InitOptionStatuses(AIAgentContext context, AISkillOption[] options)
        {
            List<AIActionOptionStatus> optionStatuses = new List<AIActionOptionStatus>();

            for (int i = 0; i < options.Length; i++)
            {
                AISkillOption option = options[i];
                AIActionOptionStatus state = new AIActionOptionStatus();
                optionStatuses.Add(state);

                state.skill = option.skill;
                state.parameters = new AIActionParameters();
                state.condition = CreateCondition(state.skill, context);
                AIActionOptionStatus.Init(state, option.settings);
            }

            return optionStatuses;
        }

        static public void UpdateOptionStatuses(AIDecisions decisions)
        {
            List<AIActionOptionStatus> optionStatuses = decisions.allOptions;

            AIOptionTag modeTag = GenerateTagsFromMode(decisions.newActionMode);
            AIOptionTag distanceTag = GenerateTagsFromDistanceType(decisions.currentDistanceType);

            decisions.maxOptionChanceValue = 0f;

            for (int i = 0; i < optionStatuses.Count; i++)
            {
                AIActionOptionStatus optionStatus = optionStatuses[i];
                AIOptionTag optionTags = optionStatus.settings.tags;

                optionStatus.chance = 0f;

                if (optionStatus.skill != null && !optionStatus.skill.enabled)
                {
                    continue;
                }

                if (optionTags.HasFlag(modeTag))
                {
                    if (optionTags.HasFlag(distanceTag))
                    {
                        optionStatus.chance += 10f;

                        if (optionTags.HasFlag(AIOptionTag.Chance_Lesser)) optionStatus.chance -= 5f;
                        if (optionTags.HasFlag(AIOptionTag.Chance_Higher)) optionStatus.chance += 5f;

                        decisions.maxOptionChanceValue += optionStatus.chance;
                    }
                }
            }
        }

        static internal AIOptionTag GenerateTagsFromDistanceType(AIDistanceType distanceType)
        {
            switch (distanceType)
            {
                case AIDistanceType.Close: return AIOptionTag.Range_Close;
                case AIDistanceType.Mid: return AIOptionTag.Range_Mid;
                case AIDistanceType.Far: return AIOptionTag.Range_Far;
            }

            return AIOptionTag.None;
        }

        internal static AIOptionTag GenerateTagsFromMode(AIActionMode newMode)
        {
            switch (newMode)
            {
                case AIActionMode.Attack: return AIOptionTag.Attack;
                case AIActionMode.Defend: return AIOptionTag.Defend;
                case AIActionMode.Dodge: return AIOptionTag.Dodge;
                case AIActionMode.Support: return AIOptionTag.Support;
                case AIActionMode.Weaken: return AIOptionTag.Weaken;
                case AIActionMode.CallForHelp: return AIOptionTag.CallForHelp;
                case AIActionMode.Wait: return AIOptionTag.Wait;
                case AIActionMode.Retreat: return AIOptionTag.Retreat;
            }

            return AIOptionTag.None;
        }

        static public AIActionOptionStatus DecideOption(AIDecisions decision)
        {
            float dice = RandomUtil.FloatPositive(decision.maxOptionChanceValue);

            float value = 0;

            List<AIActionOptionStatus> options = decision.allOptions;

            List<int> preventedOnes = new List<int>();

            for (int i = 0; i < options.Count; i++)
            {
                AIActionOptionStatus option = options[i];
                value += option.chance;

                //Debug.Log("- " + AITacticalSettingsUtil.Describe(option) + ": " + dice.ToString("0.") + "/" + (value.ToString("0.")));

                if (option.chance > 0f && dice <= value)
                {
                    if (option.IsPossibleNow() && option.IsAdvisableNow())
                    {
                        return option;
                    }
                    else
                    {
                        preventedOnes.Add(i);
                    }
                }
            }

            switch (preventedOnes.Count)
            {
                case 0: return null;

                case 1:
                    AIActionOptionStatus option = options[preventedOnes[0]];
                    if (option.IsPossibleNow() && option.IsAdvisableNow())
                        return option;
                    break;

                default:
                    ListUtil.Shuffle(preventedOnes);

                    for (int i = 0; i < preventedOnes.Count; i++)
                    {
                        option = options[preventedOnes[i]];
                        if (option.IsPossibleNow() && option.IsAdvisableNow())
                            return option;
                    }
                    break;
            }

            return null;
        }


        static public Condition CreateCondition(ActionSkill skill, AIAgentContext context)
        {
            if (skill == null)
                return null;

            switch (skill.SkillType)
            {
                case SkillType.Summon:
                    return new Condition_Summonable(context.myObjectInfo, (MagicSkill_Summon)skill);

                case SkillType.ConjureTarget:
                    return new Condition_ConjureTarget(context.myObjectInfo, (MagicSkill_Effect)skill);

                case SkillType.RangedAttack:
                    return new Condition_RangedAttack(context.myObjectInfo, skill);
            }

            return null;
        }

        static public void UpdateConditions(this List<AIActionOptionStatus> options, AITargetInfo target)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].condition is ITargetCondition)
                    ((ITargetCondition)options[i].condition).UpdateTarget(target);
            }
        }
    }
}
