using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TimerCheckApp.enums;

namespace TimerCheckApp.Models
{
    /// <summary>
    /// 表示Android设备的模型类
    /// </summary>
    public partial class DeviceModel : ObservableObject
    {
        /// <summary>
        /// 当前页面类型
        /// </summary>
        [ObservableProperty]
        private AppPageEnum _curPage;

        /// <summary>
        /// 当前子级页面类型
        /// </summary>
        [ObservableProperty]
        private AppPageEnum _curChildPage;

        /// <summary>
        /// 获取或设置当前选择的平台类型
        /// </summary>
        [ObservableProperty]
        private PlatformType _curPlatform = PlatformType.DouyinJs;

        /// <summary>
        /// 设备ID
        /// </summary>
        [ObservableProperty]
        private string _id;

        /// <summary>
        /// 设备名称
        /// </summary>
        [ObservableProperty]
        private string _name;

        /// <summary>
        /// 设备状态
        /// </summary>
        [ObservableProperty]
        private string _status;

        /// <summary>
        /// 是否正在运行检查任务
        /// </summary>
        [ObservableProperty]
        private bool _isRunning;

        /// <summary>
        /// 最后检查时间
        /// </summary>
        [ObservableProperty]
        private DateTime _lastCheckTime;

        /// <summary>
        /// 开始运行时间
        /// </summary>
        [ObservableProperty]
        private DateTime _startTime;

        /// <summary>
        /// 最后点击时间
        /// </summary>
        [ObservableProperty]
        private DateTime _lastClickTime;

        /// <summary>
        /// 当前运行的应用
        /// </summary>
        [ObservableProperty]
        private string _currentApp;

        /// <summary>
        /// 最后一次OCR识别结果
        /// </summary>
        [ObservableProperty]
        private string _lastOcrResult;

        /// <summary>
        /// 应用包名
        /// </summary>
        [ObservableProperty]
        private string _packageName;

        /// <summary>
        /// 设备日志集合
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<LogEntry> _logs;

        /// <summary>
        /// 设备截图路径
        /// </summary>
        public string ScreenshotPath { get; set; }

        /// <summary>
        /// 设备屏幕宽度（像素）
        /// </summary>
        [ObservableProperty]
        private int _width;

        /// <summary>
        /// 设备屏幕高度（像素）
        /// </summary>
        [ObservableProperty]
        private int _height;


        /// <summary>
        /// 构造函数
        /// </summary>
        public DeviceModel()
        {
            _logs = new ObservableCollection<LogEntry>();
            _status = "离线";
            _isRunning = false;
            _lastCheckTime = DateTime.Now;
            _width = 0;
            _height = 0;
        }


    }
}