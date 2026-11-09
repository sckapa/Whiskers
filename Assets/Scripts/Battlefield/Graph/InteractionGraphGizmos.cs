using UnityEngine;
using Whiskers.Core;
using Whiskers.Dice.Rolling;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Whiskers.Battlefield
{
    // Draws the interaction graph over the tray in the Scene view (and the Game view with Gizmos on).
    // Called from BattlefieldManager.OnDrawGizmos, nothing is drawn in a build.
    public static class InteractionGraphGizmos
    {
        private const float DrawHeight = 0.2f;
        private const float NodeRadius = 0.12f;
        private const float MinimumEdgeWidth = 4f;
        private const float Half = 0.5f;

        private static readonly Color PlayerNodeColor = new Color(0.3f, 0.8f, 1f);
        private static readonly Color EnemyNodeColor = new Color(1f, 0.4f, 0.3f);
        private static readonly Color AlliedEdgeColor = new Color(0.4f, 1f, 0.4f, 0.6f);
        private static readonly Color OpposingEdgeColor = new Color(1f, 0.6f, 0.2f, 0.6f);
        private static readonly Color ResolvedEdgeColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

        // Edges: green = allied, orange = opposing, grey = resolved this roll, thick yellow = next to be used.
        // Nodes: blue = player, red = enemy, labelled with id and value.
        public static void Draw(BattlefieldBoard board, DiceTray tray, bool drawDistances)
        {
            if (board == null || tray == null)
            {
                return;
            }

            InteractionGraph graph = board.Graph;
            graph.TryGetMinimumEdge(out InteractionEdge minimum);

            foreach (InteractionEdge edge in graph.Edges)
            {
                DrawEdge(board, tray, edge, edge == minimum, drawDistances);
            }

            foreach (BattlefieldObject node in graph.Nodes)
            {
                DrawNode(board, tray, node);
            }
        }

        private static void DrawEdge(BattlefieldBoard board, DiceTray tray, InteractionEdge edge, bool isMinimum, bool drawDistance)
        {
            Vector3 from = ToWorld(board, tray, edge.First.Position);
            Vector3 to = ToWorld(board, tray, edge.Second.Position);
            Color color = edge.IsResolved ? ResolvedEdgeColor : edge.IsOpposing ? OpposingEdgeColor : AlliedEdgeColor;

            if (isMinimum)
            {
                color = Color.yellow;
            }

            Gizmos.color = color;
            Gizmos.DrawLine(from, to);

#if UNITY_EDITOR
            if (isMinimum)
            {
                Handles.color = color;
                Handles.DrawAAPolyLine(MinimumEdgeWidth, from, to);
            }

            if (drawDistance)
            {
                Handles.color = color;
                Handles.Label(Vector3.Lerp(from, to, Half), edge.Distance.ToString("0.00"));
            }
#endif
        }

        private static void DrawNode(BattlefieldBoard board, DiceTray tray, BattlefieldObject node)
        {
            Vector3 position = ToWorld(board, tray, node.Position);

            Gizmos.color = node.Owner == CombatSide.Player ? PlayerNodeColor : EnemyNodeColor;
            Gizmos.DrawSphere(position, NodeRadius);

#if UNITY_EDITOR
            Handles.Label(position + Vector3.up * NodeRadius, $"#{node.Id} v{node.Value}");
#endif
        }

        private static Vector3 ToWorld(BattlefieldBoard board, DiceTray tray, Vector2 boardPosition)
        {
            return tray.FromNormalized(board.ToNormalized(boardPosition), DrawHeight);
        }
    }
}
