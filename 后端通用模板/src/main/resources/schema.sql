-- Spring Boot 启动时自动执行（spring.sql.init.mode=always）
-- 数据库由 JDBC 参数 createDatabaseIfNotExist=true 自动创建

CREATE TABLE IF NOT EXISTS account (
    id                  BIGINT       NOT NULL AUTO_INCREMENT COMMENT '主键',
    username            VARCHAR(32)  NOT NULL COMMENT '登录账号（唯一）',
    password_hash       CHAR(44)     NOT NULL COMMENT '密码加盐后的SHA256(Base64)',
    salt                VARCHAR(32)  NOT NULL COMMENT '账号专属随机盐(Base64)',
    remember_token_hash VARCHAR(64)  NOT NULL DEFAULT '' COMMENT '登录令牌哈希',
    created_at          DATETIME     NOT NULL COMMENT '注册时间',
    updated_at          DATETIME     NOT NULL COMMENT '资料更新时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_account_username (username)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COMMENT = '用户账号表';

CREATE TABLE IF NOT EXISTS score_record (
    id               CHAR(32)    NOT NULL COMMENT '记录唯一编号(32位GUID)',
    username         VARCHAR(32) NOT NULL COMMENT '取得成绩的账号',
    game_id          VARCHAR(32) NOT NULL COMMENT '游戏分类ID',
    game_name        VARCHAR(32) NOT NULL COMMENT '游戏分类中文名',
    scene_name       VARCHAR(64)          DEFAULT NULL COMMENT '来源场景名',
    score            INT         NOT NULL DEFAULT 0 COMMENT '最终分数',
    duration_seconds INT         NOT NULL DEFAULT 0 COMMENT '本局时长(秒)',
    detail           VARCHAR(128)         DEFAULT '' COMMENT '附加明细(答对数/胜负等)',
    played_at        DATETIME    NOT NULL COMMENT '游玩时间',
    PRIMARY KEY (id),
    KEY idx_score_user_game (username, game_id),
    KEY idx_score_game (game_id),
    KEY idx_score_time (played_at),
    CONSTRAINT fk_score_account
        FOREIGN KEY (username) REFERENCES account (username)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COMMENT = '游戏成绩表（按游戏分类存储）';
