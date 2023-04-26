using MageGame.AI.Actors.Battle;
using MageGame.AI.Agents;
using MageGame.Behaviours.Ability;
using MageGame.Behaviours.Environment;
using MageGame.Behaviours.Spawning;
using MageGame.Controls;
using MageGame.Data;
using MageGame.Data.World;
using MageGame.Events;
using MageGame.Scripting.Level.General;
using MageGame.Utils;
using MageGame.World;
using MageGame.World.Cameras;
using System.Collections;
using UnityEngine;

namespace MageGame.Scripting.Level.ClayPit
{
    /// <summary>
    /// Boss level script - Boss Habumir in Fortress under the claypit.
    /// </summary>
    public class LS_ClayPit_Boss_A : BossBattleScript
    {
        [Header("Development")]
        public bool skipDialog;
        public bool skipFight1;
        public bool skipDialog2;
        public bool skipBossBattleSelf;
        public bool noAudio;
        
        [Header("Specific")]
        public float spawnDelay = .5f;
        public float timerCam1 = .5f;
        public float timerBetweenDialogs= .5f;
        public WorldSceneVariableID wonVariable;

        [Header("Audio")]
        public AudioClip themePrelude;
        public AudioClip themeBattle;
        public AudioClip themeVictory;

        [Header("Entities")]
        public BackgroundBoss_Habumir bossMage;
        public MechanicalObstacle gateToTheRight;
        public SpawnWaveController spawnWaves;
        public MechanicalObstacle bigWall;
        public BreakableStructure secretWall;

        [Header("Camera")]
        public Vector2 bossDeathSceneBounds;
        public CameraRegion camRegionThrone;
        public CameraRegion camRegionArrival;
        public CameraRegion camRegionSecretPassage;

        // internal
        private bool bossFallen = false;
        private AIAgent bossAgent;

        #region de-/init
        override protected void Start()
        {
            if (!noAudio)
                AudioUtil.PlayTheme(themePrelude);

            GlobalEventManager.HeroDeathEvent.AddListener(HandleHeroCharacterDeath);
        }

        override protected void Deinit()
        {
            GlobalEventManager.HeroDeathEvent.RemoveListener(HandleHeroCharacterDeath);
            Cinematics.CameraMode = CameraWorkMode.Default;
        }
        #endregion

