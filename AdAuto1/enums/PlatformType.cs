using System.ComponentModel;

namespace AdAuto.enums
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
        FanQie = 3,
        /// <summary>
        /// 乐活
        /// </summary>
        [Description("乐活")]
        LeHuo = 4,
        /// <summary>
        /// 今日头条
        /// </summary>
        [Description("今日头条")]
        TouTiao = 5,
        /// <summary>
        /// 红果短剧
        /// </summary>
        [Description("红果短剧")]
        HongGuo = 6,
        /// <summary>
        /// 河马剧场
        /// </summary>
        [Description("河马剧场")]
        HeMa = 7,
        /// <summary>
        /// 番茄小说
        /// </summary>
        [Description("番茄小说")]
        Novel = 8,
    }
}