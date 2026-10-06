using System;
using EchoWorkSpace;
using UnityEngine;

namespace EchoWorkSpace.Net
{
    /// <summary>
    /// 成绩双写：本地 GameScoreStore 已入库后，把同一局异步上传服务器（fire-and-forget）。
    /// 没有服务器令牌（离线登录）或网络不通时静默跳过，绝不影响本地成绩与游戏流程。
    /// </summary>
    public static class ScoreSync
    {
        public static async void UploadAsync(ScoreRecord record)
        {
            try
            {
                string username = OnlineAuth.CurrentServerUsername;
                string token = OnlineAuth.CurrentServerToken;
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
                    return; // 离线模式：没有服务器令牌，静默跳过

                ScoreUploadData data = new ScoreUploadData
                {
                    gameId = record.gameId,
                    sceneName = record.sceneName,
                    score = record.score,
                    durationSeconds = record.durationSeconds,
                    detail = record.detail ?? string.Empty,
                };

                ApiMessage res = await ApiClient.UploadScoreAsync(username, token, data);
                if (res != null && !res.ok)
                    Debug.LogWarning("[ScoreSync] 成绩上传被服务器拒绝：" + res.message);
                // res == null：网络不通，静默放弃（本地已保存）
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ScoreSync] 成绩上传异常（不影响本地记录）：" + e.Message);
            }
        }
    }
}
