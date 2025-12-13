using AIGraph;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ExtraToolCustomization
{
    public static class DoorUtil
    {
        private const float PortalCheckBuffer = 0.25f;
        private const float MinDotInNode = 0.1f;

        public static bool TryGetNearbyPortal(Vector3 pos, AIG_CourseNode node, [MaybeNullWhen(false)] out AIG_CoursePortal portal)
        {
            float bestDist = float.MaxValue;
            portal = null;
            foreach (var nodePortal in node.m_portals)
            {
                var diff = pos - nodePortal.Position;
                float distSqr = diff.sqrMagnitude;

                // Distance cap to prevent false positives when the door juts out (e.g. a hallway that extends partially into another room)
                nodePortal.m_cullPortal.GetShapeWidthHeight(out var width, out var height);
                width /= 2;
                height /= 2;
                width += PortalCheckBuffer;
                height += PortalCheckBuffer;
                if (distSqr < bestDist && distSqr < width * width + height * height)
                {
                    portal = nodePortal;
                    bestDist = distSqr;
                }
            }

            return portal != null;
        }

        public static bool IsOnSameSide(Vector3 pos, AIG_CourseNode checkNode, AIG_CoursePortal portal)
        {
            var cPortal = portal.m_cullPortal;
            var diffToPos = pos - cPortal.m_center;
            var dirIntoNode = cPortal.GetDirIntoNode(checkNode.m_cullNode);
            return Vector3.Dot(dirIntoNode, diffToPos) > MinDotInNode;
        }

        public static AIG_CourseNode GetCorrectNode(Vector3 pos, AIG_CourseNode node)
        {
            if (TryGetNearbyPortal(pos, node, out var portal))
            {
                var checkNode = portal.GetOppositeNode(node);
                if (IsOnSameSide(pos, checkNode, portal))
                    return checkNode;
            }
            return node;
        }
    }
}
