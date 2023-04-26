using MageGame.Behaviours.Ability;
using MageGame.Controls;
using MageGame.Data;
using MageGame.Scripting.Logic;
using UnityEngine;

namespace MageGame.Scripting.Level.General
{
    public class BossBattleScript : MonoBehaviour, ITriggerable
    {
        protected Coroutine scriptCoroutine; // rename to mainCoroutine
        protected Coroutine endCoroutine; // rename to mainCoroutine

        protected bool ceremonyRunning;

        #region de-/init
        virtual protected void Start()
        {
            
        }

        virtual protected void Deinit()
        {
            
        }
        #endregion

        public void Trigger()
        {
            scriptCoroutine = StartCoroutine(ExecuteActions());
        }

        virtual protected System.Collections.IEnumerator ExecuteActions()
        {
            //Debug.Log("Execute");
            yield return null;
        }

        virtual protected void WinBattle()
        {
            if (endCoroutine != null)
                StopCoroutine(endCoroutine);

            endCoroutine = StartCoroutine(UpdateWin());
            ceremonyRunning = true;
        }
        
        virtual protected System.Collections.IEnumerator UpdateWin()
        {
            yield return null;

            Cinematics.FocusOn(PlayerControl.GetMainCharacter());
            yield return new WaitForSeconds(.5f);

            CharacterScripts.PlayAnimation(PlayerControl.GetMainCharacter(), AnimationParamID.Partying);

            Vulnerability v = PlayerControl.GetMainCharacter()?.GetComponent<Vulnerability>();
            if (v != null && v.Health != null && !v.Health.IsMax())
            {
                v.Health.ToMax();
            }

            yield return new WaitForSeconds(2f);

            CharacterScripts.StopAnimation(PlayerControl.GetMainCharacter(), AnimationParamID.Partying);

            ceremonyRunning = false;
            endCoroutine = null;
        }

        virtual protected void LoseBattle()
        {
            ceremonyRunning = true;

            if (scriptCoroutine != null)
            {
                StopCoroutine(scriptCoroutine);
                scriptCoroutine = null;
            }

            if (endCoroutine != null)
                StopCoroutine(endCoroutine);

            endCoroutine = StartCoroutine(UpdateLose());
        }

        virtual protected System.Collections.IEnumerator UpdateLose()
        {
            yield return null;
            endCoroutine = null;
            ceremonyRunning = false;
        }

        #region utils

        protected void ShowBigHealthBar(GameObject boss)
        {

        }

        protected void HideBigHealthBar(GameObject boss)
        {

        }

        #endregion

        #region checks
        protected bool IsCeremonyRunning() => ceremonyRunning;
        protected bool IsCeremonyOver() => !ceremonyRunning;
        #endregion

        #region events

        private void OnDestroy()
        {
            Deinit();
        }

        #endregion

        #region editor
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            MageGameEditor.Utils.GizmoUtil.DrawIconDefault("Icon_BossBattle", transform.position);
        }
#endif
        #endregion
    }
}
