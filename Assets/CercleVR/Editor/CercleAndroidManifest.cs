#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;

namespace CercleVR.Editor
{
    // The modern OpenXR broker uses OPENXR_SYSTEM. OPENXR is a legacy broker
    // permission still declared by Unity's loader AAR, unnecessary on Quest 3.
    // https://registry.khronos.org/OpenXR/specs/1.1/loader.html#android-active-runtime-location
    public sealed class CercleAndroidManifest : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 10000;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src/main/AndroidManifest.xml");
            var document = new XmlDocument { XmlResolver = null };
            document.Load(manifestPath);
            RemoveDeclarations(document, "org.khronos.openxr.permission.OPENXR");
            // CercleVR has no network feature. Keep Unity's diagnostic networking
            // only in Development APKs; revisit this policy if the game adds networking.
            if (!EditorUserBuildSettings.development)
                RemoveDeclarations(document, "android.permission.INTERNET");
            document.Save(manifestPath);

            // The loader AAR is merged by launcher, not unityLibrary. Place its
            // removal directive there to avoid unmatched-directive warnings.
            var launcherPath = Path.GetFullPath(Path.Combine(path, "..", "launcher", "src/main/AndroidManifest.xml"));
            document.Load(launcherPath);
            ExcludeLegacyOpenXR(document);
            document.Save(launcherPath);
        }

        private static void RemoveDeclarations(XmlDocument document, string permission)
        {
            const string android = "http://schemas.android.com/apk/res/android";
            var root = document.DocumentElement;
            foreach (XmlNode node in root.SelectNodes("uses-permission"))
                if (node is XmlElement element && element.GetAttribute("name", android) == permission)
                    root.RemoveChild(node);
        }

        private static void ExcludeLegacyOpenXR(XmlDocument document)
        {
            const string android = "http://schemas.android.com/apk/res/android";
            const string tools = "http://schemas.android.com/tools";
            const string permission = "org.khronos.openxr.permission.OPENXR";
            RemoveDeclarations(document, permission);
            // Remove the lower-priority AAR declaration at manifest merge time.
            var removal = document.CreateElement("uses-permission");
            removal.SetAttribute("name", android, permission);
            var directive = document.CreateAttribute("tools", "node", tools);
            directive.Value = "remove";
            removal.Attributes.Append(directive);
            document.DocumentElement.AppendChild(removal);
        }
    }
}
#endif
