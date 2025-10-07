using MageGame.Collisions.Behaviours;
using MageGame.Common.Data;
using MageGame.Scripting.Triggers;
using System;
using UnityEngine;

namespace MageGame.AI.States.Simple
{
    public class SimpleAIPerception : MonoBehaviour
    {
        public event Action<GameObject> Perceived;
        public event Action<GameObject> Lost;

        public SimpleTriggerArea triggerArea;

        virtual protected void Awake()
        {
            triggerArea.proximity.AddHandles(CanPerceive, Perceive, Lose);
            triggerArea.gameObject.SetActive(false);
        }

        protected void Start()
        {
            triggerArea.gameObject.SetActive(true);
        }

        protected bool CanPerceive(GameObject sensed)
        {
            return sensed.tag == GameObjectTag.Character;
        }

        protected bool Perceive(GameObject sensed)
        {
            if (Perceived != null)
                Perceived.Invoke(sensed);
            return true;
        }

        protected bool Lose(GameObject sensed)
        {
            if (Lost != null)
                Lost.Invoke(sensed);
            return true;
        }
    }
}