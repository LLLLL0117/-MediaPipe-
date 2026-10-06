@echo off
chcp 65001 >nul
cd /d %~dp0
title 跃篮逐迹 - Spring Boot 后端

echo ============================================
echo   跃篮逐迹后端启动中...
echo   接口地址 http://localhost:8080
echo ============================================
echo.

set JAVA_HOME=C:\Program Files\Java\jdk-17
set MYSQL_HOME=%~dp0..\tools\mysql-8.0.28-winx64

rem ── 检查 MySQL 是否已启动，没有就自动拉起 ──
netstat -ano | findstr ":3306" | findstr "LISTENING" >nul
if errorlevel 1 (
    echo MySQL 未启动，正在自动启动便携版 MySQL...
    start "MySQL" /min cmd /c ""%~dp0..\tools\start-mysql.bat""

    set /a tries=0
    :waitmysql
    timeout /t 1 >nul
    netstat -ano | findstr ":3306" | findstr "LISTENING" >nul
    if not errorlevel 1 goto mysqlready
    set /a tries+=1
    if %tries% LSS 30 goto waitmysql
    echo MySQL 启动超时了，请先双击 tools\start-mysql.bat 看看报错。
    pause
    exit /b 1
)
:mysqlready
echo MySQL 已就绪。

rem ── 首次运行兜底：确保 root 密码是 123456 ──
"%MYSQL_HOME%\bin\mysql.exe" -h127.0.0.1 -uroot -p123456 -e "SELECT 1;" >nul 2>&1
if errorlevel 1 (
    echo 首次初始化：设置 root 密码为 123456...
    "%MYSQL_HOME%\bin\mysql.exe" -h127.0.0.1 -uroot -e "ALTER USER 'root'@'localhost' IDENTIFIED BY '123456'; CREATE DATABASE IF NOT EXISTS basketball_db DEFAULT CHARACTER SET utf8mb4; FLUSH PRIVILEGES;"
)

echo.
"%JAVA_HOME%\bin\java.exe" -Dfile.encoding=UTF-8 -jar target\basketball-server-1.0.0.jar

echo.
echo 服务已停止。
pause
