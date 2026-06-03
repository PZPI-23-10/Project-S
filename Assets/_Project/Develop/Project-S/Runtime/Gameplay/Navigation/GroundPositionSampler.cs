using UnityEngine;
using UnityEngine.AI;

namespace Project_S.Runtime.Gameplay.Navigation
{
    public static class GroundPositionSampler
    {
        private const float GroundProbeHeight = 25f;
        private const float GroundProbeDistance = 80f;
        private const float MaxNavMeshGroundDelta = 1.5f;
        private const int GroundLayerMask = 1 << 8;
        private const int DefaultLayerMask = 1;
        private const int IgnoreRaycastLayerMask = 1 << 2;

        public static Vector3 SampleGround(Vector3 position)
        {
            if (TrySampleGround(position, out Vector3 groundPosition))
                return groundPosition;

            return position;
        }

        public static Vector3 SampleGroundOrNavMesh(Vector3 position, float navMeshRadius = 4f)
        {
            if (TrySampleGround(position, out Vector3 groundPosition))
                return groundPosition;

            if (NavMesh.SamplePosition(position, out NavMeshHit navMeshHit, Mathf.Max(0.5f, navMeshRadius), NavMesh.AllAreas))
            {
                if (IsCloseToHeight(navMeshHit.position, position))
                    return navMeshHit.position;
            }

            return position;
        }

        public static Vector3 SampleNavMeshNearGround(Vector3 position, float searchRadius)
        {
            bool hasGround = TrySampleGround(position, out Vector3 groundPosition);
            Vector3 sampleOrigin = hasGround ? groundPosition : position;

            if (NavMesh.SamplePosition(sampleOrigin, out NavMeshHit hit, Mathf.Max(0.5f, searchRadius), NavMesh.AllAreas))
            {
                if (hasGround ? IsCloseToHeight(hit.position, groundPosition) : IsCloseToHeight(hit.position, position))
                    return hit.position;
            }

            if (hasGround)
                return groundPosition;

            return position;
        }

        private static bool TrySampleGround(Vector3 position, out Vector3 groundPosition)
        {
            Vector3 origin = position + Vector3.up * GroundProbeHeight;
            if (TryRaycastGround(origin, GroundLayerMask, out groundPosition))
                return true;

            if (TryRaycastGround(origin, GroundLayerMask | DefaultLayerMask, out groundPosition))
                return true;

            int broadGroundMask = ~IgnoreRaycastLayerMask;
            if (TryRaycastGround(origin, broadGroundMask, out groundPosition))
                return true;

            groundPosition = default;
            return false;
        }

        private static bool TryRaycastGround(Vector3 origin, int layerMask, out Vector3 groundPosition)
        {
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundProbeDistance, layerMask, QueryTriggerInteraction.Ignore))
            {
                groundPosition = hit.point;
                return true;
            }

            groundPosition = default;
            return false;
        }

        private static bool IsCloseToHeight(Vector3 sampledPosition, Vector3 referencePosition)
        {
            return Mathf.Abs(sampledPosition.y - referencePosition.y) <= MaxNavMeshGroundDelta;
        }
    }
}
