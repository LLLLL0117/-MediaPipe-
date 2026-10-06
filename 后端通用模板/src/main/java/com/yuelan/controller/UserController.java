package com.yuelan.controller;

import com.yuelan.common.ApiResult;
import com.yuelan.dto.CredentialsRequest;
import com.yuelan.dto.LoginResponse;
import com.yuelan.service.UserService;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

/**
 * 账号接口：
 *   GET  /api/ping              连通性测试
 *   POST /api/register          注册
 *   POST /api/login             登录（返回 token）
 *   GET  /api/autologin         用 X-Username / X-Token 头校验“记住我”自动登录
 */
@RestController
@RequestMapping("/api")
public class UserController {

    private final UserService userService;

    public UserController(UserService userService) {
        this.userService = userService;
    }

    @GetMapping("/ping")
    public ApiResult<Map<String, String>> ping() {
        return ApiResult.success(Map.of("service", "basketball-server", "status", "running"));
    }

    @PostMapping("/register")
    public ApiResult<Void> register(@RequestBody CredentialsRequest request) {
        userService.register(request.getUsername(), request.getPassword());
        return ApiResult.success("注册成功，快去登录吧 ♥", null);
    }

    @PostMapping("/login")
    public ApiResult<LoginResponse> login(@RequestBody CredentialsRequest request) {
        LoginResponse data = userService.login(request.getUsername(), request.getPassword());
        return ApiResult.success("登录成功", data);
    }

    /** “记住我”自动登录：客户端启动时带上保存的 X-Username / X-Token 来校验。 */
    @GetMapping("/autologin")
    public ApiResult<Map<String, String>> autologin(
            @RequestHeader(value = "X-Username", required = false) String username,
            @RequestHeader(value = "X-Token", required = false) String token) {
        String verified = userService.authenticate(username, token);
        return ApiResult.success("自动登录成功", Map.of("username", verified));
    }
}
