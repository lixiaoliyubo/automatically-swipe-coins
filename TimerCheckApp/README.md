# 安卓自动刷视频辅助工具（主程序）

> 完整的功能说明、平台列表、环境配置与免责声明见仓库根目录的 [README.md](../README.md)。

## 排除文件夹
.vs
.vscode
.DS_Store
.idea
bin
obj
packages
Properties
.git
.gitignore


## 项目介绍

这是一个使用C# WPF MVVM模式开发的Android设备监控工具，可以实时监控连接的Android设备，定期检查设备当前运行的应用信息并进行屏幕截图OCR识别。

## 功能特点

- 自动检测并显示所有连接的Android设备
- 对每个设备可以单独启动/停止监控任务
- 每10秒自动检查设备当前运行的应用信息
- 自动截取设备屏幕并进行OCR文字识别
- 完整的日志记录系统，支持查看全局日志和设备专属日志
- 每天凌晨自动清理所有正在运行的应用和日志记录

## 技术栈

- 开发语言：C#
- UI框架：WPF
- 架构模式：MVVM
- 设备管理：AdvancedSharpAdbClient
- 日志管理：NLog
- MVVM实现：CommunityToolkit.Mvvm
- 交互行为：Microsoft.Xaml.Behaviors.Wpf
- OCR识别：PaddleOCRSharp

## 项目结构

- **Models**: 数据模型
  - DeviceModel.cs: 设备模型类
  - LogEntry.cs: 日志条目模型类
- **ViewModels**: 视图模型
  - MainViewModel.cs: 主视图模型类
- **Views**: 视图
  - MainView.xaml: 主视图
- **Services**: 服务
  - DeviceService.cs: 设备服务类
  - LogService.cs: 日志服务类
- **Converters**: 转换器
  - BoolToVisibilityConverter.cs: 布尔值到可见性转换器

## 使用说明

1. 启动应用程序，它会自动检测并显示所有已连接的Android设备
2. 点击设备列表中的「启动」按钮开始监控设备
3. 设备监控启动后，每10秒会自动检查设备当前运行的应用并进行屏幕截图OCR识别
4. 可以点击「查看日志」按钮查看特定设备的日志记录
5. 点击工具栏中的「所有日志」切换按钮可以查看全局日志
6. 点击「清理所有」按钮可以手动清理所有设备上运行的应用和日志记录

## 注意事项

- 使用前请确保已安装ADB工具并添加到系统环境变量
- 设备需要开启USB调试模式并授权连接
- 首次连接设备时可能需要在设备上确认授权请求
- 截图和日志文件会保存在应用程序目录下的Screenshots和Logs文件夹中

## 代码规范

### 属性注释格式

项目中的属性和字段应使用以下格式进行注释：

```csharp
/// <summary> 
/// 注释 
/// </summary> 
public 类型 名称 {get;set;}
```

示例：

```csharp
/// <summary> 
/// 设备ID 
/// </summary> 
public string DeviceId {get;set;}

/// <summary> 
/// 设备是否在线 
/// </summary> 
public bool IsOnline {get;set;}
```