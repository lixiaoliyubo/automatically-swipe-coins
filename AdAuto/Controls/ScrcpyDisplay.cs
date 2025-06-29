using AdAuto.Extensions.enums;
using AdAuto.Extensions;
using AdAuto.Extensions.Helper;
using Yuandl.ThemeUI.Controls;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Yuandl.Common.LogClass;
using System.Drawing;
using System.IO;
using System;
using AdAuto.Extensions.Common;
namespace AdAuto.Controls
{
    /// <summary>
    /// ScrcpyDisplay控件，用于显示和控制Android设备屏幕
    /// 提供了触摸、键盘输入等交互功能
    /// 
    /// 使用方法：
    /// 1. 在XAML中引用命名空间：
    ///    xmlns:MyNamespace="clr-namespace:ScrcpyNet.Wpf"
    /// 
    /// 2. 使用控件：
    ///    <MyNamespace:ScrcpyDisplay/>
    /// </summary>
    public class ScrcpyDisplay : Control
    {
        private static readonly ILogger log = LogFactory.CreateLogger<ScrcpyDisplay>();
        private static DateTime lastSaveTime = DateTime.MinValue;

        /// <summary>
        /// 帧更新事件，当OnFrame方法更新时触发
        /// </summary>
        public static readonly RoutedEvent FrameUpdatedEvent = EventManager.RegisterRoutedEvent(
            nameof(FrameUpdated),
            RoutingStrategy.Bubble,
            typeof(TypedEventHandler<ScrcpyDisplay, RoutedEventArgs>),
            typeof(ScrcpyDisplay));

        /// <summary>
        /// 帧更新事件，当OnFrame方法更新时触发
        /// </summary>
        public event TypedEventHandler<ScrcpyDisplay, RoutedEventArgs> FrameUpdated
        {
            add { AddHandler(FrameUpdatedEvent, value); }
            remove { RemoveHandler(FrameUpdatedEvent, value); }
        }

        /// <summary>
        /// Scrcpy依赖属性，用于绑定Scrcpy实例
        /// </summary>
        public static readonly DependencyProperty ScrcpyProperty = DependencyProperty.Register(
            nameof(Scrcpy),
            typeof(Scrcpy),
            typeof(ScrcpyDisplay),
            new PropertyMetadata(OnScrcpyChanged));

        /// <summary>
        /// 用于显示Android设备屏幕的Image控件
        /// </summary>
        private System.Windows.Controls.Image? renderTarget;

        /// <summary>
        /// 用于渲染视频帧的位图
        /// </summary>
        private WriteableBitmap? bmp;

        // 用于重用的内存流和缓冲区
        private readonly MemoryStream reusableStream = new MemoryStream(1024 * 1024); // 预分配1MB
        private byte[] imageBuffer = new byte[1024 * 1024]; // 预分配1MB

