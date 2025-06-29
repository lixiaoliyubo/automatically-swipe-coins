using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdAuto.Extensions.enums
{
    public enum AppPageEnum
    {
        Node = 0,
        [Description("首页")]
        Home = 1,
        [Description("金币首页")]
        MoneyHome = 2,
        [Description("逛街得金币")]
        GuangJie = 3,
        [Description("看指定视频得金币")]
        AppointVideo = 4,
        [Description("开宝箱得金币")]
        OpenTreasure = 5,
        [Description("看广告视频再赚")]
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
        OneClickMoney = 12



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
    }
}
