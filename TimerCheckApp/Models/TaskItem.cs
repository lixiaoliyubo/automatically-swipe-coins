using System;

namespace TimerCheckApp.Models
{
    /// <summary>
    /// 任务项实体类
    /// </summary>
    public class TaskItem
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 初始时间
        /// </summary>
        public DateTime InitialTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 任务值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskItem()
        {
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        /// <param name="name">任务名称</param>
        /// <param name="value">任务值</param>
        public TaskItem(string name, string value = "")
        {
            Name = name;
            Value = value;
            InitialTime = DateTime.Now;
            UpdateTime = DateTime.Now;
        }
        /*
        /// <summary>
        /// 更新任务值
        /// </summary>
        /// <param name="value">新的任务值</param>
        public void UpdateValue(string value)
        {
            Value = value;
            UpdateTime = DateTime.Now;
        }

        /// <summary>
        /// 获取任务值的DateTime表示
        /// </summary>
        /// <param name="defaultValue">默认值，如果转换失败</param>
        /// <returns>DateTime表示的任务值</returns>
        public DateTime ToDateTime(DateTime defaultValue)
        {
            if (string.IsNullOrEmpty(Value))
                return defaultValue;

            if (DateTime.TryParse(Value, out DateTime result))
                return result;

            return defaultValue;
        }

        /// <summary>
        /// 获取任务值的Int32表示
        /// </summary>
        /// <returns>Int32表示的任务值</returns>
        public int ToInt32()
        {
            if (string.IsNullOrEmpty(Value))
                return 0;

            if (int.TryParse(Value, out int result))
                return result;

            return 0;
        }

        /// <summary>
        /// 获取任务值的数组表示，使用|分隔
        /// </summary>
        /// <param name="split">分隔符</param>
        /// <returns>字符串数组</returns>
        public string[] GetArray(char split = '|')
        {
            if (string.IsNullOrEmpty(Value))
                return new string[0];

            return Value.Split(split, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// 检查任务值是否为空
        /// </summary>
        /// <returns>如果任务值不为空或null，则返回true</returns>
        public bool IsNotEmptyOrNull()
        {
            return !string.IsNullOrEmpty(Value);
        }*/
    }
}