using System.Collections.Generic;

namespace EchoWorkSpace
{
    /// <summary>
    /// 游戏分类目录：场景名 ↔ 游戏分类 ID / 中文名 的唯一映射处。
    /// 新增游戏时只要在这里登记一行，成绩就会自动归到对应分类。
    /// </summary>
    public static class GameCatalog
    {
        public const string SingleBean = "single_bean";
        public const string MultiBean = "multi_bean";
        public const string MultiFruit = "multi_fruit";
        public const string JumpingJacks = "jumping_jacks";
        public const string Quiz = "quiz";
        public const string Other = "other";

        /// <summary>分类的固定展示顺序（成绩界面按此排序）。</summary>
        public static readonly string[] OrderedGameIds =
        {
            SingleBean, MultiBean, MultiFruit, JumpingJacks, Quiz, Other,
        };

        private sealed class GameInfo
        {
            public string Id;
            public string Name;
        }

        // 场景名 → 分类信息
        private static readonly Dictionary<string, GameInfo> SceneMap =
            new Dictionary<string, GameInfo>
            {
                { "TrainingSingleBean",          new GameInfo { Id = SingleBean,    Name = "单人吃豆训练" } },
                { "TrainingMultiBean",           new GameInfo { Id = MultiBean,     Name = "双人吃豆训练" } },
                { "TrainingMultiFruit",          new GameInfo { Id = MultiFruit,    Name = "趣味切水果"   } },
                { "TrainingSingleJumpingJacks",  new GameInfo { Id = JumpingJacks,  Name = "开合跳训练"   } },
                { "TrainingQuiz",                new GameInfo { Id = Quiz,          Name = "知识答题"     } },
            };

        // 分类 ID → 中文名
        private static readonly Dictionary<string, string> NameMap =
            new Dictionary<string, string>
            {
                { SingleBean,   "单人吃豆训练" },
                { MultiBean,    "双人吃豆训练" },
                { MultiFruit,   "趣味切水果"   },
                { JumpingJacks, "开合跳训练"   },
                { Quiz,         "知识答题"     },
                { Other,        "其他游戏"     },
            };

        /// <summary>按场景名解析游戏分类；未登记的场景归入“其他游戏”。</summary>
        public static void ResolveByScene(string sceneName, out string gameId, out string gameName)
        {
            if (!string.IsNullOrEmpty(sceneName) && SceneMap.TryGetValue(sceneName, out GameInfo info))
            {
                gameId = info.Id;
                gameName = info.Name;
                return;
            }

            gameId = Other;
            gameName = string.IsNullOrEmpty(sceneName) ? "其他游戏" : sceneName;
        }

        /// <summary>按分类 ID 取中文名，未知 ID 返回“其他游戏”。</summary>
        public static string GetGameName(string gameId)
        {
            return !string.IsNullOrEmpty(gameId) && NameMap.TryGetValue(gameId, out string name)
                ? name
                : "其他游戏";
        }
    }
}