        /// <summary>
        /// 静态构造函数，重写默认样式元数据
        /// </summary>
        static ScrcpyDisplay()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ScrcpyDisplay), new FrameworkPropertyMetadata(typeof(ScrcpyDisplay)));
        }

        /// <summary>
        /// 获取或设置Scrcpy实例
        /// </summary>
        public Scrcpy? Scrcpy
        {
            get => (Scrcpy)GetValue(ScrcpyProperty);
            set => SetValue(ScrcpyProperty, value);
        }

        /// <summary>
        /// 应用控件模板时初始化渲染目标
        /// </summary>
        /// <remarks>
        /// 在控件模板应用时，查找并初始化名为"PART_RenderTargetImage"的Image控件作为渲染目标
        /// </remarks>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("PART_RenderTargetImage") is System.Windows.Controls.Image img)
                renderTarget = img;
        }

        /// <summary>
        /// 处理鼠标按下事件
        /// </summary>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 1. 右键按下时发送返回命令
        /// 2. 左键按下时发送触摸按下命令
        /// 3. 确保控件获得焦点以接收键盘输入
        /// </remarks>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            Focus();

            if (Scrcpy != null)
            {
                if (e.RightButton == MouseButtonState.Pressed)
                {
                    e.Handled = true;
                    Scrcpy.SendControlCommand(new BackOrScreenOnControlMessage() { Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_DOWN });
                    Scrcpy.SendControlCommand(new BackOrScreenOnControlMessage() { Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP });
                }
                else if (e.LeftButton == MouseButtonState.Pressed)
                {
                    e.Handled = true;
                    SendTouchCommand(AndroidMotionEventAction.AMOTION_EVENT_ACTION_DOWN, e);
                }
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// 处理鼠标释放事件
        /// </summary>
        /// <param name="e">鼠标按钮事件参数</param>
        /// <remarks>
        /// 当鼠标按钮释放时，发送触摸释放命令到Android设备
        /// </remarks>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (Scrcpy != null)
            {
                e.Handled = true;
                SendTouchCommand(AndroidMotionEventAction.AMOTION_EVENT_ACTION_UP, e);
            }

            base.OnMouseUp(e);
        }

        /// <summary>
        /// 处理鼠标移动事件
        /// </summary>
        /// <param name="e">鼠标事件参数</param>
        /// <remarks>
        /// 当左键按下并移动时，发送触摸移动命令到Android设备
        /// 确保鼠标位置在渲染目标范围内
        /// </remarks>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (Scrcpy != null && renderTarget != null)
            {
                var point = e.GetPosition(renderTarget);

                if (e.LeftButton == MouseButtonState.Pressed && point.X >= 0 && point.Y >= 0)
                {
                    SendTouchCommand(AndroidMotionEventAction.AMOTION_EVENT_ACTION_MOVE, e);
                }
            }

            base.OnMouseMove(e);
        }

        /// <summary>
        /// 处理键盘按下事件
        /// </summary>
        /// <param name="e">键盘事件参数</param>
        /// <remarks>
        /// 将WPF键盘事件转换为Android按键事件并发送到设备
        /// 包括按键代码和元状态（如Shift、Ctrl等修饰键状态）
        /// </remarks>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Scrcpy != null)
            {
                e.Handled = true;

                var msg = new KeycodeControlMessage();
                msg.KeyCode = KeycodeHelper.ConvertKey(e.Key);
                msg.Metastate = KeycodeHelper.ConvertModifiers(e.KeyboardDevice.Modifiers);
                Scrcpy.SendControlCommand(msg);
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// 处理键盘释放事件
        /// </summary>
        /// <param name="e">键盘事件参数</param>
        /// <remarks>
        /// 发送按键释放事件到Android设备
        /// 保持与按下事件相同的按键代码和元状态
        /// </remarks>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (Scrcpy != null)
            {
                e.Handled = true;

                var msg = new KeycodeControlMessage();
                msg.Action = AndroidKeyEventAction.AKEY_EVENT_ACTION_UP;
                msg.KeyCode = KeycodeHelper.ConvertKey(e.Key);
                msg.Metastate = KeycodeHelper.ConvertModifiers(e.KeyboardDevice.Modifiers);
                Scrcpy.SendControlCommand(msg);
            }

            base.OnKeyUp(e);
        }

        /// <summary>
        /// 处理鼠标滚轮事件
        /// </summary>
        /// <param name="e">鼠标滚轮事件参数</param>
        /// <remarks>
        /// 将鼠标滚轮事件转换为Android滚动事件
        /// 支持垂直滚动，水平滚动待实现
        /// </remarks>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            var pos = GetScrcpyMousePosition(e);
            if (Scrcpy != null && pos != null)
            {
                e.Handled = true;

                var msg = new ScrollEventControlMessage();
                msg.Position = pos;
                msg.VerticalScroll = e.Delta / 120; // 将WPF滚轮增量转换为Android滚动值
                msg.HorizontalScroll = 0; // TODO: 实现水平滚动
                Scrcpy.SendControlCommand(msg);
            }

            base.OnMouseWheel(e);
        }

        /// <summary>
        /// 发送触摸事件命令到Android设备
        /// </summary>
        /// <param name="action">触摸动作类型</param>
        /// <param name="e">鼠标事件参数</param>
        /// <remarks>
        /// 将鼠标事件转换为Android触摸事件
        /// 包括触摸位置的坐标转换
        /// </remarks>
        protected void SendTouchCommand(AndroidMotionEventAction action, MouseEventArgs e)
        {
            var pos = GetScrcpyMousePosition(e);
            if (Scrcpy != null && pos != null)
            {
                var msg = new TouchEventControlMessage();
                msg.Action = action;
                msg.Position = pos;
                Scrcpy.SendControlCommand(msg);

                log.LogDebug("Sending {Action} for position {PositionX}, {PositionY}", action, msg.Position.Point.X, msg.Position.Point.Y);
            }
        }

        /// <summary>
        /// 获取鼠标位置并转换为Android设备坐标
        /// </summary>
        /// <param name="e">鼠标事件参数</param>
        /// <returns>转换后的位置信息，如果转换失败则返回null</returns>
        /// <remarks>
        /// 将WPF坐标系中的鼠标位置转换为Android设备坐标系
        /// 考虑屏幕尺寸和缩放比例
        /// </remarks>
        private Position? GetScrcpyMousePosition(MouseEventArgs e)
        {
            if (Scrcpy == null || renderTarget == null) return null;

            var point = e.GetPosition(renderTarget);

            var pos = new Position();
            pos.Point = new Extensions.Common.Point { X = (int)point.X, Y = (int)point.Y };
            pos.ScreenSize.Width = (ushort)renderTarget.ActualWidth;
            pos.ScreenSize.Height = (ushort)renderTarget.ActualHeight;
            TouchHelper.ScaleToScreenSize(pos, Scrcpy.Width, Scrcpy.Height);

            return pos;
        }

        /// <summary>
        /// 处理视频帧更新
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="frameData">帧数据</param>
        /// <remarks>
        /// 1. 在UI线程上更新位图
        /// 2. 根据需要创建或重新创建位图
        /// 3. 将帧数据复制到位图缓冲区
        /// 4. 处理超时和任务取消异常
        /// </remarks>
        private unsafe void OnFrame(object? sender, FrameData frameData)
        {
            if (renderTarget != null)
            {
                try
                {   
                    Dispatcher.Invoke(() =>
                    {
                        if (bmp == null || bmp.Width != frameData.Width || bmp.Height != frameData.Height)
                        {
                            bmp = new WriteableBitmap(frameData.Width, frameData.Height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null);
                            renderTarget.Source = bmp;
                        }

                        try
                        {
                            bmp.Lock();
                            var dest = new Span<byte>(bmp.BackBuffer.ToPointer(), frameData.Data.Length);
                            frameData.Data.CopyTo(dest);
                            bmp.AddDirtyRect(new Int32Rect(0, 0, frameData.Width, frameData.Height));

                            // 检查是否需要更新帧
                            if ((DateTime.Now - lastSaveTime).TotalSeconds >= 2)
                            {
                                lastSaveTime = DateTime.Now;
                                // 重用Bitmap对象并直接使用已分配的内存流
                                var bitmap = new Bitmap(
                                    bmp.PixelWidth,
                                    bmp.PixelHeight,
                                    bmp.BackBufferStride,
                                    System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                                    bmp.BackBuffer);
                                
                                try
                                {
                                    reusableStream.Position = 0;
                                    reusableStream.SetLength(0);
                                    bitmap.Save(reusableStream, System.Drawing.Imaging.ImageFormat.Bmp);
                                    
                                    int bytesRead = (int)reusableStream.Position;
                                    if (bytesRead > imageBuffer.Length)
                                    {
                                        Array.Resize(ref imageBuffer, bytesRead);
                                    }
                                    
                                    reusableStream.Position = 0;
                                    reusableStream.Read(imageBuffer, 0, bytesRead);
                                    RaiseEvent(new RoutedEventArgs(FrameUpdatedEvent, imageBuffer.AsMemory(0, bytesRead).ToArray()));
                                }
                                finally
                                {
                                    bitmap.Dispose();
                                }
                            }
                        }
                        finally
                        {
                            bmp.Unlock();
                        }
                    }, DispatcherPriority.Send, default, TimeSpan.FromMilliseconds(500));


                }
                catch (TimeoutException)
                {
                    log.LogDebug("Ignoring TimeoutException inside OnFrame.");
                }
                catch (TaskCanceledException)
                {
                    log.LogDebug("Ignoring TaskCanceledException inside OnFrame.");
                }
            }
        }

        /// <summary>
        /// Scrcpy属性变更处理
        /// </summary>
        /// <param name="sender">依赖属性所有者</param>
        /// <param name="e">依赖属性变更事件参数</param>
        /// <remarks>
        /// 1. 取消订阅旧的Scrcpy实例的视频帧更新事件
        /// 2. 订阅新的Scrcpy实例的视频帧更新事件
        /// </remarks>
        private static void OnScrcpyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is ScrcpyDisplay display)
            {
                // 取消订阅旧的Scrcpy实例事件
                if (e.OldValue is Scrcpy old && old != null)
                    old.VideoStreamDecoder.OnFrame -= display.OnFrame;

                // 订阅新的Scrcpy实例事件
                if (e.NewValue is Scrcpy value && value != null)
                    value.VideoStreamDecoder.OnFrame += display.OnFrame;
            }
        }
    }
}