using System;
using UnityEngine;

namespace EchoWorkSpace.Login
{
    /// <summary>
    /// 全局登录会话：记录当前登录用户、跨场景保持登录态、“记住我”自动登录与登出。
    /// 纯 C# 静态类，任意脚本都可以读取 LoginSession.CurrentUsername 显示欢迎语。
    /// </summary>
    public static class LoginSession
    {
        private const string UsernameKey = "Basketball.Login.Username";
        private const string TokenKey = "Basketball.Login.Token";

        /// <summary>本次启动是否已经登录（含自动登录恢复）。</summary>
        public static bool IsLoggedIn { get; private set; }

        /// <summary>当前登录的账号名；未登录时为 null。</summary>
        public static string CurrentUsername { get; private set; }

        /// <summary>登录成功（含自动登录恢复）时触发一次。</summary>
        public static event Action LoggedIn;

        /// <summary>登出时触发一次。</summary>
        public static event Action LoggedOut;

        /// <summary>
        /// 尝试用本机保存的“记住我”令牌恢复登录。
        /// 登录场景启动时、以及主场景的 LoginGate 兜底时都会调用。
        /// </summary>
        public static bool TryRestore()
        {
            if (IsLoggedIn)
                return true;

            string username = PlayerPrefs.GetString(UsernameKey, string.Empty);
            string token = PlayerPrefs.GetString(TokenKey, string.Empty);

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token) ||
                !UserAccountStore.ValidateRememberToken(username, token))
            {
                ClearPersisted();
                return false;
            }

            CurrentUsername = username;
            IsLoggedIn = true;
            LoggedIn?.Invoke();
            return true;
        }

        /// <summary>账号密码校验通过后调用：建立会话，按需写入“记住我”令牌。</summary>
        public static void Login(string username, bool rememberMe)
        {
            username = string.IsNullOrEmpty(username) ? string.Empty : username.Trim();

            if (rememberMe)
            {
                string token = UserAccountStore.IssueRememberToken(username);
                if (!string.IsNullOrEmpty(token))
                {
                    PlayerPrefs.SetString(UsernameKey, username);
                    PlayerPrefs.SetString(TokenKey, token);
                    PlayerPrefs.Save();
                }
                else
                {
                    ClearPersisted();
                }
            }
            else
            {
                // 换设备/不勾记住我时，顺手清掉旧的自动登录信息
                ClearPersisted();
            }

            CurrentUsername = username;
            IsLoggedIn = true;
            LoggedIn?.Invoke();
        }

        /// <summary>登出：令牌立即失效，并清除本机登录信息（含服务器令牌）。</summary>
        public static void Logout()
        {
            if (!IsLoggedIn && string.IsNullOrEmpty(PlayerPrefs.GetString(UsernameKey, string.Empty)))
                return;

            UserAccountStore.RevokeRememberToken(CurrentUsername);
            Net.OnlineAuth.ClearServerToken();
            ClearPersisted();

            CurrentUsername = null;
            IsLoggedIn = false;
            LoggedOut?.Invoke();
        }

        private static void ClearPersisted()
        {
            if (PlayerPrefs.HasKey(UsernameKey))
                PlayerPrefs.DeleteKey(UsernameKey);
            if (PlayerPrefs.HasKey(TokenKey))
                PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.Save();
        }
    }
}
