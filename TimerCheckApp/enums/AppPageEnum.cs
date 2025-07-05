using System.ComponentModel;

namespace TimerCheckApp.enums
{
    public enum AppPageEnum
    {
        Node = 0,
        [Description("滑动看短剧")]
        SlideUp = 1,
        [Description("金币首页")]
        MoneyHome = 2,
        [Description("逛街得金币")]
        GuangJie = 3,
        [Description("看指定视频得金币")]
        AppointVideo = 4,
        [Description("开宝箱得金币")]
        OpenTreasure = 5,
        [Description("看广告")]
        Ad = 6,
        [Description("看广告赚金币")]
        ViewAdMoney = 7,
        [Description("吃饭打卡领金币")]
        Dining = 8,
        [Description("天天领金币")]
        DayMoney = 9,
        [Description("每天打卡拿奖励")]
        DayClockIn = 10,
        [Description("每天睡觉起床")]
        DaySawu = 11,
        [Description("一键领金币")]
        OneClickMoney = 12,
        [Description("早安小岛")]
        ZaoAnXiaoDao = 13,
        [Description("抖音快手快手极速版")]
        DyAndKsAddKsJs = 14,
        [Description("金币购赚金币")]
        JbGou = 15,
        [Description("刷广告视频赚金币")]
        ReLoadAd = 18,
        [Description("看视频赚金币")]
        ViewVideo = 31,
        [Description("看直播赚金币")]
        ViewLive = 30,

        /// <summary>
        /// 搜索赚金币
        /// </summary>
        [Description("搜索赚金币")]
        SearchKey = 32,

        /// <summary>
        /// 阅读赚金币
        /// </summary>
        [Description("阅读赚金币")]
        ReadLoad = 33,
        [Description("番茄小说")]
        KanXiaoShuo = 34,
        [Description("支付宝刷视频")]
        AliPayVideo = 35,

        #region 快手

        /// <summary>
        /// 打开快影必得金币
        /// </summary>
        [Description("打开快影必得金币")]
        KSOpenKuaiYing = 41,

        /// <summary>
        /// 走路赚金币
        /// </summary>
        [Description("走路赚金币")]
        KSWalkEarn = 42,

        /// <summary>
        /// 逛快手商城赚金币
        /// </summary>
        [Description("逛快手商城赚金币")]
        KSShopMall = 44,

        /// <summary>
        /// 去逛618会场
        /// </summary>
        [Description("去逛618会场")]
        KS618Shopping = 45,

        /// <summary>
        /// 数据准备
        /// </summary>
        [Description("数据准备")]
        Data = 48,
        #endregion
    }

}
