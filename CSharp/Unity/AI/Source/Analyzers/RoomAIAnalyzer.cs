using MageGame.Common.Utils;
using QPathFinder;
using System.Collections.Generic;
using UnityEngine;

namespace MageGame.AI.Analyzers
{
    /// <summary>
    /// Helps to find random free points in world where entities can go to.
    /// </summary>
    public class RoomAIAnalyzer
    {
        private RoomGraph waterRoomGraph;
        private RoomGraph airRoomGraph;

        public RoomAIAnalyzer()
        {
            waterRoomGraph = new RoomGraph();
            airRoomGraph = new RoomGraph();
        }

        public Vector3 FindFlyTarget(Vector3 position, float minRange, float maxRange)
        {
            return position + (Vector3)VectorUtil.GeneratePolarOffset(
                Random.Range(0f, 360f),
                Random.Range(minRange, maxRange)
            );
        }
    }

    public class RoomGraphNode
    {
        public int x;
        public int y;
        public QPathNodeFlags flags;
        public int lastEvaluatedAt;

        internal static string GetGraphNode(int x, int y)
        {
            return x + "_" + y;
        }
    }

    public class RoomGraph
    {
        private Dictionary<string, RoomGraphNode> nodes;

        internal RoomGraph()
        {
            nodes = new Dictionary<string, RoomGraphNode>();
        }
    }

    public class MoveOption
    {
        public RoomGraphNode node;
        public float priority;
    }
}