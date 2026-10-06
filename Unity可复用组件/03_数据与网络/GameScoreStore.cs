using System;
using System.Collections.Generic;
using EchoWorkSpace.Login;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoWorkSpace
{
    /// <summary>
    /// 游戏成绩数据库：所有训练游戏每局结束后把分数写到这里，按游戏分类 + 账号归档。
    /// 成绩保存在统一数据库 Application.persistentDataPath/basketball_db.json 的 records 表中，
    /// 与账号表 accounts 在同一个文件里，不需要服务器，卸载/清除应用数据时一起删除。
    /// </summary>
    public static class GameScoreStore
    {
        // 单机本地库，保留最近若干局，避免文件无限变大
        private const int MaxRecords = 1000;

        /// <summary>统一数据库文件的完整路径（成绩表 records 在其中）。</summary>
        public static string FilePath => LocalDatabase.FilePath;

        // ── 写入成绩 ─────────────────────────────────────────────────

        /// <summary>
        /// 记录一局成绩。用户名自动取当前登录账号（未登录时记为“游客”）。
        /// </summary>
        /// <param name="gameId">游戏分类 ID，取 GameCatalog 中的常量。</param>
        /// <param name="score">最终分数。</param>
        /// <param name="durationSeconds">本局时长（秒），没有可传 0。</param>
        /// <param name="detail">附加明细（如答题对错数），没有可传 null。</param>
        public static ScoreRecord RecordScore(string gameId, int score, int durationSeconds = 0, string detail = null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return RecordScore(gameId, GameCatalog.GetGameName(gameId), sceneName, score, durationSeconds, detail);
        }

        /// <summary>按当前所在场景自动识别游戏分类并记录成绩（吃豆/切水果/开合跳结算时直接调用）。</summary>
        public static ScoreRecord RecordScoreForCurrentScene(int score, int durationSeconds = 0, string detail = null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            GameCatalog.ResolveByScene(sceneName, out string gameId, out string gameName);
            return RecordScore(gameId, gameName, sceneName, score, durationSeconds, detail);
        }

        private static ScoreRecord RecordScore(string gameId, string gameName, string sceneName,
            int score, int durationSeconds, string detail)
        {
            ScoreRecord record = new ScoreRecord
            {
                id = Guid.NewGuid().ToString("N"),
                username = LoginSession.IsLoggedIn && !string.IsNullOrEmpty(LoginSession.CurrentUsername)
                    ? LoginSession.CurrentUsername
                    : "游客",
                gameId = string.IsNullOrEmpty(gameId) ? GameCatalog.Other : gameId,
                gameName = gameName,
                sceneName = sceneName,
                score = Mathf.Max(0, score),
                durationSeconds = Mathf.Max(0, durationSeconds),
                detail = detail ?? string.Empty,
                playedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            };

            // 一次加锁读写写入统一数据库的 records 表；超量时淘汰最旧的
            LocalDatabase.Modify(db =>
            {
                db.records.Add(record);
                if (db.records.Count > MaxRecords)
                    db.records.RemoveRange(0, db.records.Count - MaxRecords);
            });

            // 成绩双写：本地已入库，异步上传服务器（离线/无令牌时内部静默跳过）
            Net.ScoreSync.UploadAsync(record);

            Debug.Log($"[GameScoreStore] 已记录成绩：{record.username} - {record.gameName} - {record.score} 分");
            return record;
        }

        // ── 查询成绩（按分类 / 按账号 / 最高分）────────────────────────

        /// <summary>全部成绩，按游玩时间从旧到新排列。</summary>
        public static List<ScoreRecord> GetAllRecords()
        {
            return new List<ScoreRecord>(LocalDatabase.Load().records);
        }

        /// <summary>某一游戏分类下的全部成绩（例如 GameCatalog.SingleBean），按时间从旧到新。</summary>
        public static List<ScoreRecord> GetRecordsByGame(string gameId)
        {
            List<ScoreRecord> result = new List<ScoreRecord>();
            foreach (ScoreRecord r in LocalDatabase.Load().records)
            {
                if (r != null && string.Equals(r.gameId, gameId, StringComparison.Ordinal))
                    result.Add(r);
            }
            return result;
        }

        /// <summary>当前登录账号（或指定账号）在某分类下的全部成绩。</summary>
        public static List<ScoreRecord> GetRecordsByGameAndUser(string gameId, string username = null)
        {
            username = NormalizeUsername(username);
            List<ScoreRecord> result = new List<ScoreRecord>();
            foreach (ScoreRecord r in LocalDatabase.Load().records)
            {
                if (r == null) continue;
                if (!string.Equals(r.gameId, gameId, StringComparison.Ordinal)) continue;
                if (string.Equals(r.username, username, StringComparison.Ordinal))
                    result.Add(r);
            }
            return result;
        }

        /// <summary>指定账号在某分类下的历史最高分；没有记录返回 null。</summary>
        public static ScoreRecord GetBestRecord(string gameId, string username = null)
        {
            username = NormalizeUsername(username);
            ScoreRecord best = null;
            foreach (ScoreRecord r in LocalDatabase.Load().records)
            {
                if (r == null) continue;
                if (!string.Equals(r.gameId, gameId, StringComparison.Ordinal)) continue;
                if (!string.Equals(r.username, username, StringComparison.Ordinal)) continue;
                if (best == null || r.score > best.score)
                    best = r;
            }
            return best;
        }

        /// <summary>最近若干局成绩（不分类别），最新的在前。</summary>
        public static List<ScoreRecord> GetRecentRecords(int count)
        {
            List<ScoreRecord> all = LocalDatabase.Load().records;
            List<ScoreRecord> result = new List<ScoreRecord>();
            for (int i = all.Count - 1; i >= 0 && result.Count < count; i--)
            {
                if (all[i] != null)
                    result.Add(all[i]);
            }
            return result;
        }

        /// <summary>
        /// 按游戏分类分组返回全部成绩（key = gameId，value = 该分类的记录），
        /// 顺序遵循 GameCatalog.OrderedGameIds，做“成绩总览”界面时直接用。
        /// </summary>
        public static Dictionary<string, List<ScoreRecord>> GetRecordsGroupedByGame()
        {
            Dictionary<string, List<ScoreRecord>> grouped =
                new Dictionary<string, List<ScoreRecord>>();
            foreach (string id in GameCatalog.OrderedGameIds)
                grouped[id] = new List<ScoreRecord>();

            foreach (ScoreRecord r in LocalDatabase.Load().records)
            {
                if (r == null) continue;
                if (!grouped.TryGetValue(r.gameId, out List<ScoreRecord> list))
                {
                    list = new List<ScoreRecord>();
                    grouped[r.gameId] = list;
                }
                list.Add(r);
            }
            return grouped;
        }

        // ── 统一数据库读写由 LocalDatabase 负责 ────────────────────────

        private static string NormalizeUsername(string username)
        {
            if (!string.IsNullOrEmpty(username))
                return username.Trim();
            return LoginSession.IsLoggedIn && !string.IsNullOrEmpty(LoginSession.CurrentUsername)
                ? LoginSession.CurrentUsername
                : "游客";
        }
    }
}
