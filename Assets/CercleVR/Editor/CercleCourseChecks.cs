using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace CercleVR.Editor
{
    public static class CercleCourseChecks
    {
        [MenuItem("Cercle VR/Tests/Verifier les principes des cours")]
        public static void RunMenu() { Debug.Log(JsonUtility.ToJson(Run(), true)); }

        [Serializable]
        public sealed class Result
        {
            public int checks;
            public string[] scenes;
            public string limitation = "Edit-mode checks; hardware tracking, visual comfort and frame budget require a Quest test.";
        }

        // Also callable through -executeMethod CercleVR.Editor.CercleCourseChecks.RunMenu.
        public static Result Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Arreter Play avant ces tests.");
            int count = 0;
            Action<bool, string> check = (condition, message) =>
            {
                count++;
                if (!condition) throw new InvalidOperationException(message);
            };
            var paths = new[] { "Assets/CercleVR/Scenes/CerclePC.unity", "Assets/CercleVR/Scenes/CercleQuest.unity" };
            foreach (string path in paths)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
                bool wasOpen = scene.IsValid() && scene.isLoaded;
                if (!wasOpen) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).ToArray();
                    check(cameras.Length == 1, path + ": une seule camera attendue.");
                    check(Mathf.Approximately(cameras[0].nearClipPlane, 0.05f), path + ": near.");
                    check(Mathf.Approximately(cameras[0].farClipPlane, 30f), path + ": far.");
                    check(!cameras[0].orthographic, path + ": perspective.");
                    foreach (var root in roots)
                        foreach (var t in root.GetComponentsInChildren<Transform>(true))
                            check(t.GetComponents<Component>().All(c => c != null), path + ": script manquant sur " + t.name);
                    var origins = roots.SelectMany(r => r.GetComponentsInChildren<XROrigin>(true)).ToArray();
                    if (path.Contains("Quest"))
                    {
                        check(origins.Length == 1, "Un XR Origin attendu.");
                        var origin = origins[0];
                        check(origin.RequestedTrackingOriginMode == XROrigin.TrackingOriginMode.Floor, "Suivi Floor.");
                        check(Vector3.Distance(origin.transform.lossyScale, Vector3.one) < 0.0001f, "Echelle metrique XR.");
                        check(origin.GetComponent<XrTrackingGuard>() != null, "Controle du suivi absent.");
                        check(cameras[0].GetComponent<DesktopCamera>() == null, "La souris ne doit pas piloter le casque.");
                        var driver = cameras[0].GetComponent<TrackedPoseDriver>();
                        check(driver != null, "Tracked Pose Driver absent.");
                        check(driver.updateType == TrackedPoseDriver.UpdateType.UpdateAndBeforeRender, "Pose avant rendu.");
                        var managers = origin.GetComponentsInChildren<ControllerInputActionManager>(true);
                        check(managers.Length == 2, "Deux controles de locomotion attendus.");
                        foreach (var manager in managers)
                        {
                            check(!manager.smoothMotionEnabled, "Teleportation par defaut.");
                            check(!manager.smoothTurnEnabled, "Rotation par crans par defaut.");
                        }
                    }
                    else check(origins.Length == 0, "La scene PC doit rester independante du rig XR.");
                }
                finally { if (!wasOpen) EditorSceneManager.CloseScene(scene, true); }
            }

            // Regression: no inherited roll, no NaN when looking straight up/down,
            // and a reading placement at least one metre away even with an invalid offset.
            foreach (float pitch in new[] { -90f, 0f, 90f })
                foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
                    foreach (float roll in new[] { 0f, 45f, 180f })
                    {
                        var eye = new Vector3(3f, 1.72f, -4f);
                        var pose = ClipboardMount.ReadingPose(eye, Quaternion.Euler(pitch, yaw, roll), new Vector3(0, -0.03f, -1f));
                        check(Vector3.Distance(eye, pose.position) >= 1f, "Distance de lecture.");
                        check(!float.IsNaN(pose.rotation.w), "Quaternion fini.");
                        check(Vector3.Dot(pose.rotation * Vector3.up, Vector3.up) > 0.99f, "Papier sans roulis.");
                    }

            var both = InputTrackingState.Position | InputTrackingState.Rotation;
            check(XrTrackingGuard.IsPoseValid(true, true, true, true, both), "Pose 6DDL valide.");
            check(!XrTrackingGuard.IsPoseValid(false, true, true, true, both), "Peripherique deconnecte.");
            check(!XrTrackingGuard.IsPoseValid(true, true, false, true, both), "Suivi perdu.");
            check(!XrTrackingGuard.IsPoseValid(true, true, true, true, InputTrackingState.Rotation), "3DDL ne suffit pas.");
            check(!XrTrackingGuard.IsPoseValid(true, true, true, true, InputTrackingState.Position), "Orientation absente.");
            check(!XrTrackingGuard.IsPoseValid(true, true, true, false, both), "Etat de pose inconnu.");
            check(XrTrackingGuard.IsPoseValid(true, false, false, true, both), "Etat de pose sans bool optionnel.");

            // Object -> world -> camera: T_camera^-1 * T_object, column-vector order.
            var objectToWorld = Matrix4x4.TRS(new Vector3(2, 1, -3), Quaternion.Euler(15, 40, 0), new Vector3(2, 1, 0.5f));
            var cameraToWorld = Matrix4x4.TRS(new Vector3(-1, 1.7f, 4), Quaternion.Euler(0, 65, 0), Vector3.one);
            var localPoint = new Vector3(1, 0.2f, -0.5f);
            var inView = (cameraToWorld.inverse * objectToWorld).MultiplyPoint3x4(localPoint);
            var roundTrip = objectToWorld.inverse.MultiplyPoint3x4(cameraToWorld.MultiplyPoint3x4(inView));
            check(Vector3.Distance(roundTrip, localPoint) < 0.0001f, "Chaine de reperes et inverse.");
            var rotation = Quaternion.AngleAxis(50, Vector3.up) * Quaternion.AngleAxis(20, Vector3.right);
            check(Mathf.Abs((rotation * localPoint).magnitude - localPoint.magnitude) < 0.0001f, "Rotation rigide.");
            check(Vector3.Distance(Quaternion.Inverse(rotation) * (rotation * localPoint), localPoint) < 0.0001f, "Inverse quaternion.");
            return new Result { checks = count, scenes = paths };
        }
    }
}
