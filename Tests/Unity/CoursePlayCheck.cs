using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;
using CercleVR;
public static class CoursePlayCheck
{
    public static async Task<object> Main()
    {
        int firstFrame = Time.frameCount;
        var game = UnityEngine.Object.FindAnyObjectByType<CercleGame>();
        if (game == null) throw new Exception("Game missing.");
        var mount = UnityEngine.Object.FindAnyObjectByType<ClipboardMount>();
        var camera = Camera.main;
        if (mount == null || camera == null) throw new Exception("Reading objects missing.");
        mount.Recall();
        var origin = camera.GetComponentInParent<XROrigin>();
        bool worldPinned = origin != null;
        float initialDistance = Vector3.Distance(mount.transform.position, camera.transform.position);
        if (initialDistance < 0.99f) throw new Exception("Reading too close.");
        if (worldPinned)
        {
            if (mount.transform.parent != null) throw new Exception("VR clipboard head-locked.");
            var savedPosition = origin.transform.position;
            var savedRotation = origin.transform.rotation;
            var boardPosition = mount.transform.position;
            var boardRotation = mount.transform.rotation;
            try
            {
                origin.transform.position += new Vector3(0.3f, 0, 0);
                origin.transform.rotation = Quaternion.AngleAxis(45, Vector3.up) * savedRotation;
                mount.AttachNow();
                if (Vector3.Distance(boardPosition, mount.transform.position) > 0.0001f ||
                    Quaternion.Angle(boardRotation, mount.transform.rotation) > 0.001f)
                    throw new Exception("Clipboard moves with the rig.");
            }
            finally { origin.transform.SetPositionAndRotation(savedPosition, savedRotation); }
            mount.Recall();
        }
        else if (mount.transform.parent != camera.transform) throw new Exception("PC clipboard parent.");
        if (mount.GetComponentsInChildren<Canvas>().Any(c => c.renderMode != RenderMode.WorldSpace))
            throw new Exception("Overlay UI detected.");
        if (mount.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.raycastTarget))
            throw new Exception("Text consumes physical input.");
        Physics.SyncTransforms();
        var pageButton = mount.transform.Find("Action Page +/Page +").GetComponent<Collider>();
        Vector3 toButton = pageButton.bounds.center - camera.transform.position;
        if (!Physics.Raycast(camera.transform.position, toButton.normalized, out RaycastHit hit, toButton.magnitude + 0.1f)
            || hit.collider != pageButton) throw new Exception("Page button occluded by its own board.");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var label = (UnityEngine.UI.Text)typeof(CercleGame).GetField("journal", flags).GetValue(game);
        if (label.preferredHeight > label.rectTransform.rect.height + 1f) throw new Exception("Journal overflow.");
        var guard = origin != null ? origin.GetComponent<XrTrackingGuard>() : null;
        var start = Time.realtimeSinceStartup;
        await Task.Delay(1200);
        if (Time.frameCount <= firstFrame + 1) throw new Exception("Player loop frozen.");
        return new { scene = game.gameObject.scene.path, worldPinned, readingDistance=initialDistance,
            framesAdvanced=Time.frameCount-firstFrame, observedSeconds=Time.realtimeSinceStartup-start,
            trackingValid=guard != null && guard.HasValidPose, xrActive=UnityEngine.XR.XRSettings.isDeviceActive,
            near=camera.nearClipPlane, far=camera.farClipPlane,
            canvasCount=mount.GetComponentsInChildren<Canvas>().Length, pageButtonRaycast=true, journalFits=true };
    }
}
