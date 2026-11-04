using UnityEngine;
using UnityEngine.XR;

namespace CercleVR
{
    // Validate the runtime's fused pose; never integrate raw IMU data over OpenXR.
    [DisallowMultipleComponent]
    public sealed class XrTrackingGuard : MonoBehaviour
    {
        public bool HasValidPose
        {
            get
            {
                var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                bool hasTracked = head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked);
                bool hasState = head.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state);
                return IsPoseValid(head.isValid, hasTracked, tracked, hasState, state);
            }
        }

        public static bool CanInteract
        {
            get
            {
                var camera = Camera.main;
                var guard = camera != null ? camera.GetComponentInParent<XrTrackingGuard>() : null;
                return guard == null || !guard.enabled || guard.HasValidPose;
            }
        }

        public static bool IsPoseValid(bool deviceValid, bool hasTracked, bool tracked,
            bool hasState, InputTrackingState state)
        {
            const InputTrackingState required = InputTrackingState.Position | InputTrackingState.Rotation;
            return deviceValid && (!hasTracked || tracked) && hasState && (state & required) == required;
        }
    }
}
