using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EchoWorkSpace.Login
{
    /// <summary>
    /// 本地账号库：注册 / 登录校验 / 记住我令牌。
    /// 账号保存在统一数据库 Application.persistentDataPath/basketball_db.json 的 accounts 表中，
    /// 卸载应用或清除应用数据时会一起删除，不需要任何服务器。
    /// </summary>
    public static class UserAccountStore
    {
        // 账号：中英文、数字、下划线，2-12 位
        private const int UsernameMinLength = 2;
        private const int UsernameMaxLength = 12;
        private const int PasswordMinLength = 6;
        private const int PasswordMaxLength = 20;

        private static readonly Regex UsernameRegex =
            new Regex(@"^[\u4e00-\u9fa5A-Za-z0-9_]+$", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex =
            new Regex(@"\s", RegexOptions.Compiled);

        /// <summary>统一数据库文件的完整路径（账号表 accounts 在其中）。</summary>
        public static string FilePath => LocalDatabase.FilePath;

        // ── 注册 / 登录 ────────────────────────────────────────────────

        public static bool UserExists(string username)
        {
            username = NormalizeUsername(username);
            return Find(LocalDatabase.Load().accounts, username) != null;
        }

        public static bool TryRegister(string username, string password, out string error)
        {
            error = null;
            username = NormalizeUsername(username);

            if (!ValidateUsername(username, out error))
                return false;
            if (!ValidatePassword(password, out error))
                return false;

            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string salt = NewSalt();
            string passwordHash = HashPassword(password, salt);
            bool duplicated = false;

            LocalDatabase.Modify(db =>
            {
                if (Find(db.accounts, username) != null)
                {
                    duplicated = true;
                    return;
                }

                db.accounts.Add(new UserAccount
                {
                    username = username,
                    salt = salt,
                    passwordHash = passwordHash,
                    rememberTokenHash = string.Empty,
                    createdAt = now,
                    updatedAt = now,
                });
            });

            if (duplicated)
            {
                error = "这个账号已经被注册啦，换一个试试 ♥";
                return false;
            }

            return true;
        }

        public static bool TryLogin(string username, string password, out string error)
        {
            error = null;
            username = NormalizeUsername(username);

            UserAccount account = Find(LocalDatabase.Load().accounts, username);
            if (account == null)
            {
                error = "账号还不存在哦，先去注册一个吧";
                return false;
            }

            if (!FixedEquals(HashPassword(password, account.salt), account.passwordHash))
            {
                error = "密码不对哦，再想想看";
                return false;
            }

            return true;
        }

        // ── 表单校验（界面也直接调用，提示语统一）────────────────────────

        public static bool ValidateUsername(string username, out string error)
        {
            username = NormalizeUsername(username);
            error = null;

            if (string.IsNullOrEmpty(username))
            {
                error = "账号不能为空哦";
                return false;
            }
            if (username.Length < UsernameMinLength || username.Length > UsernameMaxLength)
            {
                error = $"账号要 {UsernameMinLength}-{UsernameMaxLength} 位哦";
                return false;
            }
            if (!UsernameRegex.IsMatch(username))
            {
                error = "账号只能用中文、英文、数字或下划线";
                return false;
            }
            return true;
        }

        public static bool ValidatePassword(string password, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(password))
            {
                error = "密码不能为空哦";
                return false;
            }
            if (password.Length < PasswordMinLength || password.Length > PasswordMaxLength)
            {
                error = $"密码要 {PasswordMinLength}-{PasswordMaxLength} 位哦";
                return false;
            }
            if (WhitespaceRegex.IsMatch(password))
            {
                error = "密码里不能有空格哦";
                return false;
            }
            return true;
        }

        // ── “记住我”自动登录令牌 ───────────────────────────────────────

        /// <summary>为账号签发一个新的自动登录令牌，返回需要保存在本机的明文令牌。</summary>
        public static string IssueRememberToken(string username)
        {
            username = NormalizeUsername(username);
            string token = null;

            LocalDatabase.Modify(db =>
            {
                UserAccount account = Find(db.accounts, username);
                if (account == null)
                    return;

                token = Convert.ToBase64String(RandomBytes(32));
                account.rememberTokenHash = HashToken(token);
                account.updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            });

            return token;
        }

        /// <summary>校验本机保存的自动登录令牌是否仍然有效。</summary>
        public static bool ValidateRememberToken(string username, string token)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
                return false;

            UserAccount account = Find(LocalDatabase.Load().accounts, NormalizeUsername(username));
            if (account == null || string.IsNullOrEmpty(account.rememberTokenHash))
                return false;

            return FixedEquals(HashToken(token), account.rememberTokenHash);
        }

        /// <summary>登出时让旧令牌立即失效。</summary>
        public static void RevokeRememberToken(string username)
        {
            if (string.IsNullOrEmpty(username))
                return;

            LocalDatabase.Modify(db =>
            {
                UserAccount account = Find(db.accounts, NormalizeUsername(username));
                if (account == null || string.IsNullOrEmpty(account.rememberTokenHash))
                    return;

                account.rememberTokenHash = string.Empty;
                account.updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            });
        }

        // ── 账号查询（统一数据库 accounts 表）─────────────────────────

        private static UserAccount Find(List<UserAccount> accounts, string normalizedUsername)
        {
            if (accounts == null)
                return null;

            for (int i = 0; i < accounts.Count; i++)
            {
                if (accounts[i] != null &&
                    string.Equals(accounts[i].username, normalizedUsername, StringComparison.Ordinal))
                {
                    return accounts[i];
                }
            }
            return null;
        }

        // ── 哈希工具 ──────────────────────────────────────────────────

        private static string NormalizeUsername(string username)
        {
            return string.IsNullOrEmpty(username) ? string.Empty : username.Trim();
        }

        private static string NewSalt()
        {
            return Convert.ToBase64String(RandomBytes(16));
        }

        private static string HashPassword(string password, string salt)
        {
            return Sha256Base64(salt + ":" + password);
        }

        private static string HashToken(string token)
        {
            // 固定“胡椒粉”，令牌与密码使用不同的哈希域
            return Sha256Base64("remember-token:" + token);
        }

        private static string Sha256Base64(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                return Convert.ToBase64String(bytes);
            }
        }

        private static byte[] RandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                rng.GetBytes(bytes);
            return bytes;
        }

        /// <summary>等时长字符串比较，避免通过耗时差异猜测哈希。</summary>
        private static bool FixedEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;

            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
