/// <summary>
/// Scrcpy配置选项命名空间
/// </summary>
namespace AdAuto.Extensions.enums
{
    /// <summary>
    /// 视频方向锁定选项
    /// 定义了视频显示的方向锁定模式
    /// 值来源于Genymobile/scrcpy项目的options.h文件
    /// </summary>
    public enum ScrcpyLockVideoOrientation
    {
        /// <summary>
        /// 不锁定方向，允许自由旋转
        /// </summary>
        Unlocked = -1,
        /// <summary>
        /// 锁定为scrcpy启动时的当前方向
        /// </summary>
        Initial = -2,
        /// <summary>
        /// 自然方向（0度旋转）
        /// </summary>
        Orientation0 = 0,
        /// <summary>
        /// 逆时针旋转90度
        /// </summary>
        Orientation1,
        /// <summary>
        /// 旋转180度（上下颠倒）
        /// </summary>
        Orientation2,
        /// <summary>
        /// 顺时针旋转90度
        /// </summary>
        Orientation3,
    }
}
