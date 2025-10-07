using MageGame.AI.Agents.Default;
using MageGame.AI.Agents.Visitors;
using MageGame.AI.Data;

namespace MageGame.AI.Agents.Citizen
{
    public class AIAgent_Trader : AIAgent_Default
    {
        public override string AgentID => "Trader";

        protected override void CreateFixAIComponents()
        {
            base.CreateFixAIComponents();

            behaviourFSM.AssureExistence(AIBehaviourType.LeadCaravan, typeof(AIBS_LeadCaravan));
            behaviourFSM.AssureExistence(AIBehaviourType.WorkAtCaravan, typeof(AIBS_WorkAtCaravan));
            behaviourFSM.AssureExistence(AIBehaviourType.Follow, typeof(AIBS_Follow));

            actionFSM.AssureExistence(AIActionType.Follow, typeof(AIAS_Follow));
            actionFSM.AssureExistence(AIActionType.Trade, typeof(AIAS_Trade));
            actionFSM.AssureExistence(AIActionType.HandleCargo, typeof(AIAS_HandleCargo));
            actionFSM.AssureExistence(AIActionType.LeaveScene, typeof(AIAS_LeaveScene));
        }
    }
}