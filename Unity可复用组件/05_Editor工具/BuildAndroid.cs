                                                                                                                                using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EchoWorkSpace.EditorTools
{
    /// <summary>
    /// Android 打包脚本：批量模式一键出 APK。
    /// 命令行：Unity.exe -quit -batchmode -executeMethod EchoWorkSpace.EditorTools.BuildAndroid.BuildApk
    /// </summary>
    public static class BuildAndroid
    {
        public static void BuildApk()
        {
            string output = @"D:\BasketballMediaPipe.zip\BasketballMediaPipe\Builds\BasketballMediaPipe.apk";

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.Android);

            // 手机桌面显示的应用名
            PlayerSettings.productName = "跃篮逐迹";

            // 中文应用名会让默认包名不合法，显式固定包名（与已装应用一致，覆盖安装不丢数据）
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.DefaultCompany.BasketballMediaPipe");

            // 篮球应用图标（Assets/Icons/basketball_icon.png）
            const string iconPath = "Assets/Icons/basketball_icon.png";
            TextureImporter iconImporter = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (iconImporter != null && !iconImporter.isReadable)
            {
                iconImporter.isReadable = true;
                iconImporter.SaveAndReimport();
            }
            Texture2D appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (appIcon != null)
            {
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new[] { appIcon });
            }
            else
            {
                Console.WriteLine("[BuildAndroid] 未找到应用图标: " + iconPath + "，沿用默认图标");
            }

            // 真机联网需要 INTERNET 权限（WebSocket/UnityWebRequest 自动检测，这里显式打开更稳）
            PlayerSettings.Android.forceInternetPermission = true;

            // UnityWebRequest 允许明文 HTTP（Unity 层开关；配合 AndroidManifest 的
            // usesCleartextTraffic，否则会报 "Insecure connection not allowed"）
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

            // IL2CPP + 单 ARM64（MediaPipe AAR 仅含 arm64 的 libmediapipe_jni.so）。
            // 已证实（build 29 arm64 / build 31 armv7）：场景加载死循环与 ABI 无关，
            // 两种架构 IL2CPP 版 libunity.so 的 ZipFile/AndroidSplitFile 路径均死循环，
            // Mono 版同 APK 结构正常 → 引擎 IL2CPP 运行时在 Android16 上读 APK 内
            // .assets.splitN 分块的 bug。
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // 修复方案：AssetBundle 外置场景（方案 C）。
            // OBB 已证实失败（build 32：VFS 把 OBB 也当 ZIP，同样走 ZipFile 死循环）。
            // 新策略：除 Login 外的场景全部打成 AssetBundle 放 StreamingAssets，
            // 运行时读取走 Android AssetManager（原生 API），彻底绕开
            // FileSystemAndroidAPK 的 AndroidSplitFile seek 死循环。
            PlayerSettings.Android.useAPKExpansionFiles = false;

            // 强制 OpenGL ES3：该机型（OPPO/MTK, Mali 系新 GPU）上 IL2CPP 包运行 Vulkan 时
            // 报 "Desired shader compiler platform 18 is not available in shader blob"，
            // 随后主线程卡死、首场景脚本零执行；Mono 包同设备 Vulkan 正常。
            // shader blob 中 GLES3 变体齐全，先强制 GLES3 排除该疑点。
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

            // 关键修复：Assets/Plugins/Android/ 下已放置自定义 mainTemplate.gradle 与
            // launcherTemplate.gradle（Unity 规则：文件存在即自动启用），在 aaptOptions.noCompress
            // 中追加 'assets' 与全部 'splitN' 扩展名。
            // 原因：Unity 默认把序列化数据 .assets 以 DEFLATE 打入 APK，并把大于 1MB 的
            // 压缩文件切成 .splitN 分块；IL2CPP 运行时反序列化场景（Texture2D::Transfer）
            // 要随机 seek 这些压缩分块，每次 ftello 都令 ZipFile 从分块头重新 inflate，
            // 形成 O(n^2) 空转（Loading.Preload 线程 100% CPU 数分钟），
            // PreloadManager::WaitForAllAsyncOperationsToComplete 永不返回 → 黑屏、场景进不去。
            // STORED 条目直接 pread 寻址（同包 30MB 的 .bytes 模型一直正常）。

            // 竖屏游戏
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // 先把除 Login 外的场景打成 AssetBundle（输出到 StreamingAssets/SceneBundles）。
            BuildSceneAssetBundles.Build();

            // APK 内只打 Login 场景：其余场景已从 bundle 加载，
            // 不进 sharedassets 就不会产生 .assets.splitN 分块。
            string[] scenes = { "Assets/Scenes/Login.unity" };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.Android,
                // Development + AllowDebugging：可附加 Profiler 和 Script Debugger 远程调试
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("[BuildAndroid] 打包失败: " + report.summary.result);

            Console.WriteLine("[BuildAndroid] APK 打包成功: " + output
                + " 大小=" + (report.summary.totalSize / 1024 / 1024) + " MB");
        }
    }
}
