using GataryLabs.Localization;
using MageGame.Controls;
using MageGame.GUI;
using MageGame.Scripting.Data;
using MageGame.Scripting.Logic;
using MageGame.States.Conversations;
using MageGame.Utils;
using MageGame.World;
using UnityEngine;

namespace MageGame.Scripting.Level.General
{
    public class PlayerCommentScript : MonoBehaviour, ITriggerable
    {
        public PlayerComment comment;
        public PlayerCommentType commentType = PlayerCommentType.SpeechBubble;
        public float messageDuration = 1f;
        public TriggerActionNode action;

        public void Trigger()
        {
            string msg = "";

            GameObject speaker = PlayerControl.GetMainCharacter();

            switch (comment)
            {
                case PlayerComment.Curse:
                    msg = LocaUtil.CurseToText(ConversationActor.GetRandomCurse(speaker));
                    break;

                default:
                    msg = Loca.Text(comment.ToString(), LanguageCategory.PlayerComment);
                    break;
            }
            
            switch (commentType)
            {
                case PlayerCommentType.SpeechBubble:
                    Conversations.Shout(PlayerControl.GetMainCharacter(), msg, MageGame.Data.EmotionalMood.Neutral, messageDuration);
                    break;

                case PlayerCommentType.Notification:
                    GUINotification.ShowText(msg, messageDuration);
                    break;

                    case PlayerCommentType.Dialog:
                    PlayerControl.Say(msg);
                    break;
            }
            
            if (action.IsValid())
            {
                WorldScene.ActionTimeline.AddAction(action);
            }
        }

        #region editor
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            MageGameEditor.Utils.GizmoUtil.DrawIconDefault("Icon_Talk", transform.position);
        }
#endif
        #endregion
    }

    public enum PlayerCommentType
    {
        SpeechBubble = 0,
        Dialog       = 1,
        Notification = 2,
    }

    public enum PlayerComment
    {
        Default     = 0,
        Greetings   = 1,
        Curse       = 2,
        Whoopsi     = 3,
        Beautiful   = 4,
        Disgusting  = 5,
        Interesting = 6,
        Strange     = 7,
        Sorrowful   = 8,

        LetsSee  = 10,
        Done     = 11,
        Back     = 12,
    }
}