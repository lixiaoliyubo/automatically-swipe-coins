using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using TimerCheckApp.Services.Interfaces;

namespace TimerCheckApp.Services
{
    /// <summary>
    /// 音频服务实现类
    /// </summary>
    public class AudioService : IAudioService
    {
        private readonly ILogService _logService;
        private readonly string _audioFolderPath;
        private SoundPlayer _soundPlayer;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logService">日志服务</param>
        public AudioService(ILogService logService)
        {
            _logService = logService;
            _audioFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "audio");
            
            // 确保音频文件夹存在
            EnsureAudioFolderExists();
            
            // 初始化音频播放器
            _soundPlayer = new SoundPlayer();
        }

        /// <summary>
        /// 确保音频文件夹存在
        /// </summary>
        private void EnsureAudioFolderExists()
        {
            if (!Directory.Exists(_audioFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(_audioFolderPath);
                    _logService.LogInfo($"创建音频文件夹: {_audioFolderPath}", "System");
                }
                catch (Exception ex)
                {
                    _logService.LogError($"创建音频文件夹失败: {ex.Message}", "System");
                }
            }
        }

        /// <summary>
        /// 播放音频文件
        /// </summary>
        /// <param name="fileName">音频文件名（不含路径）</param>
        /// <returns>播放任务</returns>
        public async Task PlayAsync(string fileName)
        {
            try
            {
                string filePath = GetAudioFilePath(fileName);
                
                if (!File.Exists(filePath))
                {
                    _logService.LogWarning($"音频文件不存在: {filePath}", "System");
                    return;
                }

                await Task.Run(() =>
                {
                    try
                    {
                        _soundPlayer.Stop();
                        _soundPlayer.SoundLocation = filePath;
                        _soundPlayer.Play();
                        _logService.LogInfo($"播放音频: {fileName}", "System");
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"播放音频失败: {ex.Message}", "System");
                    }
                });
            }
            catch (Exception ex)
            {
                _logService.LogError($"播放音频异常: {ex.Message}", "System");
            }
        }

        /// <summary>
        /// 停止所有音频播放
        /// </summary>
        public void StopAll()
        {
            try
            {
                _soundPlayer.Stop();
                _logService.LogInfo("停止所有音频播放", "System");
            }
            catch (Exception ex)
            {
                _logService.LogError($"停止音频播放失败: {ex.Message}", "System");
            }
        }

        /// <summary>
        /// 获取音频文件的完整路径
        /// </summary>
        /// <param name="fileName">音频文件名（不含路径）</param>
        /// <returns>完整路径</returns>
        public string GetAudioFilePath(string fileName)
        {
            return Path.Combine(_audioFolderPath, fileName);
        }

        /// <summary>
        /// 检查音频文件是否存在
        /// </summary>
        /// <param name="fileName">音频文件名（不含路径）</param>
        /// <returns>是否存在</returns>
        public bool AudioFileExists(string fileName)
        {
            string filePath = GetAudioFilePath(fileName);
            return File.Exists(filePath);
        }
    }
}