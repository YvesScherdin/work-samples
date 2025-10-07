using MageGame.Items.Filters;
using MageGame.Mechanics.Statuses.Data;
using MageGame.Scenes.Gateways;
using System.Collections.Generic;
using UnityEngine;

namespace MageGame.AI.Data
{
    public class AIAwareness
    {
        public AIMemory memory = new AIMemory();
        public List<AIDangerInfo> dangers = new List<AIDangerInfo>();
        public StatusFlags problematicStatuses = StatusFlags.None;
        public List<GameObject> allStuffNoticed = new List<GameObject>();
        public HashSet<GameObject> knownStuff = new HashSet<GameObject>();
        public HashSet<GameObject> stuffToIgnore = new HashSet<GameObject>();
        public ISceneGateway currentGateway;

        /// <summary>
        /// Agent cares only about these items and will not obtain/steal any other.
        /// </summary>
        public IItemFilter itemPreferences;

        public GameObject primaryTarget = null;
        public bool targetLost;

        public AIActionOutcome lastActionOutcome;
    }

    static public class AIAwarenessExtensions
    {
        static public bool IsLastOutcomeOfType(this AIAwareness awareness, AIActionOutcomeType type)
        {
            return awareness.lastActionOutcome != null && awareness.lastActionOutcome.type == type;
        }

        static internal void Notice(this AIAwareness awareness, GameObject go)
        {
            awareness.allStuffNoticed.Add(go);
        }

        static internal void NoticeSafe(this AIAwareness awareness, GameObject go)
        {
            if (!awareness.allStuffNoticed.Contains(go))
                awareness.allStuffNoticed.Add(go);
        }

        static internal void Unnotice(this AIAwareness awareness, GameObject go)
        {
            awareness.allStuffNoticed.Remove(go);

            if (go == awareness.primaryTarget && go != null)
            {
                awareness.targetLost = true;
            }

            awareness.knownStuff.Remove(go);
        }

        static public void Ignore(this AIAwareness awareness, GameObject go)
        {
            awareness.stuffToIgnore.Add(go);
        }
        
        static public void Unignore(this AIAwareness awareness, GameObject go)
        {
            awareness.stuffToIgnore.Remove(go);
        }

        static internal void NoteOutcome(this AIAwareness awareness, AIActionOutcome outcome)
        {
            awareness.lastActionOutcome = outcome;
        }

        static internal void NoteOutcome(this AIAwareness awareness, AIActionOutcomeType type, GameObject target)
        {
            awareness.lastActionOutcome = new AIActionOutcome();
            awareness.lastActionOutcome.type = AIActionOutcomeType.Killed;
            awareness.lastActionOutcome.target = target;
        }
        
        static internal void ForgetOutcome(this AIAwareness awareness)
        {
            awareness.lastActionOutcome = null;
        }

        static internal bool HasProblematicStatus(this AIAwareness awareness, StatusFlags statusFlag)
        {
            return awareness.problematicStatuses.HasFlag(statusFlag);
        }
        
        static internal bool AddProblematicStatus(this AIAwareness awareness, StatusFlags statusFlag)
        {
            if ((awareness.problematicStatuses & statusFlag) == 0)
            {
                awareness.problematicStatuses |= statusFlag;
                return true;
            }
            return false;
        }
        
        static internal bool RemoveProblematicStatus(this AIAwareness awareness, StatusFlags statusFlag)
        {
            if ((awareness.problematicStatuses & statusFlag) == 0)
                return false;

            awareness.problematicStatuses &= ~statusFlag;
            return true;
        }

        static internal void Clear(this AIAwareness awareness)
        {
            awareness.primaryTarget = null;
            awareness.dangers.Clear();
            awareness.allStuffNoticed.Clear();
            awareness.knownStuff.Clear();
            awareness.lastActionOutcome = null;
        }
    }
}