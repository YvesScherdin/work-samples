using MageGame.AI.Data;
using MageGame.AI.States.Movement;
using MageGame.Behaviours.Relationships;

namespace MageGame.AI.Agents.Default
{
    public class AIAgent_Default : AIAgent
    {
        public override string AgentID => "Default";

        protected override void Start()
        {
            if (gameObject.GetComponent<IdentificationFriendOrFoe>() == null)
            {
                IdentificationFriendOrFoe.Initialize(gameObject.AddComponent<IFF_Cognitive>());
            }

            base.Start();
        }

        protected override void CreateFixAIComponents()
        {
            analyzer = new AIAnalyzer_Default();

            // idle actions
            behaviourFSM.AddState(new AIBS_IdleDefault());
            actionFSM.AddState(new AIAS_HoldPosition());

            // movement
            actionFSM.AddState(new AIAS_ApproachTarget());
            actionFSM.AddState(new AIAS_ThinkAboutPath());
            actionFSM.AddState(new AIAS_MoveAlongPath());

            // initial states
            if (behaviourFSM.DefaultStateType == AIBehaviourType.Unspecified)
                behaviourFSM.DefaultStateType = AIBehaviourType.Idle;

            if (actionFSM.DefaultStateType == AIActionType.Unspecified)
                actionFSM.DefaultStateType = AIActionType.HoldPosition;

            // social
            actionFSM.AddState(new AIAS_Party());

            // combat
            behaviourFSM.AssureExistence(AIBehaviourType.Combat, typeof(AIBS_CombatDefault));
            actionFSM.AssureExistence(AIActionType.MakeBattleCry, typeof(AIAS_BattleCry));
            actionFSM.AssureExistence(AIActionType.Interim, typeof(AIAS_InterimAction));
            actionFSM.AssureExistence(AIActionType.Attack, typeof(AIAS_AttackDefault));

            behaviourFSM.AssureExistence(AIBehaviourType.Rest, typeof(AIBS_Rest));
            
            behaviourFSM.AddState(new AIBS_Retreat());
            actionFSM.AddState(new AIAS_FleeFromTarget());

            if (availableActions.cover)     actionFSM.AddState(new AIAS_DuckAndCover());
            if (availableActions.defend)    actionFSM.AddState(new AIAS_Defend());
            if (availableActions.watch)     actionFSM.AddState(new AIAS_Watch());
            if (availableActions.patrol)    behaviourFSM.AddState(new AIBS_Patrol());

            if (availableActions.wander)
            {
                actionFSM.AddState(new AIAS_Wander());
                actionFSM.AddState(new AIAS_MoveAlongCheckPoints());
            }
        }
    }
}