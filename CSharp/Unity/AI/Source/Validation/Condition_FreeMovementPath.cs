using UnityEngine;

namespace MageGame.AI.Validation
{
    public class Condition_FreeMovementPath : Condition
    {
        public PathAnalysis analysis;
        public GameObject actorObject;
        public Vector3 target;
        public AISkillValidation validation;

        public Condition_FreeMovementPath()
        {
            analysis = new PathAnalysis();
        }

        public Condition_FreeMovementPath(GameObject actorObject, AISkillValidation validation) : this()
        {
            this.actorObject = actorObject;
            this.validation = validation;
        }

        public override bool Check()
        {
            analysis.freePath = true; // reset, assume success
            analysis = validation.CheckPath(actorObject, actorObject.transform.position, target, analysis, validation.MovementParameters);
            return analysis.freePath;
        }

        public override bool IsMet()
        {
            return analysis.freePath;
        }
    }
}