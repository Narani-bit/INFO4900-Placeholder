using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CercleVR
{
    // VR reading is world-locked: head translation must produce natural parallax.
    // OpenXR alone drives the camera. The desktop fallback may remain camera-mounted.
    public sealed class ClipboardMount : MonoBehaviour
    {
        public Transform anchorOverride;
        public Vector3 viewOffset = new Vector3(-0.34f, -0.10f, 1.0f);
        public Vector3 anchorOffset = new Vector3(0, 0.15f, 0.08f);
        public Vector3 anchorEuler = new Vector3(50, 0, 0);
        public bool Reading { get; private set; }
        public event Action<bool> ReadingChanged;
        private bool needsPlacement = true;
        private bool recallWasPressed;
        private Transform lastAnchor;

        public void ToggleReading()
        {
            Reading = !Reading;
            needsPlacement = true;
            AttachNow();
            ReadingChanged?.Invoke(Reading);
        }

        public void Recall()
        {
            Reading = true;
            needsPlacement = true;
            AttachNow();
            ReadingChanged?.Invoke(Reading);
        }

        private void Update()
        {
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool pressed = left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool button) && button;
            bool keyboard = Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
            if ((keyboard || (pressed && !recallWasPressed)) && XrTrackingGuard.CanInteract) Recall();
            recallWasPressed = pressed;
        }

        private void LateUpdate() { AttachNow(); }

        public void AttachNow()
        {
            bool useHand = anchorOverride != null && !Reading;
            Camera camera = Camera.main;
            Transform anchor = useHand ? anchorOverride : camera != null ? camera.transform : null;
            if (anchor == null) return;
            if (lastAnchor != anchor) { needsPlacement = true; lastAnchor = anchor; }
            if (!useHand && camera.GetComponentInParent<XROrigin>() != null)
            {
                if (transform.parent != null) { transform.SetParent(null, true); needsPlacement = true; }
                if (needsPlacement)
                {
                    var guard = camera.GetComponentInParent<XrTrackingGuard>();
                    if (guard != null && guard.enabled && !guard.HasValidPose) return;
                    Vector3 offset = Reading ? new Vector3(0, -0.03f, 1f) : viewOffset;
                    Pose pose = ReadingPose(anchor.position, anchor.rotation, offset);
                    transform.SetPositionAndRotation(pose.position, pose.rotation);
                    transform.localScale = Vector3.one;
                    needsPlacement = false;
                }
                return;
            }
            if (transform.parent != anchor) transform.SetParent(anchor, false);
            transform.localScale = Vector3.one;
            if (useHand)
            {
                transform.localPosition = anchorOffset;
                transform.localRotation = Quaternion.Euler(anchorEuler);
            }
            else
            {
                Vector3 offset = Reading ? new Vector3(0, -0.03f, 1f) : viewOffset;
                transform.localPosition = offset;
                transform.localRotation = Quaternion.LookRotation(offset);
            }
        }

        public static Pose ReadingPose(Vector3 eyePosition, Quaternion eyeRotation, Vector3 offset)
        {
            // Use the horizontal heading, not head pitch/roll; keep the page upright.
            Vector3 forward = Vector3.ProjectOnPlane(eyeRotation * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.Cross(eyeRotation * Vector3.right, Vector3.up);
            Quaternion heading = Quaternion.LookRotation(forward.normalized, Vector3.up);
            offset.z = Mathf.Max(1f, offset.z);
            Vector3 direction = heading * offset;
            return new Pose(eyePosition + direction, Quaternion.LookRotation(direction, Vector3.up));
        }
    }
}
