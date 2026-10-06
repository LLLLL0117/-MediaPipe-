package com.yuelan.dao;

import com.yuelan.model.ScoreRecord;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.stereotype.Repository;

import java.sql.Timestamp;
import java.time.LocalDateTime;
import java.util.List;
import java.util.UUID;

/**
 * 成绩表数据访问：写入、按分类/账号查询、最高分、最近、分类汇总。
 */
@Repository
public class ScoreDao {

    private final JdbcTemplate jdbc;

    public ScoreDao(JdbcTemplate jdbc) {
        this.jdbc = jdbc;
    }

    private static final RowMapper<ScoreRecord> MAPPER = (rs, rowNum) -> {
        ScoreRecord r = new ScoreRecord();
        r.setId(rs.getString("id"));
        r.setUsername(rs.getString("username"));
        r.setGameId(rs.getString("game_id"));
        r.setGameName(rs.getString("game_name"));
        r.setSceneName(rs.getString("scene_name"));
        r.setScore(rs.getInt("score"));
        r.setDurationSeconds(rs.getInt("duration_seconds"));
        r.setDetail(rs.getString("detail"));
        Timestamp played = rs.getTimestamp("played_at");
        r.setPlayedAt(played == null ? null : played.toLocalDateTime());
        return r;
    };

    public ScoreRecord insert(String username, String gameId, String gameName, String sceneName,
                              int score, int durationSeconds, String detail) {
        ScoreRecord r = new ScoreRecord();
        r.setId(UUID.randomUUID().toString().replace("-", ""));
        r.setUsername(username);
        r.setGameId(gameId);
        r.setGameName(gameName);
        r.setSceneName(sceneName);
        r.setScore(Math.max(0, score));
        r.setDurationSeconds(Math.max(0, durationSeconds));
        r.setDetail(detail == null ? "" : detail);
        r.setPlayedAt(LocalDateTime.now().withSecond(0).withNano(0));

        jdbc.update("INSERT INTO score_record (id, username, game_id, game_name, scene_name,"
                        + " score, duration_seconds, detail, played_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                r.getId(), r.getUsername(), r.getGameId(), r.getGameName(), r.getSceneName(),
                r.getScore(), r.getDurationSeconds(), r.getDetail(), Timestamp.valueOf(r.getPlayedAt()));
        return r;
    }

    public List<ScoreRecord> listByGame(String gameId) {
        return jdbc.query("SELECT * FROM score_record WHERE game_id = ? ORDER BY played_at DESC, id DESC",
                MAPPER, gameId);
    }

    /** 某账号的全部成绩（最新在前），“我的”页面用。 */
    public List<ScoreRecord> listByUser(String username) {
        return jdbc.query("SELECT * FROM score_record WHERE username = ?"
                        + " ORDER BY played_at DESC, id DESC LIMIT 200",
                MAPPER, username);
    }

    public List<ScoreRecord> listByGameAndUser(String gameId, String username) {
        return jdbc.query("SELECT * FROM score_record WHERE game_id = ? AND username = ?"
                        + " ORDER BY played_at DESC, id DESC",
                MAPPER, gameId, username);
    }

    public ScoreRecord best(String gameId, String username) {
        List<ScoreRecord> list = jdbc.query("SELECT * FROM score_record WHERE game_id = ? AND username = ?"
                        + " ORDER BY score DESC, played_at ASC LIMIT 1",
                MAPPER, gameId, username);
        return list.isEmpty() ? null : list.get(0);
    }

    public List<ScoreRecord> recent(int count) {
        return jdbc.query("SELECT * FROM score_record ORDER BY played_at DESC, id DESC LIMIT ?",
                MAPPER, Math.max(1, Math.min(count, 200)));
    }

    /** 分类汇总：每条 [gameId, gameName, times, best]。 */
    public List<GameSummaryRow> summary() {
        return jdbc.query("SELECT game_id, game_name, COUNT(*) AS times, MAX(score) AS best"
                        + " FROM score_record GROUP BY game_id, game_name ORDER BY game_id",
                (rs, n) -> new GameSummaryRow(
                        rs.getString("game_id"),
                        rs.getString("game_name"),
                        rs.getInt("times"),
                        rs.getInt("best")));
    }

    /** 每个游戏分类的前三名（分数高优先，同分先达成优先）。 */
    public List<GameTopRow> top3() {
        return jdbc.query(
                "SELECT t.game_id, t.game_name, t.rn, t.username, t.score,"
                        + " DATE_FORMAT(t.played_at, '%Y-%m-%d %H:%i') AS played_at FROM ("
                        + "   SELECT s.game_id, s.game_name, s.username, s.score, s.played_at,"
                        + "          ROW_NUMBER() OVER (PARTITION BY s.game_id"
                        + "                             ORDER BY s.score DESC, s.played_at ASC) AS rn"
                        + "   FROM score_record s"
                        + ") t WHERE t.rn <= 3 ORDER BY t.game_id, t.rn",
                (rs, n) -> new GameTopRow(
                        rs.getString("game_id"),
                        rs.getString("game_name"),
                        rs.getInt("rn"),
                        rs.getString("username"),
                        rs.getInt("score"),
                        rs.getString("played_at")));
    }

    public int count() {
        Integer n = jdbc.queryForObject("SELECT COUNT(*) FROM score_record", Integer.class);
        return n == null ? 0 : n;
    }

    /** 分类汇总行。 */
    public record GameSummaryRow(String gameId, String gameName, int times, int best) {
    }

    /** 游戏前三名行。 */
    public record GameTopRow(String gameId, String gameName, int rank,
                             String username, int score, String playedAt) {
    }
}
