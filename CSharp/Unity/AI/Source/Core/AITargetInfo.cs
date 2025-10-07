using MageGame.AI.Data;
using MageGame.AI.Pathes;
using MageGame.AI.World;
using MageGame.Behaviours.Ability;
using MageGame.Behaviours.Misc;
using MageGame.Behaviours.Relationships;
using MageGame.Common.Data;
using MageGame.Geometry.Utils;
using MageGame.Movement.Locomotion.Behaviours;
using MageGame.Stats;
using MageGame.World.Core.Data;
using UnityEngine;
using static MageGame.AI.States.Simple.AIInterestZone;

namespace MageGame.AI.Core
{
    public class AITargetInfo
    {
        // components (fast access, mini api)
        public GameObject gameObject;
        public ObjectClassification classification;
        public Vulnerability vulnerability;
        public CharacterController2D charControl;
        public Rigidbody2D rigidBody;
        public CharacterMovement movement;
        public ActionController actions;
        public Collider2D collider;
        public IdentificationFriendOrFoe IFF;
        public RangedStat health;
        public RangedStat magic;
        public Transform targettingOriginTRF;

        // evaluation
        public sbyte relation;
        public ThreatLevel threat;
        public AIHandlingNote note;
        public AIInterestZoneID zone;
        public short priority;
        
        public Vector3 center;
        public Bounds anticipatedBounds;
        public Vector3 anticipatedPosition;

        public Vector3 Position => gameObject.transform.position;
        public Vector3 TargettingOrigin => targettingOriginTRF != null ? targettingOriginTRF.transform.position : gameObject.transform.position;
        
        public AITargetInfo() { }

        public AITargetInfo Analyze(GameObject gameObject)
        {
            this.gameObject = gameObject;

            if (gameObject == null)
            {
                return null;
            }

            WorldSceneAICacheNode node = WorldSceneAICache.Instance.Resolve(gameObject);

            classification = node.classification;
            rigidBody = node.rigidBody;
            collider = node.collider;
            vulnerability = node.vulnerability;
            IFF = node.IFF;
            charControl = node.charControl;
            movement = node.movement;
            actions = node.actions;
            health = node.health;
            magic = node.magic;

            if (actions != null && actions.targettingOrigin != null)
                targettingOriginTRF = actions.targettingOrigin.transform;
            else
                targettingOriginTRF = gameObject.transform;

            note = AIHandlingNote.Unknown;
            priority = 0;

            Update();
            return this;
        }

        public AITargetInfo Clone()
        {
            AITargetInfo info = new AITargetInfo();

            info.gameObject = this.gameObject;
            info.classification = this.classification;
            info.collider = this.collider;
            info.health = this.health;
            info.magic = this.magic;
            info.movement= this.movement;
            info.actions= this.actions;
            info.charControl = this.charControl;
            info.IFF = this.IFF;
            info.vulnerability = this.vulnerability;
            info.rigidBody = this.rigidBody;
            info.zone = this.zone;
            info.targettingOriginTRF = this.targettingOriginTRF;

            return info;
        }

        internal bool IsStatic()
        {
            return gameObject.tag != GameObjectTag.Animal && gameObject.tag != GameObjectTag.Character;
        }

        public AITargetInfo Update()
        {
            if (charControl != null)
                center = gameObject.transform.position + charControl.Center;
            else
                center = gameObject.transform.position;

            return this;
        }

        internal void Forget()
        {
            gameObject = null;
            note = AIHandlingNote.Unknown;
        }

        public Collider2D GetCollider()
        {
            return gameObject.GetComponent<Collider2D>();
        }

        public bool IsAThreat()
        {
            return threat != ThreatLevel.Harmless;
        }
        
        public override string ToString()
        {
            return gameObject != null ? $"[{gameObject.name} {priority}]" : "[empty]";
        }

        public Vector2 Velocity
        {
            get
            {
                if (charControl != null) return charControl.velocity;
                if (rigidBody   != null) return rigidBody.velocity;

                return Vector2.zero;
            }
        }
    }

    static public class AITargetInfoUtil
    {
        static public bool IsDefeated(this AITargetInfo info) => info.health != null && info.health.IsMin();

        static public void AnticipateBounds(this AITargetInfo info)
        {
            if (info.collider != null)
                info.anticipatedBounds = info.collider.bounds;
        }

        static public void AnticipateBounds(this AITargetInfo info, float futureTime, float accuracy)
        {
            if (info.collider != null && info.movement != null)
                info.anticipatedBounds = PathComputation.ComputeTargetBounds(info.collider.bounds, info.movement.VelocityPerSecond, futureTime, accuracy);
        }
        
        static public void AnticipatePosition(this AITargetInfo info, float futureTime, float accuracy)
        {
            if (info.collider != null && info.movement != null)
                info.anticipatedPosition = PathComputation.ComputeTargetPosition(info.center, info.movement.VelocityPerSecond, futureTime, accuracy);
        }

        static public bool Exists(this AITargetInfo info)
        {
            return info != null && info.gameObject != null;
        }
        
        static public bool IsMoving(this AITargetInfo info)
        {
            return info.movement != null && info.movement.VelocityPerSecond.sqrMagnitude > .01f;
        }
        
        static public Vector3 GetLocalCenter(this AITargetInfo info)
        {
            return info.charControl.Center;
        }

        static public float DetermineDistance(AITargetInfo a, AITargetInfo b)
        {
            return (a.Position - b.Position).magnitude;
        }

        static public int CompareInfosOnPriorityDesc(AITargetInfo a, AITargetInfo b)
        {
            int i = a.priority - b.priority;
            if (i == 0)
                return i;
            else
                return (i < 0) ? 1 : -1;
        }

        static public float DetermineAngleToTarget(AITargetInfo a, AITargetInfo b)
        {
            if (b != null && b.gameObject != null)
                return GeomUtil.GetAngleBetweenVectors(a.TargettingOrigin, b.center);

            return 0f;
        }

        static public float DetermineFutureAngleToTarget(AITargetInfo a, AITargetInfo b, float skillProjectileSpeed, float accuracy)
        {
            if (b != null && b.gameObject != null)
                return PathComputation.ComputeTargetAngle(a.TargettingOrigin, b.center, skillProjectileSpeed, b.Velocity, accuracy);

            return 0f;
        }

        static public Vector3 DetermineFuturePositionOfTarget(AITargetInfo a, AITargetInfo b, float skillProjectileSpeed, float accuracy)
        {
            if (b != null && b.gameObject != null)
                return PathComputation.ComputeTargetPosition(a.center, b.center, skillProjectileSpeed, b.Velocity, accuracy);

            return Vector3.zero;
        }

        static public sbyte DetermineSideToTarget(AITargetInfo a, AITargetInfo b)
        {
            if (b != null && b.gameObject != null)
                return GeomUtil.GetPointingSide(b.anticipatedBounds.center.x - a.center.x);

            return 0;
        }
    }
}