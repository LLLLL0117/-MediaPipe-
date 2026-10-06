package com.yuelan.dao;

import com.yuelan.model.UserAccount;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.stereotype.Repository;

import java.sql.Timestamp;
import java.time.LocalDateTime;
import java.util.List;

/**
 * 账号表数据访问（Spring JdbcTemplate）。
 */
@Repository
public class UserDao {

    private final JdbcTemplate jdbc;

    public UserDao(JdbcTemplate jdbc) {
        this.jdbc = jdbc;
    }

    private static final RowMapper<UserAccount> MAPPER = (rs, rowNum) -> {
        UserAccount a = new UserAccount();
        a.setId(rs.getLong("id"));
        a.setUsername(rs.getString("username"));
        a.setPasswordHash(rs.getString("password_hash"));
        a.setSalt(rs.getString("salt"));
        a.setRememberTokenHash(rs.getString("remember_token_hash"));
        Timestamp created = rs.getTimestamp("created_at");
        a.setCreatedAt(created == null ? null : created.toLocalDateTime());
        Timestamp updated = rs.getTimestamp("updated_at");
        a.setUpdatedAt(updated == null ? null : updated.toLocalDateTime());
        return a;
    };

    public UserAccount findByUsername(String username) {
        List<UserAccount> list = jdbc.query(
                "SELECT id, username, password_hash, salt, remember_token_hash, created_at, updated_at"
                        + " FROM account WHERE username = ?",
                MAPPER, username);
        return list.isEmpty() ? null : list.get(0);
    }

    public void insert(String username, String passwordHash, String salt, LocalDateTime now) {
        jdbc.update("INSERT INTO account (username, password_hash, salt, remember_token_hash, created_at, updated_at)"
                        + " VALUES (?, ?, ?, '', ?, ?)",
                username, passwordHash, salt, Timestamp.valueOf(now), Timestamp.valueOf(now));
    }

    public void updateTokenHash(String username, String tokenHash, LocalDateTime now) {
        jdbc.update("UPDATE account SET remember_token_hash = ?, updated_at = ? WHERE username = ?",
                tokenHash, Timestamp.valueOf(now), username);
    }

    public int count() {
        Integer n = jdbc.queryForObject("SELECT COUNT(*) FROM account", Integer.class);
        return n == null ? 0 : n;
    }
}