        protected override IEnumerator ExecuteActions()
        {
            yield return null;

            gateToTheRight.TriggerState(MageGame.Data.OperationState.Off, false);
            Cinematics.CameraMode = CameraWorkMode.Calm;

            // move camera to mage
            if (!skipDialog)
            {
                Cinematics.Enter();
                yield return null;

                Cinematics.FocusOn(BossCharacter, true, 1f);

                yield return new WaitUntil(Cinematics.IsCameraTransitionOver); // BUG: does not wait long enough
                //Debug.Log("Wait for " + timerCam1);
                yield return new WaitForSeconds(timerCam1);
                Conversations.Say(BossCharacter, StoryTextID.CLP_Habumir_Greet1);

                yield return new WaitForSeconds(timerBetweenDialogs);
                Conversations.Say(PlayerControl.GetMainCharacter(), StoryTextID.CLP_Habumir_Greet2);

                yield return new WaitForSeconds(timerBetweenDialogs);
                Conversations.Say(BossCharacter, StoryTextID.CLP_Habumir_Greet3);

                yield return new WaitForSeconds(timerBetweenDialogs);

                Cinematics.Leave();
            }

            bigWall.TriggerState(OperationState.On, false);

            if (!noAudio)
                AudioUtil.PlayTheme(themeBattle);

            if (!skipFight1)
            {
                // minion fight
                yield return new WaitForSeconds(spawnDelay);

                spawnWaves.Go();

                yield return new WaitUntil(spawnWaves.IsFinished);

                Debug.Log("Spawn waves over-");

                yield return new WaitForSeconds(1f);

                AudioUtil.StopTheme();
                yield return new WaitForSeconds(.5f);
            }

            if (!skipDialog2)
            {
                // boss fight
                Cinematics.Enter();
                yield return new WaitForSeconds(.5f);

                Cinematics.FocusOn(camRegionThrone, CameraTimingType.Medium);
                yield return new WaitUntil(Cinematics.IsCameraTransitionOver);
                yield return new WaitForSeconds(.5f);

                Conversations.Say(BossCharacter, StoryTextID.CLP_Habumir_MinionsSlain); // "All minions slain?"
                bossMage.StandUp();
                yield return new WaitForSeconds(1.5f);

                Conversations.Say(BossCharacter, StoryTextID.CLP_Habumir_SelfInfolvement); // "Now it's my turn!"
                yield return new WaitForSeconds(1f);

                Cinematics.FocusOn(camRegionArrival, CameraTimingType.Slow);
            }

            if (!skipBossBattleSelf)
            {
                bossMage.FlyToFront();

                yield return new WaitUntil(bossMage.HasReachedFront);
                yield return new WaitForSeconds(.5f);

                bossAgent = bossMage.TurnIntoFullFledgedCharacter();
                bossAgent.AIEvent.AddListener(HandleAIEventOfBoss);

                yield return new WaitForSeconds(.5f);

                if (Cinematics.IsModeActive())
                    Cinematics.Leave();

                yield return new WaitUntil(HasBossFallen);
            }

            // boss is dead

            if (!noAudio)
                AudioUtil.FadoutTheme(1.5f);

            Cinematics.PreventAnyDamage();
            yield return new WaitForSeconds(1f);

            Cinematics.Enter();
            yield return null;

            Cinematics.FocusOn(BossCharacter, bossDeathSceneBounds, 1f);
            yield return new WaitUntil(Cinematics.IsCameraTransitionOver);
            yield return new WaitForSeconds(1f);

            Conversations.Say(PlayerControl.GetMainCharacter(), StoryTextID.CLP_Habumir_BossSlain); // "He's dead!"
            yield return new WaitForSeconds(.5f);

            yield return new WaitForSeconds(.5f);

            // win
            //Debug.Log("Won");

            if (!noAudio && themeVictory != null)
                AudioUtil.PlayTheme(themeVictory, 1f, false);

            WinBattle();
            yield return new WaitUntil(IsCeremonyOver);

            if (secretWall != null)
            {
                yield return new WaitForSeconds(.5f);

                Cinematics.FocusOn(camRegionSecretPassage);
                yield return new WaitUntil(Cinematics.IsCameraTransitionOver);
                yield return new WaitForSeconds(.5f);
                secretWall.Break();
                yield return new WaitForSeconds(1f);
                Cinematics.RevertFocus();
                yield return new WaitForSeconds(.5f);
            }

            Cinematics.Leave();
            Deinit();
        }

        protected override void WinBattle()
        {
            WorldScene.Status.SetVariableBool(wonVariable, true);

            base.WinBattle();
        }

        protected override IEnumerator UpdateLose()
        {
            yield return new WaitForSeconds(.5f);

            if (!HasBossFallen())
            {
                Conversations.Say(BossCharacter, StoryTextID.CLP_Habumir_HeroSlain);
            }

            Deinit();
        }

        private GameObject BossCharacter => bossAgent != null ? bossAgent.gameObject : bossMage.gameObject;

        #region checks
        private bool HasBossFallen()
        {
            return bossFallen;
        }
        #endregion

        #region events
        private void HandleAIEventOfBoss(AIEventType type)
        {
            if(type == AIEventType.Died)
            {
                bossFallen = true;
            }
        }

        private void HandleHeroCharacterDeath(DeathConsequence consequence)
        {
            if (consequence == DeathConsequence.TheEnd)
            {
                LoseBattle();
            }
        }
        #endregion

    }
}