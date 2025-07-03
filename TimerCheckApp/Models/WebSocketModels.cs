using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TimerCheckApp.Models;

/// <summary>
/// 指针事件类型枚举
/// </summary>
public enum PointerEventType
{
    /// <summary>
    /// 按下事件
    /// </summary>
    Down = 0,
    
    /// <summary>
    /// 移动事件
    /// </summary>
    Move = 1,
    
    /// <summary>
    /// 松开事件
    /// </summary>
    Up = 2,
    
    /// <summary>
    /// 鼠标右键(返回)
    /// </summary>
    RightClick = 3,
    
    /// <summary>
    /// 滚轮向上
    /// </summary>
    WheelUp = 4,
    
    /// <summary>
    /// 滚轮向下
    /// </summary>
    WheelDown = 5,
    
    /// <summary>
    /// 上滑
    /// </summary>
    SwipeUp = 6,
    
    /// <summary>
    /// 下滑
    /// </summary>
    SwipeDown = 7,
    
    /// <summary>
    /// 左滑
    /// </summary>
    SwipeLeft = 8,
    
    /// <summary>
    /// 右滑
    /// </summary>
    SwipeRight = 9
}

/// <summary>
/// WebSocket响应基类
/// </summary>
public class WebSocketResponse
{
    /*
     {"StatusCode":200,"result":"[{\"deviceId\":\"KWFRX19412002479\",\"name\":\"KWFRX19412002479\",\"no\":2},{\"deviceId\":\"OB8HDMK7R8EQEE95\",\"name\":\"OB8HDMK7R8EQEE95\",\"no\":3},{\"deviceId\":\"f41f8502\",\"name\":\"f41f8502\",\"no\":1}]"}
     */
    /// <summary>
    /// 响应状态码
    /// </summary>
    [JsonPropertyName("StatusCode")]
    public int Code { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    [JsonPropertyName("result")]
    public string result { get; set; }
}


/// <summary>
/// 设备信息
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// 设备ID
    /// </summary>
    [JsonPropertyName("deviceId")]
    public string Id { get; set; }

    /// <summary>
    /// 设备名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }
    /// <summary>
    /// 设备名称
    /// </summary>
    [JsonPropertyName("no")]
    public int No { get; set; }
}