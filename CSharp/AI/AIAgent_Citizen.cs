using MageGame.AI.Agents.Default;
using MageGame.AI.Agents.Visitors;
using MageGame.AI.States.Movement;

namespace MageGame.AI.Agents.Citizen
{
    public class AIAgent_Citizen : AIAgent
    {
        public override string AgentID => "Citizen";

        protected override void Initialize()
        {
            base.Initialize();

            analyzer = new AIAnalyzer_Citizen();

            behaviourFSM.AddState<AIBS_IdleDefault>();
            behaviourFSM.AddState<AIBS_Follow>();
            behaviourFSM.AddState<AIBS_Retreat>();

            actionFSM.AddState<AIAS_FleeFromTarget>();
            actionFSM.AddState<AIAS_HoldPosition>();
            actionFSM.AddState(typeof(AIAS_Follow));
            actionFSM.AddState(typeof(AIAS_LeaveScene));

            if (availableActions.cover) actionFSM.AddState<AIAS_DuckAndCover>();

            if (availableActions.wander)
            {
                actionFSM.AddState<AIAS_Wander>();
                actionFSM.AddState<AIAS_MoveAlongCheckPoints>();
            }

            if (behaviourFSM.InitialState == null)
                behaviourFSM.SetInitialState<AIBS_IdleDefault>();
        }
    }
}