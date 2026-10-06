using System;

namespace EchoWorkSpace.Login
{
    /// <summary>
    /// 单个用户账号的持久化数据（保存到 persistentDataPath 的 JSON 文件中）。
    /// 密码永远不明文保存，只保存随机盐 + 加盐哈希。
    /// </summary>
    [Serializable]
    public sealed class UserAccount
    {
        /// <summary>登录账号名（唯一）。</summary>
        public string username;

        /// <summary>密码加盐后的 SHA256 哈希（Base64）。</summary>
        public string passwordHash;

        /// <summary>该账号专属的随机盐（Base64）。</summary>
        public string salt;

        /// <summary>“记住我”自动登录令牌的哈希；为空表示没有可自动登录的设备。</summary>
        public string rememberTokenHash;

        /// <summary>注册时间。</summary>
        public string createdAt;

        /// <summary>资料最后更新时间。</summary>
        public string updatedAt;
    }
}
