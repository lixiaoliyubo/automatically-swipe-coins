# 音频服务使用说明

## 概述

音频服务(`AudioService`)是一个全局单例服务，用于在应用中播放音频文件。音频文件存放在应用根目录下的`Resources/audio`文件夹中。

## 音频文件夹

音频文件夹位置：`[应用根目录]/Resources/audio/`

如果音频文件夹不存在，服务会在首次初始化时自动创建。

## 支持的音频格式

音频服务使用`System.Media.SoundPlayer`播放音频，主要支持以下格式：
- WAV格式（推荐）

## 使用方法

### 1. 通过依赖注入获取服务

```csharp
private readonly IAudioService _audioService;

public YourClass(IAudioService audioService)
{
    _audioService = audioService;
}
```

### 2. 通过DeviceService使用

```csharp
private readonly IDeviceService _deviceService;

public YourClass(IDeviceService deviceService)
{
    _deviceService = deviceService;
}

public async Task YourMethod()
{
    // 播放音频
    await _deviceService.PlayAudioAsync("notification.wav");
    
    // 停止所有音频播放
    _deviceService.StopAllAudio();
}
```

### 3. 直接使用AudioService

```csharp
private readonly IAudioService _audioService;

public YourClass(IAudioService audioService)
{
    _audioService = audioService;
}

public async Task YourMethod()
{
    // 播放音频
    await _audioService.PlayAsync("notification.wav");
    
    // 停止所有音频播放
    _audioService.StopAll();
    
    // 检查音频文件是否存在
    bool exists = _audioService.AudioFileExists("notification.wav");
    
    // 获取音频文件完整路径
    string path = _audioService.GetAudioFilePath("notification.wav");
}
```

## 注意事项

1. 音频服务是全局单例，只会初始化一次
2. 播放新音频时会自动停止当前正在播放的音频
3. 应用关闭时会自动停止所有音频播放
4. 如果音频文件不存在，播放方法会记录警告日志并安全返回
5. 请确保音频文件名称不包含特殊字符，以避免路径解析问题