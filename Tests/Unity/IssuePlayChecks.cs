// Run with Unity Pipeline script run while CerclePC or CercleQuest is playing.
// Synthetic poses/input validate the game code; they do not certify headset comfort/tracking.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CercleVR;
using CercleVR.Core;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public static class IssuePlayChecks
{
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    public static async Task<object> Main()
    {
        Check(Application.isPlaying, "Enter Play mode first.");
        var game = UnityEngine.Object.FindAnyObjectByType<CercleGame>();
        var mount = UnityEngine.Object.FindAnyObjectByType<ClipboardMount>(FindObjectsInactive.Include);
        var camera = Camera.main;
        Check(game != null && mount != null && camera != null, "Scene not ready.");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Func<string, object> field = n => typeof(CercleGame).GetField(n, flags).GetValue(game);
        Action<string> call = n => typeof(CercleGame).GetMethod(n, flags).Invoke(game, null);
        Action<int> select = i => typeof(CercleGame).GetMethod("Select", flags).Invoke(game, new object[] { i });
        var campaign = (CampaignProgress)field("campaign");
        Check(campaign.LevelNumber == 1, "Restart Play for a complete six-level run.");
        var origin = camera.GetComponentInParent<XROrigin>();
        var guard = origin != null ? origin.GetComponent<XrTrackingGuard>() : null;
        var modality = origin != null ? origin.GetComponentInChildren<XRInputModalityManager>(true) : null;
        bool modalityEnabled = modality != null && modality.enabled;
        var leftController = game.clipboardAnchor != null ? game.clipboardAnchor.parent.gameObject : null;
        bool leftActive = leftController != null && leftController.activeSelf;
        var drivers = origin != null ? origin.GetComponentsInChildren<TrackedPoseDriver>(true) : new TrackedPoseDriver[0];
        var driverStates = drivers.Select(d => d.enabled).ToArray();
        var guardEnabled = guard != null && guard.enabled;
        var desktop = camera.GetComponent<DesktopCamera>();
        var desktopEnabled = desktop != null && desktop.enabled;
        var cameraPose = new Pose(camera.transform.position, camera.transform.rotation);
        var rigPose = origin != null ? new Pose(origin.transform.position, origin.transform.rotation) : new Pose();
        var anchor = game.clipboardAnchor;
        var anchorPose = anchor != null ? new Pose(anchor.position, anchor.rotation) : new Pose();
        int firstFrame = Time.frameCount;
        var levels = new List<object>();
        float snapAperture = 1f, teleportAperture = 1f, rotation = 0f;
        try
        {
            if (guard != null) guard.enabled = false;
            if (modality != null) modality.enabled = false;
            if (leftController != null) leftController.SetActive(true);
            if (desktop != null) desktop.enabled = false;
            foreach (var driver in drivers) driver.enabled = false;
            if (origin != null)
            {
                camera.transform.SetPositionAndRotation(rigPose.position + Vector3.up * 1.65f, Quaternion.identity);
                anchor.SetPositionAndRotation(rigPose.position + new Vector3(-0.45f, 1.2f, 0.65f), Quaternion.Euler(-50, 0, 0));
                mount.AttachNow();
                Check(!mount.Reading && !mount.transform.IsChildOf(anchor) && mount.transform.lossyScale.x < 0.5f,
                    "Compact clipboard must follow the hand independently of its active state.");
                var before = mount.transform.position;
                anchor.position += Vector3.right * 0.1f;
                mount.AttachNow();
                Check(Vector3.Distance(mount.transform.position, before + Vector3.right * 0.1f) < 0.001f, "Clipboard does not follow hand.");
            }
            else Check(anchor == null, "Desktop must not have a hand anchor.");

            var readingButton = mount.transform.Find("Action Lire/Lire").GetComponent<WorldAction>();
            Physics.SyncTransforms();
            var buttonCollider = readingButton.GetComponent<Collider>();
            var ray = buttonCollider.bounds.center - camera.transform.position;
            Check(Physics.Raycast(camera.transform.position, ray.normalized, out var hit, ray.magnitude + 0.01f) && hit.collider == buttonCollider,
                "Read button is occluded.");
            readingButton.Invoke();
            Check(mount.Reading, "Read callback failed.");
            Check(Vector3.Distance(mount.transform.position, camera.transform.position) >= 0.99f, "Reading too close.");
            if (origin != null)
            {
                Check(mount.transform.parent == null, "Reading must be world-locked.");
                var position = mount.transform.position;
                var orientation = mount.transform.rotation;
                camera.transform.position += new Vector3(0.25f, 0.1f, 0);
                camera.transform.rotation = Quaternion.Euler(45, 30, 25);
                mount.AttachNow();
                Check(Vector3.Distance(position, mount.transform.position) < 0.001f &&
                    Quaternion.Angle(orientation, mount.transform.rotation) < 0.01f, "Reading follows head movement.");
                camera.transform.SetPositionAndRotation(rigPose.position + Vector3.up * 1.65f, Quaternion.identity);
            }
            else Check(mount.transform.parent == camera.transform, "Desktop clipboard no longer follows view.");
            await Task.Delay(100);
            readingButton.Invoke();
            Check(!mount.Reading && (origin == null || mount.transform.lossyScale.x < 0.5f), "Reduce did not return to the hand.");

            var area = UnityEngine.Object.FindAnyObjectByType<SafeTeleportationArea>();
            if (origin != null)
            {
                Check(area != null && area.colliders.Count > 0, "Teleport floor missing collider.");
                var snap = origin.GetComponentInChildren<SnapTurnProvider>();
                var vignette = camera.GetComponentInChildren<TunnelingVignetteController>();
                var reader = snap.rightHandTurnInput;
                var inputMode = reader.inputSourceMode;
                var inputValue = reader.manualValue;
                try
                {
                    var before = origin.transform.rotation;
                    reader.inputSourceMode = XRInputValueReader.InputSourceMode.ManualValue;
                    reader.manualValue = Vector2.right;
                    float deadline = Time.realtimeSinceStartup + 1.5f;
                    while (Quaternion.Angle(before, origin.transform.rotation) < 1f && Time.realtimeSinceStartup < deadline)
                    { snapAperture = Mathf.Min(snapAperture, vignette.currentParameters.apertureSize); await Task.Delay(10); }
                    reader.manualValue = Vector2.zero;
                    rotation = Quaternion.Angle(before, origin.transform.rotation);
                    Check(Mathf.Abs(rotation - snap.turnAmount) < 0.1f, "Snap turn did not rotate by one increment.");
                    Check(snapAperture < 0.95f, "Vignette did not close before snap turn.");
                }
                finally { reader.manualValue = inputValue; reader.inputSourceMode = inputMode; }
                await Task.Delay(700);
                Check(vignette.currentParameters.apertureSize > 0.99f, "Vignette did not reopen after turn.");
                origin.transform.SetPositionAndRotation(rigPose.position, rigPose.rotation);
                var destination = new Vector3(0, 0, -4.1f);
                Physics.SyncTransforms();
                Check(area.IsDestinationClear(destination), "Teleport test destination obstructed.");
                var teleport = area.teleportationProvider;
                Check(teleport.QueueTeleportRequest(new TeleportRequest { destinationPosition = destination, matchOrientation = MatchOrientation.None }), "Teleport request refused.");
                float until = Time.realtimeSinceStartup + 1.5f;
                while (Time.realtimeSinceStartup < until)
                { teleportAperture = Mathf.Min(teleportAperture, vignette.currentParameters.apertureSize); await Task.Delay(10); }
                Check(Vector3.ProjectOnPlane(camera.transform.position - destination, Vector3.up).magnitude < 0.05f &&
                    Mathf.Abs(origin.transform.position.y - destination.y) < 0.05f, "Teleport did not reach requested position.");
                Check(teleportAperture < 0.95f && vignette.currentParameters.apertureSize > 0.99f, "Teleport vignette did not close/reopen.");
                origin.transform.SetPositionAndRotation(rigPose.position, rigPose.rotation);
            }

            for (int level = 1; level <= campaign.TotalLevels; level++)
            {
                var actors = (Transform[])field("characterRoots");
                var dialogue = (Transform)field("dialogue");
                var face = dialogue.GetComponent<FaceCamera>();
                Check(face.keepUpright, "Dialogue not constrained to yaw.");
                for (int i = 0; i < actors.Length; i++)
                {
                    select(i);
                    Check(Vector3.Distance(dialogue.position, actors[i].position + Vector3.up * 2.53f) < 0.001f, "Dialogue follows wrong character.");
                    foreach (float y in new[] { 0.9f, 2.1f })
                    {
                        var old = camera.transform.position;
                        camera.transform.position = new Vector3(old.x, y, old.z);
                        face.FaceNow();
                        Check(Vector3.Dot(dialogue.up, Vector3.up) > 0.999f, "Dialogue tilted with eye height.");
                        camera.transform.position = old;
                    }
                }
                call("Deselect");
                var start = origin != null ? rigPose.position : new Vector3(0, 0, -5.35f);
                var distances = actors.Select(a => Vector3.Distance(start, a.position)).ToArray();
                Physics.SyncTransforms();
                if (area != null)
                {
                    Check(area.IsDestinationClear(start), "Spawn collides with furniture at level " + level);
                    Check(!area.IsDestinationClear(Vector3.zero), "Table destination allowed.");
                    Check(actors.All(a => !area.IsDestinationClear(a.position)), "Chair destination allowed.");
                    Check(!area.IsDestinationClear(new Vector3(5.95f, 0, 0)), "Wall destination allowed.");
                }
                var generated = game.GetComponentsInChildren<Renderer>(true).Concat(mount.GetComponentsInChildren<Renderer>(true));
                Check(generated.All(r => r.sharedMaterials.All(m => m != null && m.shader != null && m.shader.name != "Hidden/InternalErrorShader")), "Missing material/shader.");
                levels.Add(new { level, seats = actors.Length, distances, spawnClear = area == null ? (bool?)null : true });
                var previousMaterials = ((List<Material>)field("actorMaterials")).ToArray();
                var session = (Session)field("session");
                call("ValidateLevelForEditor");
                Check(session.Outcome == Outcome.Won, "Game victory flow failed.");
                if (level < campaign.TotalLevels)
                {
                    float deadline = Time.realtimeSinceStartup + 10;
                    while (campaign.LevelNumber == level || (bool)field("generating"))
                    { Check(Time.realtimeSinceStartup < deadline, "Level transition timed out."); await Task.Delay(100); }
                    await Task.Delay(100);
                    Check(previousMaterials.All(m => m == null), "Actor materials leaked across levels.");
                }
            }
            Check(campaign.Completed, "Campaign not complete.");
            Check(Time.frameCount > firstFrame + 10, "Game loop frozen.");
            return new { scene = game.gameObject.scene.path, simulatedPoses = origin != null, framesAdvanced = Time.frameCount - firstFrame,
                levels, snapDegrees = rotation, snapAperture, teleportAperture, clipboardReadReduce = true, materialsReleased = true,
                limitation = "No headset: optical readability, real tracking and comfort remain untested." };
        }
        finally
        {
            if (origin != null) origin.transform.SetPositionAndRotation(rigPose.position, rigPose.rotation);
            camera.transform.SetPositionAndRotation(cameraPose.position, cameraPose.rotation);
            if (anchor != null) anchor.SetPositionAndRotation(anchorPose.position, anchorPose.rotation);
            for (int i = 0; i < drivers.Length; i++) drivers[i].enabled = driverStates[i];
            if (guard != null) guard.enabled = guardEnabled;
            if (leftController != null) leftController.SetActive(leftActive);
            if (modality != null) modality.enabled = modalityEnabled;
            if (desktop != null) desktop.enabled = desktopEnabled;
        }
    }
}
