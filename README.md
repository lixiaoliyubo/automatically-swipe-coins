# 安卓自动刷视频辅助工具

基于 **C# + WPF + ADB + OCR** 的 Android 多设备自动化辅助工具。通过 USB 调试连接安卓设备，自动完成短视频平台的刷视频、看广告、签到打卡等重复任务，并提供设备管理、任务调度、日志查看与语音告警能力。

> 本项目仅用于个人设备的自动化学习与研究。请在使用前阅读文末的[免责声明](#免责声明)。

## 功能特性

- **多设备管理**：自动发现已连接的 ADB 设备，支持按设备单独启动/停止任务、刷新设备列表、一键清理设备后台应用。
- **多平台自动化**：内置抖音、抖音极速版、快手、快手极速版、西瓜视频、番茄畅听、番茄小说、红果短剧、河马剧场、支付宝等平台的页面识别与任务流程。
- **OCR 页面识别**：通过 ADB 抓取屏幕帧，使用 PaddleOCRSharp 识别界面文字，结合关键词与页面状态机完成点击、滑动、返回等操作。
- **任务调度**：支持检查间隔（分/秒）、随机暂停区间、重复次数、夜间设备检测与每日零点清理。
- **每日打卡**：独立的每日打卡流程，自动处理指定视频、广告、限时视频等打卡任务。
- **日志与告警**：NLog 按天写入日志文件；关键异常通过语音播报提醒；界面可查看全局日志或单设备日志。
- **数据持久化**：SQLite（SqlSugar）保存任务记录，内存缓存保存设备任务状态，所有状态按设备 ID 隔离。
- **WebSocket 扩展**：可连接本地 `ws://127.0.0.1:33332` 桥接服务，用于获取设备信息、截图以及转发点击/滑动指令。
- **界面主题**：基于 Yuandl.ThemeUI 的 Fluent 风格界面，支持明暗主题切换与应用重启。

## 已支持平台

平台与包名的对应关系定义在 [`TimerCheckApp/enums/PlatformType.cs`](TimerCheckApp/enums/PlatformType.cs)。

| 平台 | 包名 | 说明 |
| --- | --- | --- |
| 抖音 | `com.ss.android.ugc.aweme` | 刷视频、看广告、任务中心 |
| 抖音极速版 | `com.ss.android.ugc.aweme.lite` | 刷视频、看广告、任务中心 |
| 快手 | `com.smile.gifmaker` | 刷视频、直播间、金币购、逛街、打卡 |
| 快手极速版 | `com.kuaishou.nebula` | 刷视频、任务中心 |
| 西瓜视频（极速版） | `com.ss.android.article.lite` | 刷视频、金币任务 |
| 番茄畅听 | `com.xs.fm` | 听书、刷视频任务 |
| 番茄畅听音乐版 | `com.xs.fm.lite` | 听书、刷视频任务 |
| 番茄小说 | `com.dragon.read` | 看短剧、刷视频任务 |
| 红果短剧 | `com.phoenix.read` | 看短剧任务 |
| 河马剧场 | `com.dz.hmjc` | 看短剧任务 |
| 支付宝 | `com.eg.android.AlipayGphone` | 视频红包任务 |
| 每日打卡 | - | 独立打卡流程 |

> 应用版本更新后界面文案可能变化，识别规则需要同步调整，详见[常见问题](#常见问题)。

## 技术栈

| 类别 | 技术 |
| --- | --- |
| 语言 / 框架 | C# 12、.NET 8（`net8.0-windows`）、WPF |
| 架构模式 | MVVM（CommunityToolkit.Mvvm） |
| 设备通信 | AdvancedSharpAdbClient、ADB |
| 屏幕识别 | PaddleOCRSharp、OpenCvSharp4 |
| 日志 | NLog |
| 数据存储 | SqlSugar + SQLite、`Microsoft.Extensions.Caching.Memory` |
| 依赖注入 | Microsoft.Extensions.DependencyInjection |
| 界面 | Yuandl.ThemeUI、Microsoft.Xaml.Behaviors.Wpf |
| 音频 | System.Speech（TTS） |

## 目录结构

```text
.
├── TimerCheckApp/                 # 主程序（WPF）
│   ├── AdWatching/                # 各平台自动化流程
│   │   ├── AdWatchingBase.cs      # 点击、滑动、OCR、任务状态等公共能力
│   │   ├── AdWatchingFactory.cs   # 平台实例工厂
│   │   ├── DouyinAdWatching.cs
│   │   ├── KuaishouAdWatching.cs
│   │   ├── DayClockAdWatching.cs
│   │   └── ...
│   ├── Models/                    # 设备、任务、日志、WebSocket 数据模型
│   ├── Services/                  # 设备、日志、缓存、数据库、音频、WebSocket 服务
│   ├── ViewModels/                # 主视图模型
│   ├── Views/                     # 主视图
│   ├── enums/                     # 平台、页面枚举
│   ├── Resources/                 # 样式与模板图片
│   └── NLog.config                # 日志配置
├── scrcpy/                        # 可选的 scrcpy/adb 运行库
├── Yuandl.Common.dll              # 主程序引用的本地组件
├── 安卓自动刷视频辅助工具.sln      # 解决方案
├── README.md
└── LICENSE
```

## 环境要求

- Windows 10 / 11（x64）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Android 设备已开启「开发者选项 → USB 调试」，并通过 USB 连接电脑
- ADB 可用：将 `adb` 加入系统 `PATH`，或使用仓库中 `scrcpy/adb.exe` 所在目录
- 设备已安装需要自动化的目标应用

> 首次连接设备时，请在手机端确认 USB 调试授权。

## 快速开始

1. 克隆仓库并进入项目目录：

   ```bash
   git clone https://gitee.com/lixiaoliyubo/automatically-swipe-coins.git
   cd automatically-swipe-coins
   ```

2. 使用 Visual Studio 2022 打开 `安卓自动刷视频辅助工具.sln`，或使用命令行构建：

   ```bash
   dotnet restore
   dotnet build TimerCheckApp/TimerCheckApp.csproj -c Release
   ```

3. 确认设备连接正常：

   ```bash
   adb devices
   ```

   设备状态应为 `device`，而不是 `unauthorized` 或 `offline`。

4. 运行程序：

   ```bash
   dotnet run --project TimerCheckApp/TimerCheckApp.csproj
   ```

   也可以在 `bin/Release/net8.0-windows/` 下直接运行 `TimerCheckApp.exe`。

## 使用说明

1. 启动程序后，在「ADB设备」页点击「刷新设备」，程序会自动读取设备列表与屏幕分辨率。
2. 在设备行中设置任务参数：
   - **间隔时间**：两次检查之间的间隔（分 + 秒）。
   - **随机暂停**：在固定间隔之外附加的随机等待区间（秒）。
   - **重复次数**：任务循环次数，`0` 表示不限制。
3. 选择平台后点击「启动」，程序会按平台流程执行自动化任务；点击「停止」结束该设备任务。
4. 「清理应用」可强制停止设备上的后台应用；「清理所有」会清理全部设备并清空日志。
5. 日志区域可切换「所有日志 / 单设备日志」，支持自动滚动到最新日志。
6. 程序每天 0 点自动清理任务与日志；夜间可开启设备在线检测，设备掉线时会记录并在界面提示。

### WebSocket 模式

「WebSocket」页用于连接本地桥接服务（默认 `ws://127.0.0.1:33332`）：

- 连接后可获取桥接服务上报的设备列表。
- 支持发起截图、点击、滑动等指令。
- 需要自行提供符合协议的服务端，协议模型见 [`TimerCheckApp/Models/WebSocketModels.cs`](TimerCheckApp/Models/WebSocketModels.cs)。

## 配置说明

| 配置项 | 位置 | 说明 |
| --- | --- | --- |
| 日志级别与输出 | `TimerCheckApp/NLog.config` | 默认写入 `Logs/日期.log` |
| SQLite 数据库 | `Database/AdAuto.db` | 首次运行自动创建 |
| 截图目录 | `Screenshots/` | 程序启动时自动创建 |
| 日志目录 | `Logs/` | 程序启动时自动创建 |
| WebSocket 地址 | `TimerCheckApp/Services/DependencyInjection/ServiceCollectionExtensions.cs` | 默认 `ws://127.0.0.1:33332` |

> `Logs/`、`Screenshots/`、`Database/` 均位于程序运行目录下，不纳入版本控制。

## 工作原理

```text
ADB 连接设备
   │
   ├─ 启动应用 / 读取当前包名
   │
   ├─ Framebuffer 截图 ──► PaddleOCR 文字识别
   │
   ├─ 关键词匹配 + 页面状态机（AppPageEnum）
   │
   ├─ 点击 / 滑动 / 返回键
   │
   └─ 记录任务时间、缓存状态、写入日志与数据库
```

各平台流程继承 `AdWatchingBase`，复用点击、滑动、OCR、返回、任务状态判断等公共方法，只实现自身的页面识别与任务分支。

## 常见问题

**Q：程序启动后找不到设备？**

检查 USB 调试是否开启、数据线是否支持传输、`adb devices` 是否能识别；若显示 `unauthorized`，请在手机上确认授权弹窗。

**Q：提示「启动 ADB 服务器失败」？**

确保 `adb` 已加入 `PATH`。也可以将 `scrcpy` 目录加入 `PATH`，或把 `adb.exe` 复制到系统目录。

**Q：OCR 识别不到界面文字？**

确认设备已解锁且屏幕常亮；部分系统需要关闭「隐私屏/安全屏」；分辨率过低时识别率会下降。

**Q：任务在某个页面反复点击或卡住？**

目标应用更新后文案或布局可能变化，需要在对应 `AdWatching` 类中调整关键词与匹配规则。

**Q：界面提示语音但听不到声音？**

检查系统音量与默认语音引擎；TTS 使用 Windows 系统语音，需在「设置 → 时间和语言 → 语音」中安装中文语音包。

**Q：数据库或日志在哪里？**

在程序运行目录的 `Database/`、`Logs/`、`Screenshots/` 下；这些目录不会提交到仓库。

## 参与贡献

1. Fork 本仓库并新建分支：`git checkout -b feat/your-feature`
2. 提交改动：`git commit -m "feat: 描述你的改动"`
3. 推送分支并创建 Pull Request
4. 涉及新平台时，请同时补充 `PlatformType`、工厂映射与页面识别说明

## 免责声明

- 本项目仅供个人学习、自动化技术研究与自有设备测试使用，不得用于商业用途或大规模批量操作。
- 使用本工具可能违反目标应用的《用户协议》或平台规则，可能导致账号被限制或封禁，请自行评估风险。
- 本项目不提供任何账号、设备或网络资源，不保证自动化流程的稳定性与收益。
- 因使用本项目产生的任何直接或间接损失，作者不承担责任。

## 许可证

本项目基于 [MIT License](LICENSE) 开源。
