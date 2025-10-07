using MageGame.AI.Pathes;

namespace MageGame.AI.Data
{
    public class AIPathActionParameters : AIActionParameters
    {
        public IAIPath path;
        public bool takeNearestNode;
        public AIPathDirection direction;
        public bool cyclic;
        public bool approachAfterwards;
        public bool ignoreFreeSkillPath;

        static public AIPathActionParameters Create(IAIPath path, AIPathDirection direction = AIPathDirection.Forth, bool takeNearestNode=false, bool cyclic=false)
        {
            AIPathActionParameters parameters = new AIPathActionParameters();
            parameters.path = path;
            parameters.takeNearestNode = takeNearestNode;
            parameters.direction = direction;
            parameters.cyclic = cyclic;
            return parameters;
        }
    }
}
