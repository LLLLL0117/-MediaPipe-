using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EchoWorkSpace.EditorTools
{
    /// <summary>
    /// 把除 Login 外的所有游戏场景打成独立 AssetBundle（LZ4 分块压缩），
    /// 输出到 Assets/StreamingAssets/SceneBundles/，随 APK 的 assets/ 目录分发。
    /// 运行时由 SceneBundleLoader 通过 Android AssetManager 读取，
    /// 绕开 IL2CPP libunity 读 APK 内 .assets.splitN 分块的 seek 死循环。
    /// </summary>
    public static class BuildSceneAssetBundles
    {
        public const string OutputFolder = "Assets/StreamingAssets/SceneBundles";

        /// <summary>打成 bundle 的场景（Login 不在其中，保留在 BuildSettings）。</summary>
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Main.unity",
            "Assets/Scenes/Analysis.unity",
            "Assets/Scenes/TrainingSingleBean.unity",
            "Assets/Scenes/TrainingMultiBean.unity",
            "Assets/Scenes/TrainingMultiFruit.unity",
            "Assets/Scenes/TrainingSingleJumpingJacks.unity",
            "Assets/Scenes/TrainingQuiz.unity",
        };

        [MenuItem("Build/Build Scene AssetBundles")]
        public static void Build()
        {
            Directory.CreateDirectory(OutputFolder);

            var builds = ScenePaths.Select(p => new AssetBundleBuild
            {
                assetBundleName = Path.GetFileNameWithoutExtension(p).ToLowerInvariant() + ".bundle",
                assetNames = new[] { p },
            }).ToArray();

            // ChunkBasedCompression = LZ4：读取时按块解压，内存友好，加载快。
            var manifest = BuildPipeline.BuildAssetBundles(
                OutputFolder, builds,
                BuildAssetBundleOptions.ChunkBasedCompression,
                BuildTarget.Android);

            if (manifest == null)
                throw new System.Exception("[BuildSceneAssetBundles] AssetBundle 打包失败");

            AssetDatabase.Refresh();
            Debug.Log("[BuildSceneAssetBundles] 完成: " + string.Join(", ", manifest.GetAllAssetBundles()));
        }
    }
}
