using System;
using System.Buffers.Binary;
using AdAuto.Extensions.enums;

/// <summary>
/// 控制消息相关的类型定义和实现
/// 包含了用于控制Android设备的各种消息类型，如按键注入、触摸事件、滚动事件等
/// </summary>
namespace AdAuto.Extensions.Common
{
    /// <summary>
    /// 控制消息类型枚举
    /// 定义了所有支持的控制消息类型
    /// </summary>
    public enum ControlMessageType : byte
    {
        InjectKeycode,
        InjectText,
        InjectTouchEvent,
        InjectScrollEvent,
        BackOrScreenOn,
        ExpandNotificationPanel,
        ExpandSettingsPanel,
        CollapsePanels,
        GetClipboard,
        SetClipboard,
        SetScreenPowerMode,
        RotateDevice,
    }

    /// <summary>
    /// 屏幕尺寸记录类型
    /// 用于存储屏幕的宽度和高度信息
    /// </summary>
    public record ScreenSize
    {
        public ushort Width;
        public ushort Height;
    }

    /// <summary>
    /// 坐标点记录类型
    /// 用于存储X和Y坐标值
    /// </summary>
    public record Point
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// 位置记录类型
    /// 包含屏幕尺寸和坐标点信息
    /// </summary>
    public record Position
    {
        public ScreenSize ScreenSize = new();
        public Point Point = new();

        public Span<byte> ToBytes()
        {
            Span<byte> b = new byte[12];
            BinaryPrimitives.WriteInt32BigEndian(b[0..], Point.X);
            BinaryPrimitives.WriteInt32BigEndian(b[4..], Point.Y);
            BinaryPrimitives.WriteUInt16BigEndian(b[8..], ScreenSize.Width);
            BinaryPrimitives.WriteUInt16BigEndian(b[10..], ScreenSize.Height);
            return b;
        }
    }

    /// <summary>
    /// 控制消息接口
    /// 定义了所有控制消息必须实现的属性和方法
    /// </summary>
    public interface IControlMessage
    {
        public ControlMessageType Type { get; }

        Span<byte> ToBytes();
    }

    /// <summary>
    /// 按键控制消息类
    /// 用于发送按键事件到Android设备
    /// </summary>
    public class KeycodeControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.InjectKeycode;
        public AndroidKeyEventAction Action { get; set; }
        public AndroidKeycode KeyCode { get; set; }
        public uint Repeat { get; set; }
        public AndroidMetastate Metastate { get; set; }

        public Span<byte> ToBytes()
        {
            Span<byte> b = new byte[14];
            b[0] = (byte)Type;
            b[1] = (byte)Action;
            BinaryPrimitives.WriteInt32BigEndian(b[2..], (int)KeyCode);
            BinaryPrimitives.WriteInt32BigEndian(b[6..], (int)Repeat);
            BinaryPrimitives.WriteInt32BigEndian(b[10..], (int)Metastate);
            return b;
        }
    }

    /// <summary>
    /// 返回键或屏幕唤醒控制消息类
    /// 用于发送返回键事件或唤醒屏幕
    /// </summary>
    public class BackOrScreenOnControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.BackOrScreenOn;
        public AndroidKeyEventAction Action { get; set; }

        public Span<byte> ToBytes()
        {
            Span<byte> b = new byte[2];
            b[0] = (byte)Type;
            b[1] = (byte)Action;
            return b;
        }
    }

    /// <summary>
    /// 触摸事件控制消息类
    /// 用于发送触摸事件到Android设备
    /// </summary>
    public class TouchEventControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.InjectTouchEvent;
        public AndroidMotionEventAction Action { get; set; }
        public AndroidMotionEventButtons Buttons { get; set; } = AndroidMotionEventButtons.AMOTION_EVENT_BUTTON_PRIMARY;
        public ulong PointerId { get; set; } = 0xFFFFFFFFFFFFFFFF;
        public Position Position { get; set; } = new();
        //public float Pressure { get; set; }

        public Span<byte> ToBytes()
        {
            Span<byte> b = new byte[28];
            b[0] = (byte)Type;
            b[1] = (byte)Action;
            BinaryPrimitives.WriteUInt64BigEndian(b[2..], PointerId);

            // Position
            BinaryPrimitives.WriteInt32BigEndian(b[10..], Position.Point.X);
            BinaryPrimitives.WriteInt32BigEndian(b[14..], Position.Point.Y);
            BinaryPrimitives.WriteUInt16BigEndian(b[18..], Position.ScreenSize.Width);
            BinaryPrimitives.WriteUInt16BigEndian(b[20..], Position.ScreenSize.Height);

            // TODO: Pressure
            b[22] = 0xFF;
            b[23] = 0xFF;

            BinaryPrimitives.WriteInt32BigEndian(b[24..], (int)Buttons);

            return b;
        }
    }

    /// <summary>
    /// 滚动事件控制消息类
    /// 用于发送滚动事件到Android设备
    /// </summary>
    public class ScrollEventControlMessage : IControlMessage
    {
        public ControlMessageType Type => ControlMessageType.InjectScrollEvent;
        public Position Position { get; set; } = new();
        public int HorizontalScroll { get; set; }
        public int VerticalScroll { get; set; }

        public Span<byte> ToBytes()
        {
            Span<byte> b = new byte[21];
            b[0] = (byte)Type;
            Position.ToBytes().CopyTo(b[1..]);
            BinaryPrimitives.WriteInt32BigEndian(b[13..], HorizontalScroll);
            BinaryPrimitives.WriteInt32BigEndian(b[17..], VerticalScroll);
            return b;
        }
    }
}
