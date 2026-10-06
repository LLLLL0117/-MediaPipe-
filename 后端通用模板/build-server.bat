@echo off
chcp 65001 >nul
cd /d %~dp0
title 跃篮逐迹 - 重新打包后端

echo 正在重新编译打包（修改 Java 代码后运行此脚本）...
set JAVA_HOME=C:\Program Files\Java\jdk-17
"%~dp0..\tools\apache-maven-3.9.9\bin\mvn.cmd" -DskipTests clean package

echo.
pause
