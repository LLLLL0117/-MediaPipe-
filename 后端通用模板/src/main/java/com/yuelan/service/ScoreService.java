package com.yuelan.service;

import com.yuelan.common.ApiException;
import com.yuelan.common.GameCatalog;
import com.yuelan.dao.ScoreDao;
import com.yuelan.dto.ScoreUploadRequest;
import com.yuelan.model.ScoreRecord;
import org.springframework.stereotype.Service;

import java.util.List;

/**
 * 成绩业务：鉴权后写入（服务端补全游戏分类名），以及各类查询。
 */
@Service
public class ScoreService {

    private final ScoreDao scoreDao;

    public ScoreService(ScoreDao scoreDao) {
        this.scoreDao = scoreDao;
    }

    public ScoreRecord upload(String username, ScoreUploadRequest req) {
        if (req == null) {
            throw new ApiException("成绩数据不能为空");
        }
        String gameId = req.getGameId();
        if (!GameCatalog.isValid(gameId)) {
            throw new ApiException("未知的游戏分类：" + gameId);
        }
        // 游戏中文名以服务端白名单为准，防止伪造
        String gameName = GameCatalog.getName(gameId);

        String detail = req.getDetail() == null ? "" : req.getDetail().trim();
        if (detail.length() > 120) {
            detail = detail.substring(0, 120);
        }

        return scoreDao.insert(username, gameId, gameName,
                req.getSceneName(), req.getScore(), req.getDurationSeconds(), detail);
    }

    public List<ScoreRecord> query(String gameId, String username) {
        boolean hasGame = gameId != null && !gameId.isBlank();
        boolean hasUser = username != null && !username.isBlank();
        if (hasGame && hasUser) {
            return scoreDao.listByGameAndUser(gameId.trim(), username.trim());
        }
        if (hasGame) {
            return scoreDao.listByGame(gameId.trim());
        }
        if (hasUser) {
            return scoreDao.listByUser(username.trim());
        }
        return scoreDao.recent(100);
    }

    public ScoreRecord best(String gameId, String username) {
        if (gameId == null || gameId.isBlank() || username == null || username.isBlank()) {
            throw new ApiException("查询最高分需要 gameId 和 username");
        }
        return scoreDao.best(gameId.trim(), username.trim());
    }

    public List<ScoreRecord> recent(int count) {
        return scoreDao.recent(count <= 0 ? 20 : count);
    }

    public List<ScoreDao.GameSummaryRow> summary() {
        return scoreDao.summary();
    }

    /** 每个游戏分类的前三名。 */
    public List<ScoreDao.GameTopRow> top() {
        return scoreDao.top3();
    }
}
