package com.yuelan;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

/**
 * 跃篮逐迹后端启动类。
 * 启动后服务常驻在 http://localhost:8080 ，等待 Unity 客户端调用接口。
 */
@SpringBootApplication
public class BasketballApplication {

    public static void main(String[] args) {
        SpringApplication.run(BasketballApplication.class, args);
    }
}
