using System.Collections.Generic;
using UnityEngine;
using MageGame.Utils;
using MageGame.World;

namespace MageGame.Effects
{
    /// <summary>
    /// 
    /// This behaviour manages effect application.
    /// 
    /// If a game object enters a region that causes an effect
    /// - the effect shall not be re-applied when still active and game object re-enters the region
    /// - the effect shall be stopped when game object leaves the region and the effect is bound to contact (non-durational)
    /// - the effect shall be re-applied when game object stays in region and the effect is duration-based and over
    /// 
    /// </summary>
    public class SteadyEffectApplier : MonoBehaviour
    {
        // constants
        private const float defaultInterval = 1f;

        // configuration
        public IEffectCauser causer;
        public GameObject causerObject;
        public EffectData effectData;
        public SharedEffectContext sharedContext;
        public float effectInterval;
        public bool intervalBasedEffects;

        // state
        private Dictionary<GameObject, AffectedEntry> affectees;
        private bool needsUpdate;

        #region de-/init
        private void Awake()
        {
            affectees = new Dictionary<GameObject, AffectedEntry>();
        }

        private void Start()
        {
            if (effectData != null && effectData.duration > 0f)
            {
                intervalBasedEffects = true;
                effectInterval = effectData.duration + .1f;
            }
            else
                effectInterval = defaultInterval;
        }
        #endregion

        #region state change
        private void Update()
        {
            if (!needsUpdate)
                return;

            float delta = GameTime.deltaTime;

            List<GameObject> affectedTargets = new List<GameObject>(affectees.Keys);

            for (int i=0; i<affectedTargets.Count; i++)
            {
                AffectedEntry entry = affectees[affectedTargets[i]];

                if ((intervalBasedEffects || entry.failed) && (entry.timeRemaining -= delta) <= 0)
                {
                    StopEffectOf(affectedTargets[i], true);
                }
            }
        }

        public void StartEffectFor(GameObject target)
        {
            //Debug.Log("add fx " + effectData.type + " to " + target.name);
            // has no effect yet, let's create it
            EffectController effectController = target.AssureExistence<EffectController>();

            AffectedEntry entry = new AffectedEntry();
            entry.timeRemaining = intervalBasedEffects ? effectInterval : -1f;
            affectees[target] = entry;

            if (!effectController.HasEffectFrom(causer))
            {
                if (effectData.DiceChance())
                    effectController.CreateAndAdd(effectData, new EffectContext(target, gameObject, effectData.GetTimingType(), sharedContext, EffectCauserType.Misc), causer);
                else
                    entry.failed = true;
            }

            needsUpdate = true;
        }

        public void StopEffectOf(GameObject target, bool stoppedAlready=false)
        {
            //Debug.Log("rem fx " + effectData.type + " fr " + (target != null ? target.name : "DESTROYED"));

            AffectedEntry entry = affectees.ContainsKey(target) ? affectees[target] : null;

            if (target != null)
            {
                EffectController fxController = target.GetComponent<EffectController>();

                if (fxController != null)
                {
                    if (fxController.HasEffectFrom(causer))
                    {
                        if  (stoppedAlready)
                            Debug.LogWarning("Effect was not stopped yet.");

                        fxController.RemoveEffectFrom(causer);
                    }

                    // otherwise is normal.
                }
            }

            if (target != null && !affectees.ContainsKey(target))
            {
                Debug.LogWarning("Cannot stop what not exists: " + target.name + " by " + gameObject.name);
            }
            else
            {
                affectees.Remove(target);
            }

            needsUpdate = affectees.Count > 0;
        }

        public void StopAllEffects()
        {
            List<GameObject> affecteeKeys = new List<GameObject>(affectees.Keys);
            foreach(GameObject go in affecteeKeys)
            {
                StopEffectOf(go);
            }
        }
        #endregion

        #region checks
        public bool IsAppliedInThisInterval(GameObject target)
        {
            return affectees.ContainsKey(target);
        }
        #endregion
    }

    public class AffectedEntry
    {
        public float timeRemaining;
        public bool failed;
    }
}