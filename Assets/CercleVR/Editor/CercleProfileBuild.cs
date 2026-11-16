using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CercleVR.Editor
{
    public static class CercleProfileBuild
    {
        [MenuItem("Cercle VR/Build/Quest avec son profil")]
        public static void Quest() { Build("Meta Quest", "Builds/Courses-Quest/CercleQuest.apk"); }

        [MenuItem("Cercle VR/Build/Quest - Developpement")]
        public static void QuestDevelopment() { Build("Meta Quest", "Builds/Quest-Development/CercleQuest.apk", BuildOptions.Development); }

        [MenuItem("Cercle VR/Build/Quest - Livraison")]
        public static void QuestRelease() { Build("Meta Quest", "Builds/Quest-Release/CercleQuest.apk"); }

        [MenuItem("Cercle VR/Build/PC avec son profil")]
        public static void PC() { Build("Cercle PC", "Builds/Courses-PC/CerclePC.exe"); }

        [Serializable]
        private sealed class Report
        {
            public string profile, result, platform, output, options, unityVersion, applicationId, version, sha256;
            public double seconds;
            public int errors, warnings;
            public string[] messages;
        }

        private static void Build(string name, string output, BuildOptions options = BuildOptions.None)
        {
            if (EditorApplication.isPlaying) throw new BuildFailedException("Arreter Play avant le build.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new BuildFailedException("Enregistrer les scenes modifiees avant le build.");
            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>("Assets/Settings/Build Profiles/" + name + ".asset");
            if (profile == null) throw new BuildFailedException("Profil absent : " + name);
            // Unity 6000.6.0 can read Android platform options (including symbols) from
            // the active profile even when BuildPlayerWithProfileOptions specifies another.
            // Activate it separately so the target switch and domain reload finish first.
            if (BuildProfile.GetActiveBuildProfile() != profile)
                throw new BuildFailedException("Activer le profil " + name +
                    " dans File > Build Profiles, attendre la fin de l'importation, puis relancer ce build.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new BuildFailedException("Attendre la fin de la compilation et de l'importation avant le build.");
            // OpenXR 1.18's Meta Quest validation reads the selected group, which
            // can remain Standalone after a profile/domain reload despite Android being active.
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            // Explicit modes keep a previous Inspector choice from turning a release into a debug APK.
            var serialized = new SerializedObject(profile);
            var settings = serialized.FindProperty("m_PlatformBuildProfile");
            string[] flags = { "m_Development", "m_AllowDebugging", "m_WaitForManagedDebugger", "m_ConnectProfiler", "m_BuildWithDeepProfilingSupport", "m_BuildWithCodeCoverage" };
            var previous = flags.Select(f => settings.FindPropertyRelative(f).boolValue).ToArray();
            BuildReport report;
            try
            {
                for (int i = 0; i < flags.Length; i++)
                    settings.FindPropertyRelative(flags[i]).boolValue = i == 0 && (options & BuildOptions.Development) != 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                // Passing the profile is necessary for its quality/graphics overrides.
                report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
                {
                    buildProfile = profile,
                    locationPathName = output,
                    options = options
                });
            }
            finally
            {
                serialized.Update();
                settings = serialized.FindProperty("m_PlatformBuildProfile");
                for (int i = 0; i < flags.Length; i++) settings.FindPropertyRelative(flags[i]).boolValue = previous[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(profile);
            }
            var summary = report.summary;
            var result = new Report
            {
                profile = name, result = summary.result.ToString(), platform = summary.platform.ToString(),
                output = summary.outputPath, seconds = summary.totalTime.TotalSeconds,
                options = summary.options.ToString(), unityVersion = Application.unityVersion,
                applicationId = Application.identifier, version = Application.version,
                sha256 = summary.result == BuildResult.Succeeded && File.Exists(output) ? Hash(output) : "",
                errors = (int)summary.totalErrors, warnings = (int)summary.totalWarnings,
                messages = report.steps.SelectMany(s => s.messages).Select(m => m.type + ": " + m.content).ToArray()
            };
            File.WriteAllText(output + ".report.json", JsonUtility.ToJson(result, true));
            if (summary.result != BuildResult.Succeeded || summary.totalErrors != 0)
                throw new BuildFailedException("Build " + name + " : " + summary.result + ", " + summary.totalErrors +
                    " erreur(s). Voir " + output + ".report.json");
            Debug.Log("Build " + name + " reussi : " + output);
        }

        private static string Hash(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
