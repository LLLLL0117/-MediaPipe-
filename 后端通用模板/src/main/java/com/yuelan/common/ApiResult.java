package com.yuelan.common;

/**
 * 统一接口返回格式：{ ok, message, data }，Unity 端按这个格式解析。
 */
public class ApiResult<T> {

    private boolean ok;
    private String message;
    private T data;

    public ApiResult() {
    }

    private ApiResult(boolean ok, String message, T data) {
        this.ok = ok;
        this.message = message;
        this.data = data;
    }

    public static <T> ApiResult<T> success(T data) {
        return new ApiResult<>(true, "success", data);
    }

    public static <T> ApiResult<T> success(String message, T data) {
        return new ApiResult<>(true, message, data);
    }

    public static <T> ApiResult<T> fail(String message) {
        return new ApiResult<>(false, message, null);
    }

    public boolean isOk() {
        return ok;
    }

    public void setOk(boolean ok) {
        this.ok = ok;
    }

    public String getMessage() {
        return message;
    }

    public void setMessage(String message) {
        this.message = message;
    }

    public T getData() {
        return data;
    }

    public void setData(T data) {
        this.data = data;
    }
}
