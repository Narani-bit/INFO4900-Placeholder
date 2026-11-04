using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace CercleVR.Editor
{
    public static class CercleSceneBuilder
    {
        [MenuItem("Cercle VR/1 - Creer scene PC")]
        public static void Desktop() { Build(false); }
        [MenuItem("Cercle VR/2 - Creer scene Quest")]
        public static void Quest() { Build(true); }
        private static void Build(bool xr)
        {
            // Save dialog protects an existing edited scene.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
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
            new GameObject("Jeu").AddComponent<CercleGame>();
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Sol - zone teleportation";
            floor.transform.position = new Vector3(0, -0.1f, 0); floor.transform.localScale = new Vector3(12, 0.2f, 12);
            Directory.CreateDirectory("Assets/CercleVR/Materials");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
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
                rig.transform.position = new Vector3(0, 0, -5.35f);
                ConfigureRig(rig);
                var area = floor.AddComponent<TeleportationArea>();
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
            string scenePath = AssetDatabase.GenerateUniqueAssetPath("Assets/CercleVR/Scenes/" + (xr ? "CercleQuest" : "CerclePC") + ".unity");
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
            if (rig.GetComponent<XrTrackingGuard>() == null) rig.AddComponent<XrTrackingGuard>();
        }
    }
}
