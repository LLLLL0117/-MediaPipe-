package com.yuelan.service;

import com.yuelan.common.ApiException;
import com.yuelan.dao.UserDao;
import com.yuelan.dto.LoginResponse;
import com.yuelan.model.UserAccount;
import com.yuelan.util.PasswordHasher;
import org.springframework.stereotype.Service;

import java.time.LocalDateTime;
import java.util.regex.Pattern;

/**
 * 账号业务：注册校验、登录签发令牌、令牌鉴权。
 */
@Service
public class UserService {

    private static final Pattern USERNAME_PATTERN =
            Pattern.compile("^[\\u4e00-\\u9fa5A-Za-z0-9_]{2,12}$");

    private final UserDao userDao;

    public UserService(UserDao userDao) {
        this.userDao = userDao;
    }

    public void register(String username, String password) {
        username = username == null ? "" : username.trim();

        if (username.isEmpty()) {
            throw new ApiException("账号不能为空");
        }
        if (!USERNAME_PATTERN.matcher(username).matches()) {
            throw new ApiException("账号需为 2-12 位中英文、数字或下划线");
        }
        if (password == null || password.length() < 6 || password.length() > 20) {
            throw new ApiException("密码要 6-20 位哦");
        }
        if (userDao.findByUsername(username) != null) {
            throw new ApiException("这个账号已经被注册啦，换一个试试 ♥");
        }

        String salt = PasswordHasher.newSalt();
        String hash = PasswordHasher.hashPassword(password, salt);
        userDao.insert(username, hash, salt, LocalDateTime.now().withSecond(0).withNano(0));
    }

    /** 登录成功签发令牌（库里只存令牌哈希）。 */
    public LoginResponse login(String username, String password) {
        username = username == null ? "" : username.trim();
        UserAccount account = userDao.findByUsername(username);
        if (account == null) {
            throw new ApiException("账号还不存在哦，先去注册一个吧");
        }
        String inputHash = PasswordHasher.hashPassword(password == null ? "" : password, account.getSalt());
        if (!PasswordHasher.fixedEquals(inputHash, account.getPasswordHash())) {
            throw new ApiException("密码不对哦，再想想看");
        }

        String token = PasswordHasher.newToken();
        userDao.updateTokenHash(username, PasswordHasher.hashToken(token),
                LocalDateTime.now().withSecond(0).withNano(0));
        return new LoginResponse(username, token);
    }

    /** 校验上传成绩等请求携带的 X-Username / X-Token，返回账号名。 */
    public String authenticate(String username, String token) {
        if (username == null || username.trim().isEmpty() || token == null || token.isEmpty()) {
            throw new ApiException("请先登录再操作");
        }
        username = username.trim();
        UserAccount account = userDao.findByUsername(username);
        if (account == null || account.getRememberTokenHash() == null
                || account.getRememberTokenHash().isEmpty()) {
            throw new ApiException("登录已过期，请重新登录");
        }
        if (!PasswordHasher.fixedEquals(PasswordHasher.hashToken(token), account.getRememberTokenHash())) {
            throw new ApiException("登录令牌无效，请重新登录");
        }
        return username;
    }
}
