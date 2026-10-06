using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoWorkSpace
{
    /// <summary>
    /// 场景加载器：挂到场景中任意 GameObject 上，
    /// 通过 LoadScene(string) 即可切换场景（例如绑定到 UI Button 的 OnClick）。
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        /// <summary>按场景名加载（Single 模式，替换当前场景）。非 Login 场景走 AssetBundle。</summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] 场景名为空，无法加载。");
                return;
            }

            // 委托给 SceneBundleLoader：绕开 IL2CPP 读 APK split 分块的死循环。
            SceneBundleLoader.Load(sceneName);
        }
    }
}
