using System;
using System.IO;
using System.Linq;
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

        [MenuItem("Cercle VR/Build/PC avec son profil")]
        public static void PC() { Build("Cercle PC", "Builds/Courses-PC/CerclePC.exe"); }

        [Serializable]
        private sealed class Report
        {
            public string profile, result, platform, output;
            public double seconds;
            public int errors, warnings;
            public string[] messages;
        }

        private static void Build(string name, string output)
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
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            // Pass the profile itself: activation alone does not pass its quality/graphics overrides.
            var report = BuildPipeline.BuildPlayer(new BuildPlayerWithProfileOptions
            {
                buildProfile = profile,
                locationPathName = output,
                options = BuildOptions.None
            });
            var summary = report.summary;
            var result = new Report
            {
                profile = name, result = summary.result.ToString(), platform = summary.platform.ToString(),
                output = summary.outputPath, seconds = summary.totalTime.TotalSeconds,
                errors = (int)summary.totalErrors, warnings = (int)summary.totalWarnings,
                messages = report.steps.SelectMany(s => s.messages).Select(m => m.type + ": " + m.content).ToArray()
            };
            File.WriteAllText(output + ".report.json", JsonUtility.ToJson(result, true));
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Build " + name + " : " + summary.result + ". Voir " + output + ".report.json");
            Debug.Log("Build " + name + " reussi : " + output);
        }
    }
}
