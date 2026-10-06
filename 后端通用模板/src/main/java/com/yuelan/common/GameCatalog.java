package com.yuelan.common;

import java.util.LinkedHashMap;
import java.util.Map;

/**
 * 游戏分类白名单（与 Unity 端 GameCatalog 保持一致）。
 * 成绩上传时服务端按 gameId 补全中文名，不信任客户端传来的名字。
 */
public final class GameCatalog {

    public static final String OTHER = "other";

    private static final Map<String, String> GAMES = new LinkedHashMap<>();

    static {
        GAMES.put("single_bean", "单人吃豆训练");
        GAMES.put("multi_bean", "双人吃豆训练");
        GAMES.put("multi_fruit", "趣味切水果");
        GAMES.put("jumping_jacks", "开合跳训练");
        GAMES.put("quiz", "知识答题");
        GAMES.put(OTHER, "其他游戏");
    }

    private GameCatalog() {
    }

    public static String getName(String gameId) {
        if (gameId != null && GAMES.containsKey(gameId)) {
            return GAMES.get(gameId);
        }
        return GAMES.get(OTHER);
    }

    public static boolean isValid(String gameId) {
        return gameId != null && GAMES.containsKey(gameId);
    }
}
