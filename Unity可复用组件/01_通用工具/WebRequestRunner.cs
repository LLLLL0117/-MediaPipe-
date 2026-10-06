using System.Collections;
using UnityEngine;

namespace EchoWorkSpace.Net
{
    /// <summary>
    /// 给静态 ApiClient 提供协程运行环境（隐藏的不销毁 GameObject）。
    /// </summary>
    internal sealed class WebRequestRunner : MonoBehaviour
    {
        private static WebRequestRunner _instance;

        public static void Run(IEnumerator routine)
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("[WebRequestRunner]");
                Object.DontDestroyOnLoad(go);
                _instance = go.AddComponent<WebRequestRunner>();
            }
            _instance.StartCoroutine(routine);
        }
    }
}
