using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace CercleVR.Editor
{
    public static class CercleSceneBuilder
    {
        public static readonly Vector3 QuestStartPosition = new Vector3(0, 0, -3.5f);
        [MenuItem("Cercle VR/1 - Creer scene PC")]
        public static void Desktop() { Build(false); }
        [MenuItem("Cercle VR/2 - Creer scene Quest")]
        public static void Quest() { Build(true); }
        private static void Build(bool xr)
        {
            // Save dialog protects an existing edited scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string scenePath = "Assets/CercleVR/Scenes/" + (xr ? "CercleQuest" : "CerclePC") + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                EditorSceneManager.OpenScene(scenePath);
                Debug.Log("Scene existante ouverte sans recreation : " + scenePath);
                return;
            }
            GameObject prefab = null;
            if (xr)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (Path.GetFileNameWithoutExtension(path) == "XR Origin (XR Rig)" && path.Contains("Starter Assets"))
                    { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path); break; }
                }
                if (prefab == null)
                {
                    EditorUtility.DisplayDialog("Starter Assets requis", "Installer XR Interaction Toolkit 3.x, puis importer Samples > Starter Assets. Relancer ensuite ce menu.", "OK"); return;
                }
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("Jeu").AddComponent<CercleGame>();
            ConfigureGameMaterial(game);
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Sol - zone teleportation";
            floor.transform.position = new Vector3(0, -0.1f, 0); floor.transform.localScale = new Vector3(12, 0.2f, 12);
            Directory.CreateDirectory("Assets/CercleVR/Materials");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) throw new System.InvalidOperationException("Shader URP/Lit absent.");
            var floorMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CercleVR/Materials/Sol.mat");
            if (floorMaterial == null)
            {
                floorMaterial = new Material(lit) { color = new Color(0.32f, 0.34f, 0.36f) };
                AssetDatabase.CreateAsset(floorMaterial, "Assets/CercleVR/Materials/Sol.mat");
            }

            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            if (xr)
            {
                var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                rig.transform.position = QuestStartPosition;
                ConfigureRig(rig);
                ConfigureClipboard(game, rig);
                var area = floor.AddComponent<SafeTeleportationArea>();
                area.colliders.Add(floor.GetComponent<Collider>());
                area.playerRig = rig.transform;
                area.teleportationProvider = rig.GetComponentInChildren<TeleportationProvider>(true);
                var mask = InteractionLayerMask.GetMask("Teleport");
                area.interactionLayers = mask == 0 ? 1 : mask;
            }
            else
            {
                var camera = new GameObject("Main Camera"); camera.tag = "MainCamera";
                camera.transform.position = new Vector3(0, 1.65f, -5.35f);
                ConfigureCamera(camera.AddComponent<Camera>());
                camera.AddComponent<AudioListener>(); camera.AddComponent<DesktopCamera>();
            }
            var light = new GameObject("Lumiere").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.1f; light.shadows = LightShadows.None; light.transform.rotation = Quaternion.Euler(55, -30, 0);
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.5f);
            Directory.CreateDirectory("Assets/CercleVR/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);
            Selection.activeGameObject = GameObject.Find("Jeu");
            Debug.Log("Scene creee : " + scenePath + ". Suivre README.md pour OpenXR et Build Profiles.");
        }

        public static void ConfigureCamera(Camera camera)
        {
            // Room is 12 m wide. Clipping distances are unrelated to the HMD optical focal plane.
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 30f;
        }

        public static void ConfigureRig(GameObject rig)
        {
            var origin = rig.GetComponent<XROrigin>();
            if (origin == null) throw new System.InvalidOperationException("XR Origin manquant.");
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            origin.CameraYOffset = 1.65f; // Used only if the runtime falls back to Device mode.
            rig.transform.localScale = Vector3.one;
            ConfigureCamera(origin.Camera);
            foreach (var controller in rig.GetComponentsInChildren<ControllerInputActionManager>(true))
            {
                controller.smoothMotionEnabled = false;
                controller.smoothTurnEnabled = false;
            }
            foreach (var move in rig.GetComponentsInChildren<ContinuousMoveProvider>(true)) move.enabled = false;
            foreach (var turn in rig.GetComponentsInChildren<ContinuousTurnProvider>(true)) turn.enabled = false;
            var snap = rig.GetComponentInChildren<SnapTurnProvider>(true);
            var teleport = rig.GetComponentInChildren<TeleportationProvider>(true);
            if (snap == null || teleport == null) throw new System.InvalidOperationException("Providers de locomotion absents.");
            snap.enabled = teleport.enabled = true;
            snap.delayTime = teleport.delayTime = 0.15f;
            var vignette = origin.Camera.GetComponentInChildren<TunnelingVignetteController>(true);
            if (vignette == null)
            {
                string path = AssetDatabase.FindAssets("TunnelingVignette t:Prefab", new[] { "Assets" })
                    .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => Path.GetFileName(p) == "TunnelingVignette.prefab");
                if (path == null) throw new System.InvalidOperationException("Prefab TunnelingVignette absent.");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), origin.Camera.transform);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
                vignette = instance.GetComponent<TunnelingVignetteController>();
            }
            vignette.enabled = true;
            vignette.defaultParameters.easeInTime = 0.1f;
            vignette.defaultParameters.easeOutTime = 0.2f;
            vignette.defaultParameters.easeInTimeLock = true;
            vignette.defaultParameters.easeOutDelayTime = 0.05f;
            vignette.locomotionVignetteProviders = new System.Collections.Generic.List<LocomotionVignetteProvider>
            {
                new LocomotionVignetteProvider { locomotionProvider = snap, enabled = true },
                new LocomotionVignetteProvider { locomotionProvider = teleport, enabled = true }
            };
            PrefabUtility.RecordPrefabInstancePropertyModifications(snap);
            PrefabUtility.RecordPrefabInstancePropertyModifications(teleport);
            PrefabUtility.RecordPrefabInstancePropertyModifications(vignette);
            foreach (var component in rig.GetComponentsInChildren<Behaviour>(true))
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            if (rig.GetComponent<XrTrackingGuard>() == null) rig.AddComponent<XrTrackingGuard>();
        }

        public static void ConfigureGameMaterial(CercleGame game)
        {
            const string path = "Assets/CercleVR/Materials/Base.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new System.InvalidOperationException("Shader URP/Lit absent.");
                Directory.CreateDirectory("Assets/CercleVR/Materials");
                material = new Material(shader) { name = "Base", color = Color.white };
                AssetDatabase.CreateAsset(material, path);
            }
            var serialized = new SerializedObject(game);
            serialized.FindProperty("baseMaterial").objectReferenceValue = material;
            serialized.ApplyModifiedProperties();
        }

        public static void ConfigureClipboard(CercleGame game, GameObject rig)
        {
            var left = rig.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Left Controller");
            var anchor = left.Find("Clipboard Anchor");
            if (anchor == null)
            {
                anchor = new GameObject("Clipboard Anchor").transform;
                anchor.SetParent(left, false);
            }
            game.clipboardAnchor = anchor;
            EditorUtility.SetDirty(game);
        }
    }
}
