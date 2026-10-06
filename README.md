# 跃篮逐迹 · 自研代码复用指南

本目录只收录项目中**具有复用价值**的自研代码，按"通用程度"分组。第三方组件（MediaPipe、Unity 引擎、Spring Boot 等）不在此处。

---

## 目录结构

```
自研/
├─ README.md
├─ 后端通用模板/                 Spring Boot 账号 + 成绩微服务（可直接改造成任意后端）
│  ├─ src/main/java/com/yuelan/  18 个 Java 类
│  ├─ src/main/resources/schema.sql
│  ├─ pom.xml
│  └─ run-server.bat / build-server.bat
└─ Unity可复用组件/
   ├─ 01_通用工具/               与业务无关，拿来即用
   ├─ 02_姿态与手势/             体感/AR 项目核心
   ├─ 03_数据与网络/             弱网、离线优先应用
   ├─ 04_框架与场景/             平台分支加载、运行时 UI 框架
   └─ 05_Editor工具/             一键打包、出 APK 脚本
```

---

## 01 通用工具（零成本复用）

| 文件 | 功能 |
|---|---|
| `RoundedUIImage.cs` + `RoundedUIImage.shader` | UGUI 圆角描边图片组件 |
| `WebRequestRunner.cs` | 协程宿主单例，让静态类能跑协程 |

## 02 姿态与手势（体感项目通用）

| 文件 | 功能 |
|---|---|
| `PoseLandmarkerJointReceiver.cs` | 接收 MediaPipe 33 身体关节（防腐层） |
| `HandLandmarkerJointReceiver.cs` | 接收 MediaPipe 21 手部关键点 |
| `CanvasPoseLandmarkRenderer.cs` | UGUI 绘制骨架 + 输出食指触摸坐标 |
| `HandHitDetector.cs` + `HitTarget.cs` | 食指屏幕命中判定 |
| `FruitSliceDetector.cs` + `FruitTarget.cs` + `FruitDefinition.cs` | 手部轨迹线段-圆相交切割判定 |
| `JumpingJackPoseDetector.cs` | 关节距离归一化的姿态二分类 |
| `ShootingFormAnalyzer.cs` | 基于关节角度的规则引擎纠错框架 |
| `TwoPlayerIdentityTracker.cs` | 多人同屏身份追踪（肩中点锚定 + 歧义冻结 + 漏检宽限） |

## 03 数据与网络（弱网/离线优先应用通用）

| 文件 | 功能 |
|---|---|
| `LocalDatabase.cs` | `persistentDataPath` 下 JSON 持久化，加锁读写、事务式修改 |
| `GameScoreStore.cs` + `ScoreRecord.cs` + `GameCatalog.cs` | 成绩按用户/分类查询、上限控制、触发同步 |
| `OnlineAuth.cs` + `ApiClient.cs` + `ServerConfig.cs` + `ApiModels.cs` + `UserAccountStore.cs` + `UserAccount.cs` + `LoginSession.cs` | 在线优先、断网回退本地的双轨登录 |
| `ScoreSync.cs` | 先写本地、再异步上传、失败静默的同步模式 |

## 04 框架与场景

| 文件 | 功能 |
|---|---|
| `SceneBundleLoader.cs` + `SceneLoader.cs` | 平台分支：桌面直通 BuildSettings、安卓走 AssetBundle |
| `MainMenuFlowController.cs` | 运行时动态搭建 UI、底部页签、视频轮播的主菜单框架 |

## 05 Editor 工具

| 文件 | 功能 |
|---|---|
| `BuildSceneAssetBundles.cs` | 场景打包为 AssetBundle |
| `BuildAndroid.cs` / `ApkBuilder.cs` | 一键出 IL2CPP + ARM64 APK |

## 后端通用模板

完整的账号注册/登录/记住我 + 成绩上传/查询/排行榜后端，18 个 Java 类，标准 controller-service-dao 分层。可直接改造成任意需要账号与数据存储的后端服务。核心亮点：盐+SHA256 密码哈希、等时长比较、服务端白名单防客户端伪造、JdbcTemplate + 窗口函数排名。

---

## 复用注意事项

1. **许可证**：本目录自研代码以 MIT 协议开源，可自由使用、修改、商用。
2. **依赖**：Unity 脚本依赖 Unity 2022.3+ 与 MediaPipeUnityPlugin；后端依赖 Spring Boot 3.3.5 + MySQL 8。
3. **姿态脚本**：`*Receiver.cs` 依赖 MediaPipe 的 `OnResultOutput` 事件，复用需先跑通 MediaPipe。
4. **依赖文件**：每个分组已包含直接依赖（如 `OnlineAuth` 所需的 `ApiClient`、`UserAccountStore` 等都在同组），复制后通常只需补齐命名空间即可。
