using System;

namespace TimerCheckApp.Models
{
    /// <summary>
    /// 表示日志条目的模型类
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 日志时间
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 相关设备ID
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public LogEntry()
        {
            Timestamp = DateTime.Now;
            Level = "Info";
            Message = string.Empty;
            DeviceId = string.Empty;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="deviceId">设备ID</param>
        public LogEntry(string level, string message, string deviceId)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Message = message;
            DeviceId = deviceId;
        }
    }
}