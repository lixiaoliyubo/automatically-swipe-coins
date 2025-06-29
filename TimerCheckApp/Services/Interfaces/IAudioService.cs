using System;
using System.Threading.Tasks;

namespace TimerCheckApp.Services.Interfaces
{
    /// <summary>
    /// 音频服务接口
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// 播放音频文件
        /// </summary>
        /// <param name="fileName">音频文件名（不含路径）</param>
        /// <returns>播放任务</returns>
        Task PlayAsync(string fileName);

        /// <summary>
        /// 停止所有音频播放
        /// </summary>
        void StopAll();

        /// <summary>
        /// 获取音频文件的完整路径
        /// </summary>
        /// <param name="fileName">音频文件名（不含路径）</param>
        /// <returns>完整路径</returns>
        string GetAudioFilePath(string fileName);

        /// <summary>
        /// 检查音频文件是否存在
        /// </summary>
        /// <param name="fileName">音频文件名（不含路径）</param>
        /// <returns>是否存在</returns>
        bool AudioFileExists(string fileName);
    }
}