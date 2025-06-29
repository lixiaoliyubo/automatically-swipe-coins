using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TimerCheckApp.enums;

namespace TimerCheckApp.Models
{
    /// <summary>
    /// 表示Android设备的模型类
    /// </summary>
    public class DeviceModel : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private string _status;
        private bool _isRunning;
        private DateTime _startTime;
        private DateTime _lastCheckTime;
        private DateTime _lastClickTime;
        private string _currentApp;
        private string _lastOcrResult;
        private string _packageName;
        private ObservableCollection<LogEntry> _logs;
        private int _width;
        private int _height;

        /// <summary>
        /// 当前页面类型
        /// </summary>
        private AppPageEnum _curPage;
        public AppPageEnum CurPage
        {
            get => _curPage;
            set => SetProperty(ref _curPage, value);
        }

        /// <summary>
        /// 获取或设置当前选择的平台类型
        /// </summary>
        private PlatformType _curPlatform = PlatformType.DouyinJs;

        /// <summary>
        /// 获取或设置当前选择的平台类型
        /// </summary>
        public PlatformType CurPlatform
        {
            get => _curPlatform;
            set => SetProperty(ref _curPlatform, value);
        }


        /// <summary>
        /// 设备ID
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 设备名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 设备状态
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 是否正在运行检查任务
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }

        /// <summary>
        /// 最后检查时间
        /// </summary>
        public DateTime LastCheckTime
        {
            get => _lastCheckTime;
            set => SetProperty(ref _lastCheckTime, value);
        }
        /// <summary>
        /// 开始运行时间
        /// </summary>
        public DateTime StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        /// <summary>
        /// 最后点击时间
        /// </summary>
        public DateTime LastClickTime
        {
            get => _lastClickTime;
            set => SetProperty(ref _lastClickTime, value);
        }

        /// <summary>
        /// 当前运行的应用
        /// </summary>
        public string CurrentApp
        {
            get => _currentApp;
            set => SetProperty(ref _currentApp, value);
        }

        /// <summary>
        /// 最后一次OCR识别结果
        /// </summary>
        public string LastOcrResult
        {
            get => _lastOcrResult;
            set => SetProperty(ref _lastOcrResult, value);
        }
        /// <summary>
        /// 应用包名
        /// </summary>
        public string PackageName
        {
            get => _packageName;
            set => SetProperty(ref _packageName, value);
        }

        /// <summary>
        /// 设备日志集合
        /// </summary>
        public ObservableCollection<LogEntry> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        public string ScreenshotPath { get; set; }

        /// <summary>
        /// 设备屏幕宽度（像素）
        /// </summary>
        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// 设备屏幕高度（像素）
        /// </summary>
        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }


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

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 设置属性值并触发属性变更通知
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="storage">属性存储字段</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名称</param>
        /// <returns>是否发生变更</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}