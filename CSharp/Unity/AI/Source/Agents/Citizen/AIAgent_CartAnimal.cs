using MageGame.AI.Agents.Default;
using MageGame.AI.Agents.Vehicles;
using MageGame.AI.Agents.Visitors;
using MageGame.AI.Data;
using MageGame.AI.States.Movement;

namespace MageGame.AI.Agents.Citizen
{
    public class AIAgent_CartAnimal : AIAgent
    {
        public override string AgentID => "CartAnimal";

        protected override void CreateFixAIComponents()
        {
            base.CreateFixAIComponents();

            behaviourFSM.AddState(new AIBS_IdleDefault());
            behaviourFSM.AddState(new AIBS_Follow());
            behaviourFSM.AddState(new AIBS_Retreat());

            actionFSM.AddState(new AIAS_FleeFromTarget());
            actionFSM.AddState(new AIAS_HoldPosition());
            actionFSM.AddState(new AIAS_Park());
            actionFSM.AddState(new AIAS_Follow());
            actionFSM.AddState(new AIAS_LeaveScene());

            if (availableActions.cover)
                actionFSM.AddState(new AIAS_DuckAndCover());
            
            if (availableActions.wander)
            {
                actionFSM.AddState(new AIAS_Wander());
                actionFSM.AddState(new AIAS_MoveAlongCheckPoints());
            }

            if (behaviourFSM.DefaultStateType == AIBehaviourType.Unspecified)
                behaviourFSM.DefaultStateType = AIBehaviourType.Idle;
        }
    }
}
