# 跃篮逐迹 · 自研代码复用指南

本目录收录"跃篮逐迹"项目的全部自研代码，分为 **后端（Java / Spring Boot）** 和 **Unity 客户端（C#）** 两部分。本指南说明哪些代码可以被其他项目复用、如何接入，以及注意事项。

> 自研边界：MediaPipe 模型、Unity 引擎、Spring Boot 等第三方组件不在本目录，代码中的注释和本指南已如实标注。

---

## 目录结构

```
自研/
├─ 后端/BasketballServer/
│  ├─ src/main/java/com/yuelan/
│  │  ├─ controller/   接口层（账号、成绩）
│  │  ├─ service/      业务层
│  │  ├─ dao/          数据访问层
│  │  ├─ model/ dto/   数据模型
│  │  ├─ common/       通用组件（统一返回、异常、游戏分类）
│  │  ├─ config/       跨域配置
│  │  └─ util/         密码哈希工具
│  └─ src/main/resources/schema.sql   建表脚本
└─ Unity客户端/
   ├─ Scripts/   70 个业务脚本
   └─ Editor/    8 个编辑器工具脚本
```

---

## 一、几乎零成本即可复用（与业务无关）

| 文件 | 语言 | 功能 | 接入方式 |
|---|---|---|---|
| `后端/util/PasswordHasher.java` | Java | 16 字节盐 + SHA-256 密码哈希、32 字节随机 token、等时长比较 | 静态工具类，直接调用 `hash()`/`verify()`/`generateToken()` |
| `后端/common/ApiResult.java` | Java | `{ok, message, data}` 统一返回体 | controller 返回值统一包装 |
| `后端/common/GlobalExceptionHandler.java` | Java | 全局异常捕获，返回统一 JSON | `@RestControllerAdvice`，放包里自动生效 |
| `后端/config/WebCorsConfig.java` | Java | `/api/**` 跨域配置 | 复制后改路径前缀即可 |
| `Unity客户端/Scripts/RoundedUIImage.cs` + 同名 shader | C# | UGUI 圆角描边图片组件 | 挂到 Image 上，调参数 |
| `Unity客户端/Scripts/Api/WebRequestRunner.cs` | C# | 协程宿主单例，让静态类能跑协程 | `WebRequestRunner.Instance.StartCoroutine(...)` |

---

## 二、体感 / AI / 教育项目通用（核心复用价值）

### 姿态与手势

| 文件 | 功能 | 复用说明 |
|---|---|---|
| `Scripts/PoseLandmarkerJointReceiver.cs` | 接收 MediaPipe 33 身体关节 | 与具体玩法解耦的防腐层，换模型只改这一层 |
| `Scripts/HandLandmarkerJointReceiver.cs` | 接收 MediaPipe 21 手部关键点 | 同上，支持多人 |
| `Scripts/CanvasPoseLandmarkRenderer.cs` | UGUI 绘制骨架并输出食指触摸坐标 | 体感项目通用可视化组件 |
| `Scripts/HandHitDetector.cs` | 食指屏幕坐标命中判定 | 手势点击/触碰交互通用 |
| `Scripts/Fruit/FruitSliceDetector.cs` | 手部轨迹线段与圆相交判定 | "切割"类交互的几何基础，不局限于切水果 |
| `Scripts/JumpingJacks/JumpingJackPoseDetector.cs` | 关节距离归一化做姿态二分类 | 开合跳可换深蹲、高抬腿等任意两状态动作 |
| `Scripts/Analysis/ShootingFormAnalyzer.cs` | 基于关节角度的规则引擎纠错 | 换成其他动作只需改规则表 |
| `Scripts/TwoPlayerIdentityTracker.cs` | 多人同屏身份追踪（肩中点锚定 + 歧义冻结 + 漏检宽限） | 多人体感项目的核心难点解决方案 |

### 数据与网络

| 文件 | 功能 | 复用说明 |
|---|---|---|
| `Scripts/Database/LocalDatabase.cs` | `persistentDataPath` 下 JSON 持久化，加锁读写、事务式修改 | 移动端离线存储通用方案 |
| `Scripts/Stats/GameScoreStore.cs` | 成绩按用户/分类查询、上限控制、触发同步 | 游戏与教育应用的成绩存储模式 |
| `Scripts/Api/OnlineAuth.cs` | 在线优先、断网自动回退本地的双轨登录 | 弱网环境通用登录方案 |
| `Scripts/Api/ScoreSync.cs` | "先写本地、再异步上传、失败静默"同步模式 | 离线优先应用通用 |
| `后端/common/GameCatalog.java` + 客户端 `Scripts/Stats/GameCatalog.cs` | 场景名→业务分类映射，服务端白名单防伪造 | 防客户端篡改分类的设计思路 |

---

## 三、可借鉴思路，需按项目改造

- `Scripts/SceneBundleLoader.cs`：平台分支加载（桌面直通 BuildSettings、安卓走 AssetBundle）——思路通用，bundle 名按项目改
- `Scripts/MainMenuFlowController.cs`：运行时动态搭建 UI、底部页签、视频轮播的框架
- `Editor/BuildSceneAssetBundles.cs`、`Editor/BuildAndroid.cs`、`Editor/ApkBuilder.cs`：Editor 一键打包脚本模板

---

## 四、与本项目强绑定，复用价值低

`LoginScreen.cs`、`GameManager.cs`、`HitTargetSpawner.cs`、`Quiz/QuizQuestionBank.cs`、具体题库数据、美术与语音资源——这些与"篮球训练"具体玩法耦合，改造成本通常大于重写。

---

## 五、复用注意事项

1. **许可证**：本目录自研代码以 MIT 协议开源，可自由使用、修改、商用。第三方组件（MediaPipe、Spring Boot 等）不在本目录，使用时需遵守各自协议并保留声明。
2. **依赖**：Unity 脚本依赖 Unity 2022.3+ 与 MediaPipeUnityPlugin；后端依赖 Spring Boot 3.3.5 + MySQL 8。接入前确认环境。
3. **姿态脚本**：`*Receiver.cs` 依赖 MediaPipe 的 `OnResultOutput` 事件，复用需先在项目里跑通 MediaPipe。
4. **数据安全**：`PasswordHasher` 的盐+哈希算法已在前后端保持一致，复用时勿自行简化。
