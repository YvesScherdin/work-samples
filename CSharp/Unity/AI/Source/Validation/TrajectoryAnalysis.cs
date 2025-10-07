using MageGame.AI.Data;
using UnityEngine;

namespace MageGame.AI.Validation
{
    public class PathAnalysis
    {
        public bool freePath;
        public AIObstacleType obstacleType;
        public float distance;

        public Vector3 trajectoryTarget;
        public float trajectoryStartAngle;
    }
}