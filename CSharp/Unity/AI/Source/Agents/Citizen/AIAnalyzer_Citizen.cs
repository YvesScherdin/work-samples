using MageGame.AI.Core;
using MageGame.AI.Data;
using MageGame.AI.Validation;
using MageGame.Common.Data;
using MageGame.World.Core.Data;

namespace MageGame.AI.Agents.Default
{
    public class AIAnalyzer_Citizen : AIAnalyzer
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

            if (info.threat == ThreatLevel.Harmless && parameters.HasFlag(AITargetAnalysisParameter.Attacked))
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
                info.priority += 1000;
            }
            else
            {
                return null;
            }

            return info;
        }

    }
}
