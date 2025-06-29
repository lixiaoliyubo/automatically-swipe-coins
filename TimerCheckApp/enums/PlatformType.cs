using System;
using System.ComponentModel;

namespace TimerCheckApp.enums
{
    /// <summary>
    /// 用于标注App的包名称
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AppPackageAttribute : Attribute
    {
        /// <summary>
        /// App的包名称
        /// </summary>
        public string PackageName { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="packageName">App的包名称</param>
        public AppPackageAttribute(string packageName)
        {
            PackageName = packageName;
        }
    }
    /// <summary>
    /// 平台类型枚举
    /// </summary>
    public enum PlatformType
    {
        /// <summary>
        /// 抖音
        /// </summary>
        [Description("抖音极速版")]
        [AppPackage("com.ss.android.ugc.aweme.lite")]
        Douyin = 0,
        /// <summary>
        /// 快手
        /// </summary>
        [Description("快手")]
        [AppPackage("com.smile.gifmaker")]
        Kuaishou = 1,
        /// <summary>
        /// 快手极速版
        /// </summary>
        [Description("快手极速版")]
        [AppPackage("com.kuaishou.nebula")]
        KuaishouJs = 2,
        /// <summary>
        /// 西瓜
        /// </summary>
        [Description("西瓜极速版")]
        [AppPackage("com.ss.android.article.lite")]
        XiGua = 3,
        /// <summary>
        /// 番茄畅听
        /// </summary>
        [Description("番茄畅听")]
        [AppPackage("com.xs.fm")]
        FanQie = 4,
        /// <summary>
        /// 番茄畅听音乐版
        /// </summary>
        [Description("番茄畅听音乐版")]
        [AppPackage("com.xs.fm.lite")]
        FanQieMusic = 5,
        /// <summary>
        /// 红果短剧
        /// </summary>
        [Description("红果短剧")]
        [AppPackage("com.phoenix.read")]
        HongGuo = 6,
        /// <summary>
        /// 河马剧场
        /// </summary>
        [Description("河马剧场")]
        [AppPackage("com.dz.hmjc")]
        HeMa = 7,
        /// <summary>
        /// 番茄小说
        /// </summary>
        [Description("番茄小说")]
        [AppPackage("com.dragon.read")]
        Novel = 8,
        /// <summary>
        /// 数据准备
        /// </summary>
        [Description("数据准备")]
        [AppPackage("")]
        Data = 9,
        /// <summary>
        /// 支付宝
        /// </summary>
        [Description("支付宝")]
        [AppPackage("com.eg.android.AlipayGphone")]
        AliPay = 10,
    }
}