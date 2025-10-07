using MageGame.AI.Validation;
using MageGame.Skills;
using UnityEngine;

namespace MageGame.AI.Data
{
    [System.Serializable]
    public class AIActionOption
    {
        public AIOptionTag tags;

        public ActionSkill skill;

        [Range(0f,100f)]
        public float chance;

        [Tooltip("Delay in seconds after which abilty can be used for first time. 0 = right away.")]
        public float availableAfter;

        [Tooltip("Delay in seconds after which abilty can be re-used. 0 = instantly again.")]
        public float reusableAfter;

        [Tooltip("Customizes the deed.")]
        public AIActionParameters parameters;

        // state
        [System.NonSerialized]
        public Condition condition;

        [System.NonSerialized]
        public float coolDown;
    }
}