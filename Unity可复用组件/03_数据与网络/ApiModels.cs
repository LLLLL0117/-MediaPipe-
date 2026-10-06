using System;

namespace EchoWorkSpace.Net
{
    // ── 与后端 com.yuelan.dto / ApiResult 对应的 JSON 模型 ──────────

    [Serializable]
    public class ApiMessage
    {
        public bool ok;
        public string message;
    }

    [Serializable]
    public class LoginData
    {
        public string username;
        public string token;
    }

    [Serializable]
    public class LoginResult
    {
        public bool ok;
        public string message;
        public LoginData data;
    }

    /// <summary>上传成绩的请求体（对应后端 ScoreUploadRequest）。</summary>
    [Serializable]
    public class ScoreUploadData
    {
        public string gameId;
        public string sceneName;
        public int score;
        public int durationSeconds;
        public string detail;
    }

    [Serializable]
    public class ScoreSummaryEntry
    {
        public string gameId;
        public string gameName;
        public int times;
        public int best;
    }

    [Serializable]
    public class SummaryResult
    {
        public bool ok;
        public string message;
        public ScoreSummaryEntry[] data;
    }

    /// <summary>“我的”页面里的一条历史成绩（对应后端 ScoreRecord）。</summary>
    [Serializable]
    public class ScoreRecordEntry
    {
        public string gameId;
        public string gameName;
        public int score;
        public int durationSeconds;
        public string detail;
        public string playedAt;
    }

    [Serializable]
    public class MineResult
    {
        public bool ok;
        public string message;
        public ScoreRecordEntry[] data;
    }

    /// <summary>排行榜某个游戏分类的前三名中的一条（对应后端 GameTopRow）。</summary>
    [Serializable]
    public class TopEntry
    {
        public string gameId;
        public string gameName;
        public int rank;
        public string username;
        public int score;
        public string playedAt;
    }

    [Serializable]
    public class TopResult
    {
        public bool ok;
        public string message;
        public TopEntry[] data;
    }
}
