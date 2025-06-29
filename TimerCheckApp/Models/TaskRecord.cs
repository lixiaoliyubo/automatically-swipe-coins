using SqlSugar;
using System;

namespace TimerCheckApp.Models
{
    /// <summary>
    /// 任务记录实体类
    /// </summary>
    [SugarTable("TaskRecords")]
    public class TaskRecord
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        [SugarColumn(Length = 100)]
        public string TaskName { get; set; }

        /// <summary>
        /// 平台类型
        /// </summary>
        [SugarColumn(Length = 50)]
        public string PlatformType { get; set; }

        /// <summary>
        /// 设备ID
        /// </summary>
        [SugarColumn(Length = 100)]
        public string DeviceId { get; set; }

        /// <summary>
        /// 执行状态
        /// </summary>
        [SugarColumn(Length = 20)]
        public string Status { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 执行时长（秒）
        /// </summary>
        public int Duration { get; set; }

        /// <summary>
        /// 执行结果
        /// </summary>
        [SugarColumn(Length = 500)]
        public string Result { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(IsOnlyIgnoreInsert = true)]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(Length = 1000, IsNullable = true)]
        public string Remark { get; set; }
    }
}