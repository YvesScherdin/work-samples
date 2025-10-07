using MageGame.AI.Data.Modules;
using MageGame.Common.Data;
using MageGame.Skills;
using UnityEngine;

namespace MageGame.AI.Data
{
    [System.Serializable]
    public class AICombatSettings
    {
        public bool active = true;

        [Range(0f, 1f)]   public float accuracy;
        [Range(0f, 0.5f)] public float accuracyVariance;
        [Min(0f)]         public float hurryThreshold;

        public bool noFighting;
        public bool noFleeing;
        public bool dontLoseTarget;
        public bool ignoreBadProximity;

        public AIDistanceType preferredDistance;
        public RangeF midRange;

        public bool optionsEnabled = true;
        public AISkillOption[] options;
        public AICombatActionSettings actions;
        public AIInterimActionSettings interimActions;
        public AIModuleBundle_Combat modules;
    }

    static public class AICombatSettingsExtensions
    {
        static public bool HasAccuracy(this AICombatSettings settings) => settings.accuracy > 0f || settings.accuracyVariance > 0f;

        static public ActionSkill FindAppropriateOptionSkill(this AICombatSettings settings, AIOptionTag tags)
        {
            if (!settings.optionsEnabled || settings.options.Length == 0)
                return null;
            else
            {
                ActionSkill bestSkill = null;

                for (int i = 0; i < settings.options.Length; i++)
                {
                    int combo = (int)settings.options[i].settings.tags & (int)tags;
                    if (combo != 0)
                    {
                        if (combo == (int)tags)
                            return settings.options[i].skill;
                        else if (bestSkill == null)
                            bestSkill = settings.options[i].skill;
                    }
                }

                return bestSkill;
            }
        }
    }
}
