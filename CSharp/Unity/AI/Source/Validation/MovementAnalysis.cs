using UnityEngine;

namespace MageGame.AI.Validation
{
    public class MovementAnalysis
    {
        public Vector3 averagePosition;
        public Vector3 averagePositionDelta;
        public Vector3 lastPos;
        public float distanceMovedTotal;
        public float distanceMoved;
        public float timeTrackingStarted;
        public float moveSpeedScale; // could get affected by damping, slowening effects etc.

        internal Transform objectTransform;

        public void StartTracking()
        {
            lastPos = objectTransform.position;
            averagePosition = lastPos;
            distanceMovedTotal = 0f;
            distanceMoved = 0f;
            timeTrackingStarted = Time.time;
        }

        internal void UpdateTracking()
        {
            distanceMoved = (objectTransform.position - lastPos).magnitude;
            distanceMovedTotal += distanceMoved;

            averagePositionDelta = (lastPos - objectTransform.position) * .5f;
            lastPos = objectTransform.position;
            averagePosition = (averagePosition + lastPos) * .5f;
        }

        internal void StopTracking()
        {
            timeTrackingStarted = -1f;
        }

        public float GetAveragePositionDeltaValue()
        {
            return (averagePositionDelta.x < 0f ? -averagePositionDelta.x : averagePositionDelta.x)
            + (averagePositionDelta.y < 0f ? -averagePositionDelta.y : averagePositionDelta.y);
        }
    }
}
