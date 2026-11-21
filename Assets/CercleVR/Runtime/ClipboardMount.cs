using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace CercleVR
{
    // VR reading is world-locked: head translation must produce natural parallax.
    // OpenXR alone drives the camera. The desktop fallback may remain camera-mounted.
    public sealed class ClipboardMount : MonoBehaviour
    {
        public Transform anchorOverride;
        public Vector3 viewOffset = new Vector3(-0.34f, -0.10f, 1.0f);
        public Vector3 anchorOffset = new Vector3(0, 0.02f, 0.16f);
        private const float CompactScale = 0.42f;
        private const float MinimumHandDistance = 0.65f;
        private static readonly Vector3 ReadingOffset = new Vector3(0, -0.10f, 1.35f);
        private static readonly Vector3 RestingOffset = new Vector3(-0.34f, -0.26f, 0.65f);
        public bool Reading { get; private set; }
        public event Action<bool> ReadingChanged;
        private bool needsPlacement = true;
        private InputAction recallAction;

        private void Start()
        {
            if (anchorOverride == null) return;
            var manager = anchorOverride.GetComponentInParent<InputActionManager>(true);
            if (manager == null) return;
            foreach (var asset in manager.actionAssets)
            {
                if (asset == null) continue;
                recallAction = asset.FindAction("XRI Left Interaction/Recall Clipboard");
                if (recallAction != null) break;
            }
        }

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
            bool keyboard = Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
            if (keyboard || (recallAction != null && recallAction.WasPressedThisFrame())) Recall();
        }

        private void LateUpdate() { AttachNow(); }

        public void AttachNow()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            var eye = camera.transform;
            if (camera.GetComponentInParent<XROrigin>() == null)
            {
                if (transform.parent != eye) transform.SetParent(eye, false);
                Vector3 offset = Reading ? new Vector3(0, -0.03f, 1f) : viewOffset;
                transform.SetLocalPositionAndRotation(offset, Quaternion.LookRotation(offset));
                transform.localScale = Vector3.one;
                return;
            }

            // A controller can be deactivated by the XR rig. The recall listener must stay alive.
            if (transform.parent != null) transform.SetParent(null, true);
            if (Reading && !needsPlacement) return;
            var guard = camera.GetComponentInParent<XrTrackingGuard>();
            if (guard != null && guard.enabled && !guard.HasValidPose) return;
            if (Reading)
            {
                var pose = ReadingPose(eye.position, eye.rotation, ReadingOffset);
                transform.SetPositionAndRotation(pose.position, pose.rotation);
                transform.localScale = Vector3.one;
                needsPlacement = false;
                return;
            }

            var rest = Quaternion.Euler(0f, eye.eulerAngles.y, 0f) * RestingOffset;
            var direction = anchorOverride != null && anchorOverride.gameObject.activeInHierarchy
                ? anchorOverride.TransformPoint(anchorOffset) - eye.position : rest;
            if (direction.sqrMagnitude < 0.02f) direction = rest;
            var position = eye.position + direction.normalized * Mathf.Max(MinimumHandDistance, direction.magnitude);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction, Vector3.up));
            transform.localScale = Vector3.one * CompactScale;
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
