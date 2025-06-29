using System.ComponentModel;

namespace AdAuto.Extensions.enums
{
    /// <summary>
    /// 平台类型枚举
    /// </summary>
    public enum PlatformType
    {
        /// <summary>
        /// 抖音
        /// </summary>
        [Description("抖音极速版")]
        Douyin = 0,

        /// <summary>
        /// 快手
        /// </summary>
        [Description("快手极速版")]
        Kuaishou = 1,

        /// <summary>
        /// 西瓜
        /// </summary>
        [Description("西瓜极速版")]
        XiGua = 2,
        /// <summary>
        /// 番茄畅听
        /// </summary>
        [Description("番茄畅听")]
        FanQie = 3
    }
}