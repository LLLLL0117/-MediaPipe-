using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EchoWorkSpace.Login;
using UnityEngine;

namespace EchoWorkSpace
{
    /// <summary>
    /// 统一本地数据库：一个 basketball_db.json 文件内含两张“表”——
    /// accounts（注册账号）和 records（按游戏分类的成绩记录）。
    /// 账号表通过 username 与成绩表关联。首次使用时自动把旧版
    /// basketball_users.json / basketball_scores.json 迁移进来。
    /// </summary>
    public static class LocalDatabase
    {
        private const string DbFileName = "basketball_db.json";
        private const string LegacyAccountsFile = "basketball_users.json";
        private const string LegacyScoresFile = "basketball_scores.json";

        private static readonly object WriteLock = new object();

        /// <summary>统一数据库文件的完整路径（可在编辑器里直接打开查看）。</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, DbFileName);

        [Serializable]
        public sealed class DatabaseContent
        {
            public List<UserAccount> accounts = new List<UserAccount>();
            public List<ScoreRecord> records = new List<ScoreRecord>();
        }

        /// <summary>读取整库（首次会触发旧数据迁移）。</summary>
        public static DatabaseContent Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(json))
                        return JsonUtility.FromJson<DatabaseContent>(json) ?? new DatabaseContent();
                }

                // 新库不存在：从旧的两个单表文件迁移，合并成统一数据库
                DatabaseContent migrated = new DatabaseContent();
                bool hasLegacy = false;

                string legacyAccounts = ResolvePath(LegacyAccountsFile);
                if (File.Exists(legacyAccounts))
                {
                    try
                    {
                        string json = File.ReadAllText(legacyAccounts, Encoding.UTF8);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            AccountTableLegacy old = JsonUtility.FromJson<AccountTableLegacy>(json);
                            if (old != null && old.accounts != null)
                            {
                                migrated.accounts = old.accounts;
                                hasLegacy = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[LocalDatabase] 迁移旧账号文件失败：" + e.Message);
                    }
                }

                string legacyScores = ResolvePath(LegacyScoresFile);
                if (File.Exists(legacyScores))
                {
                    try
                    {
                        string json = File.ReadAllText(legacyScores, Encoding.UTF8);
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            ScoreTableLegacy old = JsonUtility.FromJson<ScoreTableLegacy>(json);
                            if (old != null && old.records != null)
                            {
                                migrated.records = old.records;
                                hasLegacy = true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[LocalDatabase] 迁移旧成绩文件失败：" + e.Message);
                    }
                }

                if (hasLegacy)
                {
                    Save(migrated);
                    TryBackupLegacy(legacyAccounts);
                    TryBackupLegacy(legacyScores);
                    Debug.Log("[LocalDatabase] 已把旧版账号/成绩文件迁移到统一数据库 basketball_db.json");
                }

                return migrated;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalDatabase] 读取数据库失败：{FilePath}\n{e}");
                return new DatabaseContent();
            }
        }

        /// <summary>整库写盘（加锁，避免两个模块同时写造成文件损坏）。</summary>
        public static void Save(DatabaseContent content)
        {
            lock (WriteLock)
            {
                try
                {
                    string directory = Path.GetDirectoryName(FilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.WriteAllText(FilePath, JsonUtility.ToJson(content, true), Encoding.UTF8);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[LocalDatabase] 写入数据库失败：{FilePath}\n{e}");
                }
            }
        }

        /// <summary>在一次加锁读写内修改数据库（读取→修改→保存）。</summary>
        public static void Modify(Action<DatabaseContent> action)
        {
            lock (WriteLock)
            {
                DatabaseContent content = Load();
                action?.Invoke(content);
                Save(content);
            }
        }

        private static string ResolvePath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        private static void TryBackupLegacy(string legacyPath)
        {
            try
            {
                if (File.Exists(legacyPath))
                    File.Move(legacyPath, legacyPath + ".bak");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LocalDatabase] 备份旧文件失败（不影响使用）：" + e.Message);
            }
        }

        // 旧版单表文件的反序列化结构（仅迁移时使用）
        [Serializable]
        private sealed class AccountTableLegacy
        {
            public List<UserAccount> accounts = new List<UserAccount>();
        }

        [Serializable]
        private sealed class ScoreTableLegacy
        {
            public List<ScoreRecord> records = new List<ScoreRecord>();
        }
    }
}
