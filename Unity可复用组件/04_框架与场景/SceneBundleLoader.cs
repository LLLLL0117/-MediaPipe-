using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoWorkSpace
{
    /// <summary>
    /// 场景 AssetBundle 加载器。
    /// 背景：IL2CPP 运行时在本机（Android 16）读 APK 内 .assets.splitN 分块时，
    /// ZipFile/AndroidSplitFile 的 seek 路径 O(n^2) 死循环，LoadScene 永久卡死。
    /// 修复：除 Login 外的场景全部打成 AssetBundle 放 StreamingAssets/SceneBundles，
    /// 读取走 Android AssetManager（原生 API），绕开 Unity VFS ZipFile 路径。
    /// Login 场景保留在 BuildSettings（资源小，可正常加载）。
    /// </summary>
    public static class SceneBundleLoader
    {
        private const string BundleFolder = "SceneBundles";
        private const string BundleExtension = ".bundle";

        /// <summary>留在 BuildSettings 里直通的场景（不打 bundle）。</summary>
        private static readonly HashSet<string> DirectScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Login",
        };

        private static AssetBundle _currentBundle;
        private static string _currentSceneName;
        private static bool _loading;
        private static GameObject _host;

        /// <summary>加载场景（Single 模式）。Login 直通 BuildSettings，其余走 StreamingAssets bundle。</summary>
        public static void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneBundleLoader] 场景名为空，无法加载。");
                return;
            }

            // 桌面平台（Windows/macOS/Linux）与编辑器：场景已全部在 BuildSettings 中，直接加载。
            // StreamingAssets 里的 bundle 是按 Android 目标打包的（为绕开安卓 IL2CPP 读
            // APK split 分块的卡死），在桌面平台既加载不了也没有必要走 bundle。
            if (UseDirectLoading)
            {
                if (_currentBundle != null)
                {
                    _currentBundle.Unload(false);
                    _currentBundle = null;
                }
                _currentSceneName = null;
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                return;
            }

            if (DirectScenes.Contains(sceneName))
            {
                // 回到 Login 等直通场景时，把旧的 bundle 卸掉，下次进非直通场景可重新加载
                if (_currentBundle != null)
                {
                    _currentBundle.Unload(false);
                    _currentBundle = null;
                }
                _currentSceneName = null;
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                return;
            }

            if (_loading)
            {
                Debug.LogWarning("[SceneBundleLoader] 正在加载场景，忽略重复请求: " + sceneName);
                return;
            }

            // 已在目标场景中，跳过重复加载
            if (_currentSceneName != null && _currentSceneName.Equals(sceneName, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[SceneBundleLoader] 目标场景已在当前场景中，跳过加载: " + sceneName);
                return;
            }

            EnsureHost();
            _host.GetComponent<Host>().StartCoroutine(LoadRoutine(sceneName));
        }

        private static IEnumerator LoadRoutine(string sceneName)
        {
            _loading = true;
            string bundleName = sceneName.ToLowerInvariant() + BundleExtension;
            string path = Path.Combine(Application.streamingAssetsPath, BundleFolder + "/" + bundleName);

            Debug.Log("[SceneBundleLoader] 加载场景包: " + path);

            // Android 上 StreamingAssets 走 AAssetManager（原生 API），不经 Unity ZipFile。
            var req = AssetBundle.LoadFromFileAsync(path);
            yield return req;

            var bundle = req.assetBundle;
            if (bundle == null)
            {
                Debug.LogError("[SceneBundleLoader] 场景包加载失败: " + path);
                _loading = false;
                yield break;
            }

            var scenePaths = bundle.GetAllScenePaths();
            if (scenePaths.Length == 0)
            {
                Debug.LogError("[SceneBundleLoader] 场景包内没有场景: " + bundleName);
                bundle.Unload(false);
                _loading = false;
                yield break;
            }

            var op = SceneManager.LoadSceneAsync(scenePaths[0], LoadSceneMode.Single);
            while (!op.isDone)
                yield return null;

            // 旧 bundle 延迟到新场景加载完成后再卸载，避免场景切换间隙资源悬空。
            if (_currentBundle != null)
                _currentBundle.Unload(false);
            _currentBundle = bundle;
            _currentSceneName = sceneName;
            _loading = false;
            Debug.Log("[SceneBundleLoader] 场景加载完成: " + sceneName);
        }

        private static void EnsureHost()
        {
            if (_host != null) return;
            _host = new GameObject("[SceneBundleLoader]");
            _host.AddComponent<Host>();
            UnityEngine.Object.DontDestroyOnLoad(_host);
        }

        /// <summary>协程宿主（静态类不能 StartCoroutine）。</summary>
        private sealed class Host : MonoBehaviour { }

        /// <summary>桌面平台与编辑器直接走 BuildSettings 加载；Android 设备继续走 bundle。</summary>
        private static bool UseDirectLoading
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.WebGLPlayer:
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
