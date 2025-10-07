using MageGame.Collisions.Behaviours;
using MageGame.Common.Data;
using UnityEngine;
using UnityEngine.Events;

namespace MageGame.AI.States.Simple
{
    /// <summary>
    /// Informs higher entity about entering or leaving entities.
    /// </summary>
    public class AIInterestZone : MonoBehaviour
    {
        [System.Flags]
        public enum AIInterestZoneID
        {
            Default=0,
            Left=1,
            Right=2,
            Close=4,
            Mid=8,
            Far=16,
        }

        #region configuration
        public AIInterestZoneID interestZoneID;
        public ProximityCollector collector;
        #endregion

        public UnityEvent<GameObject, AIInterestZoneID, bool> ObjectChangeEvent { get; internal set; } = new UnityEvent<GameObject, AIInterestZoneID, bool>();

        virtual protected void Awake()
        {
            collector.AddHandles(IsInteresting, HandleEntered, HandleLeft);
        }

        protected bool IsInteresting(GameObject sensed)
        {
            return sensed.tag == GameObjectTag.Character;
        }

        protected bool HandleEntered(GameObject sensed)
        {
            ObjectChangeEvent.Invoke(sensed, interestZoneID, true);
            return true;
        }

        protected bool HandleLeft(GameObject sensed)
        {
            ObjectChangeEvent.Invoke(sensed, interestZoneID, false);
            return true;
        }
    }
}