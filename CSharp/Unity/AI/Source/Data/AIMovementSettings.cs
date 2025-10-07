using UnityEngine;

namespace MageGame.AI.Data
{
    [System.Serializable]
    public class AIMovementSettings
    {
        public AILocomotionType defaultType = AILocomotionType.Terristic;

        [Header("Flags")]
        public bool preciseBraking = true;
        public bool swingOnPath = false;
        public bool constantHovering = false;
        public bool fixPosition;

        [Header("Actions")]
        public AIWanderParameters wander;
        public HoldPositionParameters holdPosition;
    }
}
