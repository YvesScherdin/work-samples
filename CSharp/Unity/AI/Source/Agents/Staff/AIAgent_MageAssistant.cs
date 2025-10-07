using MageGame.AI.Agents.Default;

namespace MageGame.AI.Agents.Staff
{
    public class AIAgent_MageAssistant : AIAgent_Default
    {
        protected override void CreateFixAIComponents()
        {
            base.CreateFixAIComponents();

            behaviourFSM.AddState(new AIBS_Work());
            actionFSM.AddState(new AIAS_Work());
            actionFSM.AddState(new AIAS_StoreItems());
        }
    }
}
