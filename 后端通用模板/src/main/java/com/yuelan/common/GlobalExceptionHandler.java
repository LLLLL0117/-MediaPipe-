package com.yuelan.common;

import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;

/**
 * 全局异常处理：任何接口抛业务异常都返回统一 JSON，而不是 Tomcat 错误页。
 */
@RestControllerAdvice
public class GlobalExceptionHandler {

    @ExceptionHandler(ApiException.class)
    public ApiResult<Void> handleBusiness(ApiException e) {
        return ApiResult.fail(e.getMessage());
    }

    @ExceptionHandler(Exception.class)
    public ApiResult<Void> handleOther(Exception e) {
        return ApiResult.fail("服务器开小差了：" + e.getMessage());
    }
}
