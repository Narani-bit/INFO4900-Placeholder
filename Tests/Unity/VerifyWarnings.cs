using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;

public static class VerifyWarnings
{
    public static object Main()
    {
        var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>("Assets/Settings/Build Profiles/Meta Quest.asset");
        if (BuildProfile.GetActiveBuildProfile() != profile) throw new Exception("Quest profile inactive.");
        var properties = new SerializedObject(profile);
        var symbols = properties.FindProperty("m_PlatformBuildProfile.m_DebugSymbolLevel");
        if (symbols.enumNames[symbols.enumValueIndex] != "Full") throw new Exception("Symbols not Full.");
        string apk = "Builds/Courses-Quest/CercleQuest.apk";
        var lastBuild = File.GetLastWriteTimeUtc(apk);
        bool rejectedWrongProfile = false;
        try
        {
            BuildProfile.SetActiveBuildProfile(null);
            try { CercleVR.Editor.CercleProfileBuild.Quest(); }
            catch (BuildFailedException ex) { rejectedWrongProfile = ex.Message.StartsWith("Activer le profil Meta Quest"); }
        }
        finally { BuildProfile.SetActiveBuildProfile(profile); }
        if (!rejectedWrongProfile || File.GetLastWriteTimeUtc(apk) != lastBuild)
            throw new Exception("Inactive-profile build guard failed.");
        var courses = CercleVR.Editor.CercleCourseChecks.Run();
        return new { rejectedWrongProfile, apkUnchangedByGuard = true, symbols = "Full", courseChecks = courses.checks,
            compiling = EditorApplication.isCompiling, compilationFailed = EditorUtility.scriptCompilationFailed };
    }
}
