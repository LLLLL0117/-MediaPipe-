using System;

namespace EchoWorkSpace
{
    /// <summary>
    /// 单局游戏成绩记录（保存到 persistentDataPath 的 basketball_scores.json）。
    /// 每条记录带游戏分类 ID 和中文名，天然按游戏分类存储。
    /// </summary>
    [Serializable]
    public sealed class ScoreRecord
    {
        /// <summary>记录唯一编号（时间戳 + 随机数，防止同一秒多局冲突）。</summary>
        public string id;

        /// <summary>取得该成绩的账号名。</summary>
        public string username;

        /// <summary>游戏分类 ID，见 GameCatalog（single_bean / multi_bean / multi_fruit / jumping_jacks / quiz）。</summary>
        public string gameId;

        /// <summary>游戏分类中文名，例如“单人吃豆训练”，界面直接展示用。</summary>
        public string gameName;

        /// <summary>成绩来源场景名。</summary>
        public string sceneName;

        /// <summary>最终分数。</summary>
        public int score;

        /// <summary>本局时长（秒）；答题等无倒计时模式为 0。</summary>
        public int durationSeconds;

        /// <summary>附加明细，例如“答对 8 / 10 题，最高连胜 5 题”，没有则为空。</summary>
        public string detail;

        /// <summary>游玩时间（yyyy-MM-dd HH:mm）。</summary>
        public string playedAt;
    }
}
