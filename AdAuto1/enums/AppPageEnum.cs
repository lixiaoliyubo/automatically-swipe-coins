using System.ComponentModel;

namespace AdAuto.enums
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
        [Description("逛商城赚金币")]
        GuangSC = 10,
        [Description("看直播开宝箱")]
        ViewLiveRoom = 11,
        [Description("一键领金币")]
        OneClickMoney = 12,
        [Description("早安小岛")]
        ZaoAnXiaoDao = 13,
        [Description("听书赚金币")]
        ListenBooks = 14,
        [Description("去番茄畅听听书*S秒")]
        FqTingShu = 15,
        [Description("看热点3分钟赚钱")]
        ViewRenDian = 17,
        [Description("刷广告视频赚金币")]
        ReLoadAd = 18,

        #region 乐活
        /// <summary>
        /// 喝水打卡领金币活动，对应数值 3
        /// </summary>
        [Description("喝水打卡领金币")]
        DrinkWaterPunchIn = 19,

        /// <summary>
        /// 每日三餐补贴活动，对应数值 4
        /// </summary>
        [Description("每日三餐补贴")]
        ThreeMealsSubsidy = 20,

        /// <summary>
        /// 任务赚活动，对应数值 5
        /// </summary>
        [Description("任务赚")]
        TaskEarning = 21,

        /// <summary>
        /// 百度智慧回答活动，对应数值 6
        /// </summary>
        [Description("百度智慧回答")]
        BaiduSmartAnswer = 22,

        /// <summary>
        /// 搜索领金币活动，对应数值 7
        /// </summary>
        [Description("搜索领金币")]
        SearchForCoins = 23,

        /// <summary>
        /// 看一个文章或视频活动，对应数值 8
        /// </summary>
        [Description("看一个文章或视频")]
        ReadArticleOrWatchVideo = 24,

        /// <summary>
        /// 逛商城赚金币活动，对应数值 9
        /// </summary>
        [Description("逛商城赚金币")]
        ShopMallEarning = 25,

        /// <summary>
        /// 每日签到领现金活动，对应数值 10
        /// </summary>
        [Description("每日签到领现金")]
        DailySignInForCash = 26,

        /// <summary>
        /// 看看广告领金币奖励活动，对应数值 11
        /// </summary>
        [Description("看广告领金币奖励")]
        WatchAdsForCoins = 27,
        /// <summary>
        /// 看视频文章领金币
        /// </summary>
        [Description("看视频文章领金币")]
        ReadVideoOrArticle = 28,
        #endregion

        #region 抖音
        /// <summary>
        /// 去今日头条极速版看视频活动
        /// </summary>
        [Description("去今日头条极速版看视频")]
        ToutiaoVideo = 29,

        /// <summary>
        /// 去番茄小说看书活动
        /// </summary>
        [Description("去番茄小说看书")]
        FanqieBook = 30,

        /// <summary>
        /// 看短剧赚更多金币活动
        /// </summary>
        [Description("看短剧赚更多金币")]
        ShortDramaCoins = 31,
        #endregion

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
        //[Description("天天预约领金币")]
        //Subscribe = 6,
        //[Description("直播间")]
        //LiveRoom = 7,
        //[Description("钱包")]
        //Balance = 8,
        //[Description("一键领音符")]
        //OneClicklead = 11,
        //[Description("一键领金币")]
        //OneClickMoney = 12,
        //[Description("限时任务赚音符")]
        //TimeLmitedTask = 13


        [Description("抖音看番茄小说")]
        KanXiaoShuo = 34
    }

}
