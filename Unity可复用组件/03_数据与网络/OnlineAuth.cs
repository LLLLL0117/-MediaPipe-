using System;
using EchoWorkSpace.Login;
using UnityEngine;

namespace EchoWorkSpace.Net
{
    /// <summary>最近一次账号操作走的通道。</summary>
    public enum AuthMode
    {
        Unknown,
        /// <summary>服务器在线模式。</summary>
        Server,
        /// <summary>离线本地模式（服务器连不上时的回退）。</summary>
        Local,
    }

    /// <summary>一次注册 / 登录尝试的结果。</summary>
    public sealed class AuthOutcome
    {
        public bool Success;
        public string Error;
        public AuthMode Mode;
    }

    /// <summary>
    /// 在线账号流：注册 / 登录优先走服务器，网络不通或服务器拒绝时回退本地账号库。
    /// 服务器令牌保存在 PlayerPrefs（server.username / server.token），
    /// 成绩双写（ScoreSync）和启动自动登录都依赖它。
    /// </summary>
    public static class OnlineAuth
    {
        public const string ServerUsernameKey = "server.username";
        public const string ServerTokenKey = "server.token";

        // 本次会话内的服务器凭据（不勾“记住我”时只存这里，退出应用即失效）
        private static string _sessionUsername = string.Empty;
        private static string _sessionToken = string.Empty;

        /// <summary>本机是否保存过“记住我”的服务器令牌（启动自动登录用）。</summary>
        public static bool HasPersistedServerToken =>
            !string.IsNullOrEmpty(PlayerPrefs.GetString(ServerUsernameKey, string.Empty)) &&
            !string.IsNullOrEmpty(PlayerPrefs.GetString(ServerTokenKey, string.Empty));

        /// <summary>当前可用的服务器账号名（会话优先，其次本机保存的）。</summary>
        public static string CurrentServerUsername =>
            FirstNonEmpty(_sessionUsername, PlayerPrefs.GetString(ServerUsernameKey, string.Empty));

        /// <summary>当前可用的服务器令牌（会话优先，其次本机保存的）。</summary>
        public static string CurrentServerToken =>
            FirstNonEmpty(_sessionToken, PlayerPrefs.GetString(ServerTokenKey, string.Empty));

        // ── 注册：服务器优先，连不上回退本地 ──────────────────────────

        public static async System.Threading.Tasks.Task<AuthOutcome> RegisterAsync(string username, string password)
        {
            ApiMessage res = await ApiClient.RegisterAsync(username, password);
            if (res != null && res.ok)
                return new AuthOutcome { Success = true, Mode = AuthMode.Server };

            if (res == null)
            {
                // 服务器连不上：回退本地账号库注册
                if (UserAccountStore.TryRegister(username, password, out string localError))
                    return new AuthOutcome { Success = true, Mode = AuthMode.Local };
                return new AuthOutcome { Success = false, Error = localError, Mode = AuthMode.Local };
            }

            // 服务器明确拒绝（如账号重复），提示服务器的理由
            return new AuthOutcome { Success = false, Error = res.message, Mode = AuthMode.Server };
        }

        // ── 登录：服务器优先，失败再试本地（可能有离线注册的账号）─────

        public static async System.Threading.Tasks.Task<AuthOutcome> LoginAsync(string username, string password, bool rememberMe)
        {
            LoginResult res = await ApiClient.LoginAsync(username, password);
            UnityEngine.Debug.Log($"[LoginFlow] 服务器登录结果: {(res == null ? "null(网络不通)" : $"ok={res.ok} msg={res.message}")}");

            if (res != null && res.ok && res.data != null && !string.IsNullOrEmpty(res.data.token))
            {
                SetServerCredential(
                    string.IsNullOrEmpty(res.data.username) ? username : res.data.username,
                    res.data.token, rememberMe);
                return new AuthOutcome { Success = true, Mode = AuthMode.Server };
            }

            if (UserAccountStore.TryLogin(username, password, out string localError))
            {
                UnityEngine.Debug.Log("[LoginFlow] 本地登录成功（离线模式）");
                return new AuthOutcome { Success = true, Mode = AuthMode.Local };
            }
            UnityEngine.Debug.Log($"[LoginFlow] 本地登录失败: {localError}");

            // 都失败：服务器可达时优先展示服务器的提示（更权威）
            if (res != null && !string.IsNullOrEmpty(res.message))
                return new AuthOutcome { Success = false, Error = res.message, Mode = AuthMode.Server };

            return new AuthOutcome { Success = false, Error = localError, Mode = AuthMode.Local };
        }

        // ── 启动自动登录（服务器“记住我”）────────────────────────────

        /// <summary>
        /// 用本机保存的服务器令牌自动登录。
        /// 成功返回服务器账号名（并补建本次会话凭据）；
        /// 失败返回 null（令牌被服务器明确拒绝时已清除，网络不通时保留）。
        /// </summary>
        public static async System.Threading.Tasks.Task<string> TryServerAutoLoginAsync()
        {
            string username = PlayerPrefs.GetString(ServerUsernameKey, string.Empty);
            string token = PlayerPrefs.GetString(ServerTokenKey, string.Empty);
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
                return null;

            ApiMessage res = await ApiClient.AutoLoginAsync(username, token);
            if (res == null)
                return null; // 网络不通：保留令牌，走本地流程

            if (!res.ok)
            {
                ClearPersistedServerToken(); // 服务器明确拒绝：令牌已失效
                return null;
            }

            _sessionUsername = username;
            _sessionToken = token;
            return username;
        }

        /// <summary>已本地登录时后台顺手校验服务器令牌：失效就清掉，网络不通不动。</summary>
        public static async void ValidateServerTokenInBackground()
        {
            try
            {
                if (!HasPersistedServerToken)
                    return;

                ApiMessage res = await ApiClient.AutoLoginAsync(
                    PlayerPrefs.GetString(ServerUsernameKey, string.Empty),
                    PlayerPrefs.GetString(ServerTokenKey, string.Empty));

                if (res != null && !res.ok)
                    ClearPersistedServerToken();
            }
            catch (Exception)
            {
                // 后台校验失败不影响使用
            }
        }

        /// <summary>登出时清除服务器凭据（本次会话 + 本机保存的）。</summary>
        public static void ClearServerToken()
        {
            _sessionUsername = string.Empty;
            _sessionToken = string.Empty;
            ClearPersistedServerToken();
        }

        // ── 内部工具 ─────────────────────────────────────────────────

        private static void SetServerCredential(string username, string token, bool rememberMe)
        {
            _sessionUsername = username;
            _sessionToken = token;

            if (rememberMe)
            {
                PlayerPrefs.SetString(ServerUsernameKey, username);
                PlayerPrefs.SetString(ServerTokenKey, token);
                PlayerPrefs.Save();
            }
            else
            {
                // 没勾“记住我”：顺手清掉本机旧令牌，下次不自动登录
                ClearPersistedServerToken();
            }
        }

        private static void ClearPersistedServerToken()
        {
            if (PlayerPrefs.HasKey(ServerUsernameKey))
                PlayerPrefs.DeleteKey(ServerUsernameKey);
            if (PlayerPrefs.HasKey(ServerTokenKey))
                PlayerPrefs.DeleteKey(ServerTokenKey);
            PlayerPrefs.Save();
        }

        private static string FirstNonEmpty(string a, string b)
        {
            return !string.IsNullOrEmpty(a) ? a : b;
        }
    }
}
