package com.yuelan.common;

/**
 * 业务异常：抛出后由 GlobalExceptionHandler 转成 { ok=false, message } 返回。
 */
public class ApiException extends RuntimeException {

    public ApiException(String message) {
        super(message);
    }
}
