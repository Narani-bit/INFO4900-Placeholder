using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace CercleVR
{
    // A floor ray hit alone is insufficient: a chair or table may occupy the landing space.
    public sealed class SafeTeleportationArea : TeleportationArea
    {
        public Transform playerRig;
        [Min(0.1f)] public float clearanceRadius = 0.25f;
        [Min(0.5f)] public float clearanceHeight = 1.8f;

        public bool IsDestinationClear(Vector3 floorPosition)
        {
            float radius = Mathf.Max(0.1f, clearanceRadius);
            float height = Mathf.Max(radius * 2, clearanceHeight);
            var bottom = floorPosition + Vector3.up * (radius + 0.05f);
            var top = floorPosition + Vector3.up * (height - radius + 0.05f);
            foreach (var hit in Physics.OverlapCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                if (playerRig != null && (hit.transform == playerRig || hit.transform.IsChildOf(playerRig))) continue;
                return false;
            }
            return true;
        }

        protected override bool GenerateTeleportRequest(IXRInteractor interactor, RaycastHit hit, ref TeleportRequest request)
        {
            return base.GenerateTeleportRequest(interactor, hit, ref request) && IsDestinationClear(request.destinationPosition);
        }

        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            if (!base.IsSelectableBy(interactor)) return false;
            return !(interactor is XRRayInteractor ray) ||
                (ray.TryGetCurrent3DRaycastHit(out var hit) && IsDestinationClear(hit.point));
        }
    }
}
