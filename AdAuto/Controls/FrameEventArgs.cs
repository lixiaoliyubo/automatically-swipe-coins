using AdAuto.Extensions.Common;
using System;

namespace AdAuto.Controls
{
    /// <summary>
    /// 帧更新事件的参数类
    /// </summary>
    public class FrameEventArgs : EventArgs
    {
        /// <summary>
        /// 帧数据
        /// </summary>
        public FrameData FrameData { get; }

        /// <summary>
        /// 初始化帧更新事件参数
        /// </summary>
        /// <param name="frameData">帧数据</param>
        public FrameEventArgs(FrameData frameData)
        {
            FrameData = frameData;
        }
    }
}