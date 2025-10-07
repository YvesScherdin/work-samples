using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.Validation;
using MageGame.Common.Data;
using MageGame.World.Core.Data;
using System;
using UnityEngine;

namespace MageGame.AI.Agents.Default
{
    public class AIAnalyzer_Default : AIAnalyzer
    {
        public override void Init()
        {
            base.Init();

            tagHooks.Add(GameObjectTag.Animal, AnalyzeBasically);
            tagHooks.Add(GameObjectTag.Character, AnalyzeBasically);
        }

        public override AITargetInfo AnalyzeBasically(AITargetInfo info, AITargetAnalysisParameter parameters, AIAgentContext context)
        {
            info.relation = context.myObjectInfo.IFF.GetRelation(info.gameObject);
            info.threat = ThreatUtil.DetermineThreatLevel(info, context.myObjectInfo.IFF, context.militaryUnit);

            if(info.threat == ThreatLevel.Harmless && parameters.HasFlag(AITargetAnalysisParameter.Attacked))
            {
                if (info.relation <= 0)
                {
                    // ignore friendly fire
                    info.threat = ThreatLevel.Low;

                    if (!context.myObjectInfo.IFF.IsConcreteEnemy(info.gameObject))
                    {
                        context.myObjectInfo.IFF.ToggleEnemy(info.gameObject, true);
                    }
                }
            }

            float distance = AITargetInfoUtil.DetermineDistance(context.myObjectInfo, info);
            info.priority -= (short)(distance * 100);

            if (info.IsAThreat())
            {
                info.priority += (short)((short)info.threat * (short)1000);

                if (info.health != null)
                {
                    info.priority += Math.Min((short)(info.health.TotalValue / 10f), (short)10);
                }
                else
                {
                    Debug.LogWarning("detected invincible damage dealer. Top prio! " + info.gameObject);
                    info.priority += 1000;
                }

                skillPathFree.skill = context.actions.CurrentSkill;
                skillPathFree.targetInfo = info;

                if (!skillPathFree.Check())
                {
                    info.priority -= 10000;
                }
            }
            else
            {
                return null;
            }

            return info;
        }

    }
}
