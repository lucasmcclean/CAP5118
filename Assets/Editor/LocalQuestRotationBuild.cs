using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds the review branch's rotation scene without changing the team's scene list.
public static class LocalQuestRotationBuild
{
    public static void Build()
    {
        const string appId = "com.rohangudla.cap5118.rotationpilot";
        string previousId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        string previousName = PlayerSettings.productName;
        bool previousBundle = EditorUserBuildSettings.buildAppBundle;
        try
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, appId);
            PlayerSettings.productName = "CAP5118 Rotation Pilot";
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds/Quest");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Rotation.unity" },
                locationPathName = "Builds/Quest/CAP5118-Rotation-Pilot.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new Exception("Quest build failed: " + result.summary.result);
            Debug.Log("Rotation Quest build succeeded: " + result.summary.outputPath);
        }
        finally
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, previousId);
            PlayerSettings.productName = previousName;
            EditorUserBuildSettings.buildAppBundle = previousBundle;
        }
    }
}
