using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ApkBuilder
{
    public static void Build()
    {
        string projectDir = Directory.GetParent(Application.dataPath).FullName;
        string outputDir = Path.Combine(projectDir, "Builds");
        Directory.CreateDirectory(outputDir);
        string apkPath = Path.Combine(outputDir, "BasketballMediaPipe.apk");

        var buildTargetGroup = BuildTargetGroup.Android;
        var buildTarget = BuildTarget.Android;

        var switchResult = EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        if (!switchResult)
        {
            throw new Exception("Failed to switch active build target to Android.");
        }

        // mediapipe_android.aar ships arm64-v8a native libraries only
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        // CAMERA permission is auto-injected by Unity because the app uses WebCamTexture

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled && File.Exists(s.path))
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new Exception("No enabled scenes found in Build Settings.");
        }

        Debug.Log($"[ApkBuilder] Building APK with {scenes.Length} scenes -> {apkPath}");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apkPath,
            targetGroup = buildTargetGroup,
            target = buildTarget,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log($"[ApkBuilder] result={summary.result} totalErrors={summary.totalErrors} totalSize={summary.totalSize} output={apkPath}");

        if (summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }
}
