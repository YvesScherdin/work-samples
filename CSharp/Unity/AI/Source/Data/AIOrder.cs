using UnityEngine;

namespace MageGame.AI.Data
{
    public class AIOrder
    {
        public AIBehaviourType behaviourType;
        public AIActionType actionType;
        public GameObject target;
        public AIActionParameters parameters;
        public int priority;

        public AIOrder() { }

        public AIOrder(AIBehaviourType behaviourType, AIActionParameters parameters = null)
        {
            this.behaviourType = behaviourType;
            this.parameters = parameters;
        }
        
        public AIOrder(AIBehaviourType behaviourType, GameObject target = null, AIActionParameters parameters = null)
        {
            this.behaviourType = behaviourType;
            this.parameters = parameters;
            this.target = target;
        }

        public AIOrder(AIActionType actionType, GameObject target=null, AIActionParameters parameters=null)
        {
            this.actionType = actionType;
            this.parameters = parameters;
            this.target = target;
        }

        public override string ToString()
        {
            if (behaviourType != AIBehaviourType.Unspecified)
                return behaviourType.ToString();
            else
                return actionType.ToString();
        }

        public AIOrder WithPriority(int value)
        {
            this.priority = value;
            return this;
        }
    }
}
