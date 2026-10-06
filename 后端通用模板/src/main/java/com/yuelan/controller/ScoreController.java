package com.yuelan.controller;

import com.yuelan.common.ApiResult;
import com.yuelan.dao.ScoreDao;
import com.yuelan.dto.ScoreUploadRequest;
import com.yuelan.model.ScoreRecord;
import com.yuelan.service.ScoreService;
import com.yuelan.service.UserService;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;

/**
 * 成绩接口：
 *   POST /api/scores            上传一局成绩（需 X-Username / X-Token 头）
 *   GET  /api/scores            按分类/账号查询（参数 gameId、username，都不传返回最近100局）
 *   GET  /api/scores/recent     最近若干局（参数 count）
 *   GET  /api/scores/best       某账号在某分类的最高分（参数 gameId、username）
 *   GET  /api/scores/summary    全部分类的局数和最高分汇总
 *   GET  /api/scores/top        每个游戏分类的前三名（排行榜用）
 */
@RestController
@RequestMapping("/api/scores")
public class ScoreController {

    private final ScoreService scoreService;
    private final UserService userService;

    public ScoreController(ScoreService scoreService, UserService userService) {
        this.scoreService = scoreService;
        this.userService = userService;
    }

    @PostMapping
    public ApiResult<ScoreRecord> upload(
            @RequestHeader(value = "X-Username", required = false) String username,
            @RequestHeader(value = "X-Token", required = false) String token,
            @RequestBody ScoreUploadRequest request) {

        String verifiedUsername = userService.authenticate(username, token);
        ScoreRecord saved = scoreService.upload(verifiedUsername, request);
        return ApiResult.success("成绩已上传", saved);
    }

    @GetMapping
    public ApiResult<List<ScoreRecord>> query(
            @RequestParam(value = "gameId", required = false) String gameId,
            @RequestParam(value = "username", required = false) String username) {
        return ApiResult.success(scoreService.query(gameId, username));
    }

    @GetMapping("/recent")
    public ApiResult<List<ScoreRecord>> recent(@RequestParam(value = "count", defaultValue = "20") int count) {
        return ApiResult.success(scoreService.recent(count));
    }

    @GetMapping("/best")
    public ApiResult<ScoreRecord> best(
            @RequestParam("gameId") String gameId,
            @RequestParam("username") String username) {
        return ApiResult.success(scoreService.best(gameId, username));
    }

    @GetMapping("/summary")
    public ApiResult<List<ScoreDao.GameSummaryRow>> summary() {
        return ApiResult.success(scoreService.summary());
    }

    @GetMapping("/top")
    public ApiResult<List<ScoreDao.GameTopRow>> top() {
        return ApiResult.success(scoreService.top());
    }
}
