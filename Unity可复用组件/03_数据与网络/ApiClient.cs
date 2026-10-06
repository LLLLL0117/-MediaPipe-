using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EchoWorkSpace.Net
{
    /// <summary>
    /// 调用 Spring Boot 后端的 HTTP 客户端：注册 / 登录 / 上传成绩 / 查汇总。
    /// 全部方法可在 async 方法里 await，例如：
    /// var r = await ApiClient.LoginAsync("1111", "123456");
    /// </summary>
    public static class ApiClient
    {
        // ── 账号接口 ───────────────────────────────────────────────

        public static Task<ApiMessage> RegisterAsync(string username, string password)
        {
            string json = $"{{\"username\":\"{Escape(username)}\",\"password\":\"{Escape(password)}\"}}";
            return SendPostAsync<ApiMessage>("/api/register", json);
        }

        public static Task<LoginResult> LoginAsync(string username, string password)
        {
            string json = $"{{\"username\":\"{Escape(username)}\",\"password\":\"{Escape(password)}\"}}";
            return SendPostAsync<LoginResult>("/api/login", json);
        }

        /// <summary>“记住我”自动登录：用保存的账号名 + 令牌换取会话有效性。</summary>
        public static Task<ApiMessage> AutoLoginAsync(string username, string token)
        {
            return SendGetAsync<ApiMessage>("/api/autologin", username, token);
        }

        // ── 成绩接口（需要登录令牌）────────────────────────────────

        public static Task<ApiMessage> UploadScoreAsync(string username, string token, ScoreUploadData data)
        {
            string json = JsonUtility.ToJson(data ?? new ScoreUploadData());
            return SendPostAsync<ApiMessage>("/api/scores", json, username, token);
        }

        public static Task<SummaryResult> GetSummaryAsync()
        {
            return SendGetAsync<SummaryResult>("/api/scores/summary");
        }

        /// <summary>当前账号的历史成绩（最新在前），“我的”页面用。</summary>
        public static Task<MineResult> GetMineAsync(string username)
        {
            return SendGetAsync<MineResult>("/api/scores?username=" + UnityWebRequest.EscapeURL(username ?? string.Empty));
        }

        /// <summary>每个游戏分类的前三名（排行榜用）。</summary>
        public static Task<TopResult> GetTopAsync()
        {
            return SendGetAsync<TopResult>("/api/scores/top");
        }

        public static Task<ApiMessage> PingAsync()
        {
            return SendGetAsync<ApiMessage>("/api/ping");
        }

        // ── 底层请求封装 ───────────────────────────────────────────

        private static Task<T> SendGetAsync<T>(string path, string username = null, string token = null) where T : class
        {
            UnityWebRequest request = UnityWebRequest.Get(ServerConfig.BaseUrl + path);
            if (!string.IsNullOrEmpty(username))
                request.SetRequestHeader("X-Username", username);
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("X-Token", token);
            return SendAsync<T>(request);
        }

        private static Task<T> SendPostAsync<T>(string path, string jsonBody,
            string username = null, string token = null) where T : class
        {
            byte[] body = Encoding.UTF8.GetBytes(jsonBody ?? "{}");
            UnityWebRequest request = new UnityWebRequest(ServerConfig.BaseUrl + path, "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            if (!string.IsNullOrEmpty(username))
                request.SetRequestHeader("X-Username", username);
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("X-Token", token);
            return SendAsync<T>(request);
        }

        private static Task<T> SendAsync<T>(UnityWebRequest request) where T : class
        {
            // 超时兜底：手机连不上服务器时自动转离线模式，不能一直转圈
            request.timeout = 8;
            Debug.Log($"[ApiClient] 请求发出: {request.method} {request.url}");
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            WebRequestRunner.Run(RunRequest(request, tcs));
            return tcs.Task;
        }

        private static IEnumerator RunRequest<T>(UnityWebRequest request, TaskCompletionSource<T> tcs)
            where T : class
        {
            using (request)
            {
                UnityWebRequestAsyncOperation op;
                try
                {
                    op = request.SendWebRequest();
                }
                catch (System.Exception e)
                {
                    // 明文 HTTP 被系统禁掉等异常：立即结束，转离线模式，绝不卡住界面
                    Debug.LogError("[ApiClient] 请求发起失败：" + e.Message);
                    tcs.SetResult(null);
                    yield break;
                }

                yield return op;

                Debug.Log($"[ApiClient] 请求返回: {request.url} result={request.result} code={request.responseCode} err={request.error}");

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // 后端业务错误（如密码不对）也会返回 JSON，尝试解析出 message
                    string text = request.downloadHandler != null ? request.downloadHandler.text : null;
                    if (!string.IsNullOrEmpty(text))
                    {
                        try
                        {
                            ApiMessage msg = JsonUtility.FromJson<ApiMessage>(text);
                            if (msg != null && !string.IsNullOrEmpty(msg.message))
                            {
                                tcs.SetResult(JsonUtility.FromJson<T>(text));
                                yield break;
                            }
                        }
                        catch
                        {
                            // 不是 JSON（通常是连不上服务器），走下面的网络错误
                        }
                    }
                    tcs.SetResult(null);
                    yield break;
                }

                string responseText = request.downloadHandler.text;
                T result;
                try
                {
                    result = JsonUtility.FromJson<T>(responseText);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[ApiClient] 返回内容解析失败：" + e.Message + "\n" + responseText);
                    tcs.SetResult(null);
                    yield break;
                }
                tcs.SetResult(result);
            }
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"")
                        .Replace("\r", "").Replace("\n", "");
        }
    }
}
