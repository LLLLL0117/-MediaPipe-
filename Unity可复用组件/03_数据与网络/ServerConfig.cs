using UnityEngine;

namespace EchoWorkSpace.Net
{
    /// <summary>
    /// 服务器地址配置。
    /// 电脑（编辑器/PC 包）上 Spring Boot 跑在本机，用 http://127.0.0.1:8080；
    /// 手机真机用电脑的局域网 IP，手机和电脑必须连同一个 WiFi，且电脑防火墙放行 8080。
    /// 换了 WiFi / IP 变了时，可在游戏里用 PlayerPrefs 写 "server.baseurl" 临时覆盖。
    /// </summary>
    public static class ServerConfig
    {
        /// <summary>电脑在局域网里的 IP（打包手机包时按实际 WiFi 修改）。</summary>
        public const string LanBaseUrl = "http://192.168.31.100:8080";

        /// <summary>本机地址（电脑上运行用）。</summary>
        public const string LocalBaseUrl = "http://127.0.0.1:8080";

        /// <summary>当前生效的服务器地址：优先 PlayerPrefs 覆盖，再按平台取默认。</summary>
        public static string BaseUrl
        {
            get
            {
                string custom = PlayerPrefs.GetString("server.baseurl", string.Empty);
                if (!string.IsNullOrEmpty(custom))
                    return custom;

#if UNITY_ANDROID && !UNITY_EDITOR
                return LanBaseUrl;
#else
                return LocalBaseUrl;
#endif
            }
        }
    }
}
