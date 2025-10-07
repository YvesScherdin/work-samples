using MageGame.Common.Data;
using UnityEngine;

namespace MageGame.AI.Data
{
    [System.Serializable]
    public class AIFollowerParameters : AIActionParameters
    {
        [Header("Follower-specific")]
        public GameObject whom;

        public RangeF range = new RangeF(2f, 4f);
        public float hurryThreshold = 7f;

        public bool fallbackPlayer;
        public bool allowCombat = true;
        
        static public AIFollowerParameters Create(GameObject whom, float hurryThreshold=7f, bool allowCombat=true)
        {
            AIFollowerParameters parameters = new AIFollowerParameters();
            parameters.whom = whom;
            parameters.hurryThreshold = hurryThreshold;
            parameters.allowCombat = allowCombat;
            return parameters;
        }

        static public AIFollowerParameters Create(GameObject whom, RangeF range, float hurryThreshold=7f, bool allowCombat=true)
        {
            AIFollowerParameters parameters = new AIFollowerParameters();
            parameters.whom = whom;
            parameters.range = range;
            parameters.hurryThreshold = hurryThreshold;
            parameters.allowCombat = allowCombat;
            return parameters;
        }
    }
}
