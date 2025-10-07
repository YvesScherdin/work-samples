using MageGame.Skills.Simple;

namespace MageGame.AI.Data
{
    [System.Serializable]
    public class SkillOptionGroup
    {
        public SimpleSkillOption[] options;
        public SimpleSkillOption fallback;

        public SimpleSkill ChooseRandom()
        {
            return ChooseRandomOption()?.skill ?? fallback?.skill;
        }

        public SimpleSkillOption ChooseRandomOption()
        {
            float randomMax = 0f;

            bool[] availables = new bool[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                if (availables[i] = options[i].IsAvailable())
                    randomMax += options[i].probability;
            }

            float randomValue = UnityEngine.Random.Range(0f, randomMax);
            float min = 0f, max = 0f;

            for (int i = 0; i < options.Length; i++)
            {
                if (availables[i])
                {
                    max += options[i].probability;

                    if (randomValue >= min && randomValue <= max)
                    {
                        return options[i];
                    }

                    min += options[i].probability;
                }
            }

            return null;
        }
    }
}
