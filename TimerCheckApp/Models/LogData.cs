using System.Collections.ObjectModel;

namespace TimerCheckApp.Models;

/// <summary> 
/// 日志数据实体类，用于存储日志相关属性 
/// </summary> 
public class LogData
{
    
    private bool _isWebSocket;
    
    /// <summary> 
    /// 是否为WebSocket日志 
    /// </summary> 
    public bool IsWebSocket
    {
        get { return _isWebSocket; }
        set { _isWebSocket = value; }
    }

    private ObservableCollection<LogEntry> _items = new ObservableCollection<LogEntry>();
    
    /// <summary> 
    /// 日志数据列表 
    /// </summary> 
    public ObservableCollection<LogEntry> Items
    {
        get { return _items; }
        set { _items = value; }
    }

    private bool _showAllLogs = true;
    
    /// <summary> 
    /// 是否显示全部日志 
    /// </summary> 
    public bool ShowAllLogs
    {
        get { return _showAllLogs; }
        set { _showAllLogs = value; }
    }

    /// <summary> 
    /// 日志数量 
    /// </summary> 
    public int LogCount => Items?.Count ?? 0;

    /// <summary> 
    /// 默认构造函数 
    /// </summary> 
    public LogData()
    {
        // 初始化Items集合
    }

    /// <summary> 
    /// 带参数的构造函数 
    /// </summary> 
    /// <param name="isWebSocket">是否为WebSocket日志</param> 
    /// <param name="showAllLogs">是否显示全部日志</param> 
    public LogData(bool isWebSocket, bool showAllLogs) : this()
    {
        IsWebSocket = isWebSocket;
        ShowAllLogs = showAllLogs;
    }
}