// Based on https://android.googlesource.com/platform/frameworks/native/+/master/include/android/keycodes.h

/// <summary>
/// Android系统按键码定义
/// 包含了所有Android系统支持的按键码，用于处理键盘、虚拟按键等输入事件
/// 代码基于Android原生按键系统头文件
/// </summary>
namespace AdAuto.Extensions.enums
{
    /// <summary>
    /// Android按键码枚举，定义了所有可能的按键值
    /// 包括标准键盘按键、功能键、多媒体键、游戏控制器按键等
    /// </summary>
    public enum AndroidKeycode : int
    {
        /// <summary>
        /// 未知按键代码
        /// </summary>
        AKEYCODE_UNKNOWN = 0,
        /// <summary>
        /// 软左键
        /// 通常位于手机显示屏下方，用作多功能特征键
        /// 用于选择显示屏左下角显示的软件定义功能
        /// </summary>
        AKEYCODE_SOFT_LEFT = 1,
        /// <summary>
        /// 软右键
        /// 通常位于手机显示屏下方，用作多功能特征键
        /// 用于选择显示屏右下角显示的软件定义功能
        /// </summary>
        AKEYCODE_SOFT_RIGHT = 2,
        /// <summary>
        /// Home键（主页键）
        /// 此按键由系统框架处理，永远不会传递给应用程序
        /// </summary>
        AKEYCODE_HOME = 3,
        /// <summary>
        /// 返回键
        /// </summary>
        AKEYCODE_BACK = 4,
        /// <summary>
        /// 通话键
        /// </summary>
        AKEYCODE_CALL = 5,
        /// <summary>
        /// 结束通话键
        /// </summary>
        AKEYCODE_ENDCALL = 6,
        /// <summary>
        /// 数字键'0'
        /// </summary>
        AKEYCODE_0 = 7,
        /// <summary>
        /// 数字键'1'
        /// </summary>
        AKEYCODE_1 = 8,
        /// <summary>
        /// 数字键'2'
        /// </summary>
        AKEYCODE_2 = 9,
        /// <summary>
        /// 数字键'3'
        /// </summary>
        AKEYCODE_3 = 10,
        /// <summary>
        /// 数字键'4'
        /// </summary>
        AKEYCODE_4 = 11,
        /// <summary>
        /// 数字键'5'
        /// </summary>
        AKEYCODE_5 = 12,
        /// <summary>
        /// 数字键'6'
        /// </summary>
        AKEYCODE_6 = 13,
        /// <summary>
        /// 数字键'7'
        /// </summary>
        AKEYCODE_7 = 14,
        /// <summary>
        /// 数字键'8'
        /// </summary>
        AKEYCODE_8 = 15,
        /// <summary>
        /// 数字键'9'
        /// </summary>
        AKEYCODE_9 = 16,
        /// <summary>
        /// 星号键'*'
        /// </summary>
        AKEYCODE_STAR = 17,
        /// <summary>
        /// 井号键'#'
        /// </summary>
        AKEYCODE_POUND = 18,
                /// <summary>
        /// 方向键向上
        /// 也可以由轨迹球动作合成
        /// </summary>
        AKEYCODE_DPAD_UP = 19,
        /// <summary>
        /// 方向键向下
        /// 也可以由轨迹球动作合成
        /// </summary>
        AKEYCODE_DPAD_DOWN = 20,
        /// <summary>
        /// 方向键向左
        /// 也可以由轨迹球动作合成
        /// </summary>
        AKEYCODE_DPAD_LEFT = 21,
        /// <summary>
        /// 方向键向右
        /// 也可以由轨迹球动作合成
        /// </summary>
        AKEYCODE_DPAD_RIGHT = 22,
        /// <summary>
        /// 方向键中心键
        /// 也可以由轨迹球动作合成
        /// </summary>
        AKEYCODE_DPAD_CENTER = 23,
        /// <summary>
        /// 音量增大键
        /// 用于调高扬声器音量
        /// </summary>
        AKEYCODE_VOLUME_UP = 24,
        /// <summary>
        /// 音量减小键
        /// 用于调低扬声器音量
        /// </summary>
        AKEYCODE_VOLUME_DOWN = 25,
        /// <summary>
        /// 电源键
        /// </summary>
        AKEYCODE_POWER = 26,
        /// <summary>
        /// 相机键
        /// 用于启动相机应用或拍摄照片
        /// </summary>
        AKEYCODE_CAMERA = 27,
        /// <summary>
        /// 清除键
        /// </summary>
        AKEYCODE_CLEAR = 28,
        /// <summary>
        /// 字母键'A'
        /// </summary>
        AKEYCODE_A = 29,
        /// <summary>
        /// 字母键'B'
        /// </summary>
        AKEYCODE_B = 30,
        /// <summary>
        /// 字母键'C'
        /// </summary>
        AKEYCODE_C = 31,
        /// <summary>
        /// 字母键'D'
        /// </summary>
        AKEYCODE_D = 32,
        /// <summary>
        /// 字母键'E'
        /// </summary>
        AKEYCODE_E = 33,
        /// <summary>
        /// 字母键'F'
        /// </summary>
        AKEYCODE_F = 34,
        /// <summary>
        /// 字母键'G'
        /// </summary>
        AKEYCODE_G = 35,
        /// <summary>
        /// 字母键'H'
        /// </summary>
        AKEYCODE_H = 36,
        /// <summary>
        /// 字母键'I'
        /// </summary>
        AKEYCODE_I = 37,
        /// <summary>
        /// 字母键'J'
        /// </summary>
        AKEYCODE_J = 38,
        /// <summary>
        /// 字母键'K'
        /// </summary>
        AKEYCODE_K = 39,
        /// <summary>
        /// 字母键'L'
        /// </summary>
        AKEYCODE_L = 40,
        /// <summary>
        /// 字母键'M'
        /// </summary>
        AKEYCODE_M = 41,
        /// <summary>
        /// 字母键'N'
        /// </summary>
        AKEYCODE_N = 42,
        /// <summary>
        /// 字母键'O'
        /// </summary>
        AKEYCODE_O = 43,
        /// <summary>
        /// 字母键'P'
        /// </summary>
        AKEYCODE_P = 44,
        /// <summary>
        /// 字母键'Q'
        /// </summary>
        AKEYCODE_Q = 45,
        /// <summary>
        /// 字母键'R'
        /// </summary>
        AKEYCODE_R = 46,
        /// <summary>
        /// 字母键'S'
        /// </summary>
        AKEYCODE_S = 47,
        /// <summary>
        /// 字母键'T'
        /// </summary>
        AKEYCODE_T = 48,
        /// <summary>
        /// 字母键'U'
        /// </summary>
        AKEYCODE_U = 49,
        /// <summary>
        /// 字母键'V'
        /// </summary>
        AKEYCODE_V = 50,
        /// <summary>
        /// 字母键'W'
        /// </summary>
        AKEYCODE_W = 51,
        /// <summary>
        /// 字母键'X'
        /// </summary>
        AKEYCODE_X = 52,
        /// <summary>
        /// 字母键'Y'
        /// </summary>
        AKEYCODE_Y = 53,
        /// <summary>
        /// 字母键'Z'
        /// </summary>
        AKEYCODE_Z = 54,
        /// <summary>
        /// 逗号键','，标点符号
        /// </summary>
        AKEYCODE_COMMA = 55,
        /// <summary>
        /// 句号键'.'，标点符号
        /// </summary>
        AKEYCODE_PERIOD = 56,
        /// <summary>
        /// 左Alt修饰键
        /// </summary>
        AKEYCODE_ALT_LEFT = 57,
        /// <summary>
        /// 右Alt修饰键
        /// </summary>
        AKEYCODE_ALT_RIGHT = 58,
        /// <summary>
        /// 左Shift修饰键
        /// </summary>
        AKEYCODE_SHIFT_LEFT = 59,
        /// <summary>
        /// 右Shift修饰键
        /// </summary>
        AKEYCODE_SHIFT_RIGHT = 60,
        /// <summary>
        /// Tab键（制表符键）
        /// </summary>
        AKEYCODE_TAB = 61,
        /// <summary>
        /// 空格键
        /// </summary>
        AKEYCODE_SPACE = 62,
        /// <summary>
        /// 符号修饰键
        /// 用于输入替代符号
        /// </summary>
        AKEYCODE_SYM = 63,
        /// <summary>
        /// 浏览器特殊功能键
        /// 用于启动浏览器应用程序
        /// </summary>
        AKEYCODE_EXPLORER = 64,
        /// <summary>
        /// 邮件特殊功能键
        /// 用于启动邮件应用程序
        /// </summary>
        AKEYCODE_ENVELOPE = 65,
        /// <summary>
        /// 回车键
        /// </summary>
        AKEYCODE_ENTER = 66,
        /// <summary>
        /// 退格键
        /// 删除插入点之前的字符，与AKEYCODE_FORWARD_DEL不同
        /// </summary>
        AKEYCODE_DEL = 67,
        /// <summary>
        /// 反引号键'`'
        /// </summary>
        AKEYCODE_GRAVE = 68,
        /// <summary>
        /// 减号键'-'
        /// </summary>
        AKEYCODE_MINUS = 69,
        /// <summary>
        /// 等号键'='
        /// </summary>
        AKEYCODE_EQUALS = 70,
        /// <summary>
        /// 左方括号键'['
        /// </summary>
        AKEYCODE_LEFT_BRACKET = 71,
        /// <summary>
        /// 右方括号键']'
        /// </summary>
        AKEYCODE_RIGHT_BRACKET = 72,
        /// <summary>
        /// 反斜杠键'\'
        /// </summary>
        AKEYCODE_BACKSLASH = 73,
        /// <summary>
        /// 分号键';'
        /// </summary>
        AKEYCODE_SEMICOLON = 74,
        /// <summary>
        /// 单引号键'''
        /// </summary>
        AKEYCODE_APOSTROPHE = 75,
        /// <summary>
        /// 斜杠键'/'
        /// </summary>
        AKEYCODE_SLASH = 76,
        /// <summary>
        /// 邮件符号键'@'
        /// </summary>
        AKEYCODE_AT = 77,
        /// <summary>
        /// 数字修饰键
        /// 用于输入数字符号
        /// 注意：此键不同于数字锁定键（NUM_LOCK），更类似于左Alt键
        /// </summary>
        AKEYCODE_NUM = 78,
        /// <summary>
        /// 耳机钩子键
        /// 用于挂断通话和停止媒体播放
        /// </summary>
        AKEYCODE_HEADSETHOOK = 79,
        /// <summary>
        /// 相机对焦键
        /// 用于控制相机对焦
        /// </summary>
        AKEYCODE_FOCUS = 80,
        /// <summary>
        /// 加号键'+'
        /// </summary>
        AKEYCODE_PLUS = 81,
        /// <summary>
        /// 菜单键
        /// </summary>
        AKEYCODE_MENU = 82,
        /// <summary>
        /// 通知键
        /// </summary>
        AKEYCODE_NOTIFICATION = 83,
        /// <summary>
        /// 搜索键
        /// </summary>
        AKEYCODE_SEARCH = 84,
        /// <summary>
        /// 媒体播放/暂停键
        /// </summary>
        AKEYCODE_MEDIA_PLAY_PAUSE = 85,
        /// <summary>
        /// 媒体停止键
        /// </summary>
        AKEYCODE_MEDIA_STOP = 86,
        /// <summary>
        /// 媒体下一曲键
        /// </summary>
        AKEYCODE_MEDIA_NEXT = 87,
        /// <summary>
        /// 媒体上一曲键
        /// </summary>
        AKEYCODE_MEDIA_PREVIOUS = 88,
        /// <summary>
        /// 媒体快退键
        /// </summary>
        AKEYCODE_MEDIA_REWIND = 89,
        /// <summary>
        /// 媒体快进键
        /// </summary>
        AKEYCODE_MEDIA_FAST_FORWARD = 90,
        /// <summary>
        /// 静音键
        /// 用于静音麦克风，与音量静音键（VOLUME_MUTE）不同
        /// </summary>
        AKEYCODE_MUTE = 91,
        /// <summary>
        /// 向上翻页键
        /// </summary>
        AKEYCODE_PAGE_UP = 92,
        /// <summary>
        /// 向下翻页键
        /// </summary>
        AKEYCODE_PAGE_DOWN = 93,
        /// <summary>
        /// 图片符号修饰键
        /// 用于切换符号集（表情符号、颜文字等）
        /// </summary>
        AKEYCODE_PICTSYMBOLS = 94,
        /// <summary>
        /// 字符集切换修饰键
        /// 用于切换字符集（如汉字、片假名等）
        /// </summary>
        AKEYCODE_SWITCH_CHARSET = 95,
        /// <summary>
        /// 游戏手柄A按钮
        /// 在游戏手柄上，A按钮应该是标有A的按钮
        /// 或者是手柄底部按钮行的第一个按钮
        /// </summary>
        AKEYCODE_BUTTON_A = 96,
        /// <summary>
        /// 游戏手柄B按钮
        /// 在游戏手柄上，B按钮应该是标有B的按钮
        /// 或者是手柄底部按钮行的第二个按钮
        /// </summary>
        AKEYCODE_BUTTON_B = 97,
        /// <summary>
        /// 游戏手柄C按钮
        /// 在游戏手柄上，C按钮应该是标有C的按钮
        /// 或者是手柄底部按钮行的第三个按钮
        /// </summary>
        AKEYCODE_BUTTON_C = 98,
        /// <summary>
        /// 游戏手柄X按钮
        /// 在游戏手柄上，X按钮应该是标有X的按钮
        /// 或者是手柄上部按钮行的第一个按钮
        /// </summary>
        AKEYCODE_BUTTON_X = 99,
        /// <summary>
        /// 游戏手柄Y按钮
        /// 在游戏手柄上，Y按钮应该是标有Y的按钮
        /// 或者是手柄上部按钮行的第二个按钮
        /// </summary>
        AKEYCODE_BUTTON_Y = 100,
        /// <summary>
        /// 游戏手柄Z按钮
        /// 在游戏手柄上，Z按钮应该是标有Z的按钮
        /// 或者是手柄上部按钮行的第三个按钮
        /// </summary>
        AKEYCODE_BUTTON_Z = 101,
        /// <summary>
        /// 游戏手柄L1按钮
        /// 在游戏手柄上，L1按钮应该是标有L1（或L）的按钮
        /// 或者是左上方的扳机按钮
        /// </summary>
        AKEYCODE_BUTTON_L1 = 102,
        /// <summary>
        /// 游戏手柄R1按钮
        /// 在游戏手柄上，R1按钮应该是标有R1（或R）的按钮
        /// 或者是右上方的扳机按钮
        /// </summary>
        AKEYCODE_BUTTON_R1 = 103,
        /// <summary>
        /// 游戏手柄L2按钮
        /// 在游戏手柄上，L2按钮应该是标有L2的按钮
        /// 或者是左下方的扳机按钮
        /// </summary>
        AKEYCODE_BUTTON_L2 = 104,
        /// <summary>
        /// 游戏手柄R2按钮
        /// 在游戏手柄上，R2按钮应该是标有R2的按钮
        /// 或者是右下方的扳机按钮
        /// </summary>
        AKEYCODE_BUTTON_R2 = 105,
        /// <summary>
        /// 左摇杆按钮
        /// 在游戏手柄上，表示左侧（或唯一的）摇杆被按下
        /// </summary>
        AKEYCODE_BUTTON_THUMBL = 106,
        /// <summary>
        /// 右摇杆按钮
        /// 在游戏手柄上，表示右侧摇杆被按下
        /// </summary>
        AKEYCODE_BUTTON_THUMBR = 107,
        /// <summary>
        /// 开始按钮
        /// 在游戏手柄上，标有Start的按钮
        /// </summary>
        AKEYCODE_BUTTON_START = 108,
        /// <summary>
        /// 选择按钮
        /// 在游戏手柄上，标有Select的按钮
        /// </summary>
        AKEYCODE_BUTTON_SELECT = 109,
        /// <summary>
        /// 模式按钮
        /// 在游戏手柄上，标有Mode的按钮
        /// </summary>
        AKEYCODE_BUTTON_MODE = 110,
        /// <summary>
        /// Esc键（退出键）
        /// </summary>
        AKEYCODE_ESCAPE = 111,
        /// <summary>
        /// 向前删除键
        /// 删除插入点之后的字符，与退格键（AKEYCODE_DEL）不同
        /// </summary>
        AKEYCODE_FORWARD_DEL = 112,
        /// <summary>
        /// 左Control修饰键
        /// </summary>
        AKEYCODE_CTRL_LEFT = 113,
        /// <summary>
        /// 右Control修饰键
        /// </summary>
        AKEYCODE_CTRL_RIGHT = 114,
        /// <summary>
        /// 大写锁定键
        /// </summary>
        AKEYCODE_CAPS_LOCK = 115,
        /// <summary>
        /// 滚动锁定键
        /// </summary>
        AKEYCODE_SCROLL_LOCK = 116,
        /// <summary>
        /// 左Meta修饰键（通常是Windows键或Command键）
        /// </summary>
        AKEYCODE_META_LEFT = 117,
        /// <summary>
        /// 右Meta修饰键（通常是Windows键或Command键）
        /// </summary>
        AKEYCODE_META_RIGHT = 118,
        /// <summary>
        /// 功能修饰键（Fn键）
        /// </summary>
        AKEYCODE_FUNCTION = 119,
        /// <summary>
        /// 系统请求/打印屏幕键
        /// </summary>
        AKEYCODE_SYSRQ = 120,
        /// <summary>
        /// 中断/暂停键
        /// </summary>
        AKEYCODE_BREAK = 121,
        /// <summary>
        /// Home光标移动键
        /// 用于滚动或将光标移动到行首
        /// 或列表顶部
        /// </summary>
        AKEYCODE_MOVE_HOME = 122,
        /// <summary>
        /// End光标移动键
        /// 用于滚动或将光标移动到行尾
        /// 或列表底部
        /// </summary>
        AKEYCODE_MOVE_END = 123,
        /// <summary>
        /// 插入键
        /// 切换插入/覆盖编辑模式
        /// </summary>
        AKEYCODE_INSERT = 124,
        /// <summary>
        /// 前进键
        /// 在历史记录中向前导航，与返回键（AKEYCODE_BACK）相对
        /// </summary>
        AKEYCODE_FORWARD = 125,
        /// <summary>
        /// 媒体播放键
        /// </summary>
        AKEYCODE_MEDIA_PLAY = 126,
        /// <summary>
        /// 媒体暂停键
        /// </summary>
        AKEYCODE_MEDIA_PAUSE = 127,
        /// <summary>
        /// 媒体关闭键
        /// 例如，可用于关闭CD托盘
        /// </summary>
        AKEYCODE_MEDIA_CLOSE = 128,
        /// <summary>
        /// 媒体弹出键
        /// 例如，可用于弹出CD托盘
        /// </summary>
        AKEYCODE_MEDIA_EJECT = 129,
        /// <summary>
        /// 媒体录制键
        /// </summary>
        AKEYCODE_MEDIA_RECORD = 130,
        /// <summary>
        /// 功能键F1
        /// </summary>
        AKEYCODE_F1 = 131,
        /// <summary>
        /// 功能键F2
        /// </summary>
        AKEYCODE_F2 = 132,
        /// <summary>
        /// 功能键F3
        /// </summary>
        AKEYCODE_F3 = 133,
        /// <summary>
        /// 功能键F4
        /// </summary>
        AKEYCODE_F4 = 134,
        /// <summary>
        /// 功能键F5
        /// </summary>
        AKEYCODE_F5 = 135,
        /// <summary>
        /// 功能键F6
        /// </summary>
        AKEYCODE_F6 = 136,
        /// <summary>
        /// 功能键F7
        /// </summary>
        AKEYCODE_F7 = 137,
        /// <summary>
        /// 功能键F8
        /// </summary>
        AKEYCODE_F8 = 138,
        /// <summary>
        /// 功能键F9
        /// </summary>
        AKEYCODE_F9 = 139,
        /// <summary>
        /// 功能键F10
        /// </summary>
        AKEYCODE_F10 = 140,
        /// <summary>
        /// 功能键F11
        /// </summary>
        AKEYCODE_F11 = 141,
        /// <summary>
        /// 功能键F12
        /// </summary>
        AKEYCODE_F12 = 142,
        /// <summary>
        /// 数字锁定键（Num Lock）
        /// 这是数字锁定键，与AKEYCODE_NUM不同
        /// 此键用于改变数字小键盘上其他键的行为
        /// </summary>
        AKEYCODE_NUM_LOCK = 143,
        /// <summary>
        /// 数字小键盘'0'键
        /// </summary>
        AKEYCODE_NUMPAD_0 = 144,
        /// <summary>
        /// 数字小键盘'1'键
        /// </summary>
        AKEYCODE_NUMPAD_1 = 145,
        /// <summary>
        /// 数字小键盘'2'键
        /// </summary>
        AKEYCODE_NUMPAD_2 = 146,
        /// <summary>
        /// 数字小键盘'3'键
        /// </summary>
        AKEYCODE_NUMPAD_3 = 147,
        /// <summary>
        /// 数字小键盘'4'键
        /// </summary>
        AKEYCODE_NUMPAD_4 = 148,
        /// <summary>
        /// 数字小键盘'5'键
        /// </summary>
        AKEYCODE_NUMPAD_5 = 149,
        /// <summary>
        /// 数字小键盘'6'键
        /// </summary>
        AKEYCODE_NUMPAD_6 = 150,
        /// <summary>
        /// 数字小键盘'7'键
        /// </summary>
        AKEYCODE_NUMPAD_7 = 151,
        /// <summary>
        /// 数字小键盘'8'键
        /// </summary>
        AKEYCODE_NUMPAD_8 = 152,
        /// <summary>
        /// 数字小键盘'9'键
        /// </summary>
        AKEYCODE_NUMPAD_9 = 153,
        /// <summary>
        /// 数字小键盘除号键'/'（用于除法）
        /// </summary>
        AKEYCODE_NUMPAD_DIVIDE = 154,
        /// <summary>
        /// 数字小键盘乘号键'*'（用于乘法）
        /// </summary>
        AKEYCODE_NUMPAD_MULTIPLY = 155,
        /// <summary>
        /// 数字小键盘减号键'-'（用于减法）
        /// </summary>
        AKEYCODE_NUMPAD_SUBTRACT = 156,
        /// <summary>
        /// 数字小键盘加号键'+'（用于加法）
        /// </summary>
        AKEYCODE_NUMPAD_ADD = 157,
        /// <summary>
        /// 数字小键盘点号键'.'（用于小数点或数字分组）
        /// </summary>
        AKEYCODE_NUMPAD_DOT = 158,
        /// <summary>
        /// 数字小键盘逗号键','（用于小数点或数字分组）
        /// </summary>
        AKEYCODE_NUMPAD_COMMA = 159,
        /// <summary>
        /// 数字小键盘回车键
        /// </summary>
        AKEYCODE_NUMPAD_ENTER = 160,
        /// <summary>
        /// 数字小键盘等号键'='
        /// </summary>
        AKEYCODE_NUMPAD_EQUALS = 161,
        /// <summary>
        /// 数字小键盘左括号键'('
        /// </summary>
        AKEYCODE_NUMPAD_LEFT_PAREN = 162,
        /** Numeric keypad ')' key. */
        AKEYCODE_NUMPAD_RIGHT_PAREN = 163,
        /// <summary>
        /// 音量静音键
        /// 与AKEYCODE_MUTE不同，此键用于控制扬声器静音
        /// 通常实现为切换功能：第一次按下静音，第二次按下恢复原音量
        /// </summary>
        AKEYCODE_VOLUME_MUTE = 164,
        /// <summary>
        /// 信息键
        /// 常见于电视遥控器，用于显示当前观看内容的附加信息
        /// </summary>
        AKEYCODE_INFO = 165,
        /// <summary>
        /// 频道增加键
        /// 在电视遥控器上用于切换到下一个电视频道
        /// </summary>
        AKEYCODE_CHANNEL_UP = 166,
        /// <summary>
        /// 频道减少键
        /// 在电视遥控器上用于切换到上一个电视频道
        /// </summary>
        AKEYCODE_CHANNEL_DOWN = 167,
        /// <summary>
        /// 放大键
        /// 用于放大显示内容
        /// </summary>
        AKEYCODE_ZOOM_IN = 168,
        /// <summary>
        /// 缩小键
        /// 用于缩小显示内容
        /// </summary>
        AKEYCODE_ZOOM_OUT = 169,
        /// <summary>
        /// 电视键
        /// 在电视遥控器上用于切换到实时电视观看模式
        /// </summary>
        AKEYCODE_TV = 170,
        /// <summary>
        /// 窗口键
        /// 在电视遥控器上用于切换画中画模式或其他窗口功能
        /// </summary>
        AKEYCODE_WINDOW = 171,
        /// <summary>
        /// 指南键
        /// 在电视遥控器上用于显示节目指南
        /// </summary>
        AKEYCODE_GUIDE = 172,
        /// <summary>
        /// 数字视频录像机（DVR）键
        /// 在某些电视遥控器上用于切换到录制节目的DVR模式
        /// </summary>
        AKEYCODE_DVR = 173,
        /// <summary>
        /// 书签键
        /// 在某些电视遥控器上用于为内容或网页添加书签
        /// </summary>
        AKEYCODE_BOOKMARK = 174,
        /// <summary>
        /// 字幕切换键
        /// 用于切换电视节目等内容的隐藏字幕模式
        /// </summary>
        AKEYCODE_CAPTIONS = 175,
        /// <summary>
        /// 设置键
        /// 用于启动系统设置活动
        /// </summary>
        AKEYCODE_SETTINGS = 176,
        /// <summary>
        /// 电视电源键
        /// 在电视遥控器上用于切换电视屏幕的电源状态
        /// </summary>
        AKEYCODE_TV_POWER = 177,
        /// <summary>
        /// 电视输入源键
        /// 在电视遥控器上用于切换电视屏幕的输入源
        /// </summary>
        AKEYCODE_TV_INPUT = 178,
        /// <summary>
        /// 机顶盒电源键
        /// 在电视遥控器上用于切换外部机顶盒的电源状态
        /// </summary>
        AKEYCODE_STB_POWER = 179,
        /// <summary>
        /// 机顶盒输入源键
        /// 在电视遥控器上用于切换外部机顶盒的输入模式
        /// </summary>
        AKEYCODE_STB_INPUT = 180,
        /// <summary>
        /// 音视频接收器电源键
        /// 在电视遥控器上用于切换外部音视频接收器的电源状态
        /// </summary>
        AKEYCODE_AVR_POWER = 181,
        /// <summary>
        /// 音视频接收器输入源键
        /// 在电视遥控器上用于切换外部音视频接收器的输入模式
        /// </summary>
        AKEYCODE_AVR_INPUT = 182,
        /// <summary>
        /// 红色可编程键
        /// 在电视遥控器上作为上下文相关的可编程功能键
        /// </summary>
        AKEYCODE_PROG_RED = 183,
        /// <summary>
        /// 绿色可编程键
        /// 在电视遥控器上作为上下文相关的可编程功能键
        /// </summary>
        AKEYCODE_PROG_GREEN = 184,
        /// <summary>
        /// 黄色可编程键
        /// 在电视遥控器上作为上下文相关的可编程功能键
        /// </summary>
        AKEYCODE_PROG_YELLOW = 185,
        /// <summary>
        /// 蓝色可编程键
        /// 在电视遥控器上作为上下文相关的可编程功能键
        /// </summary>
        AKEYCODE_PROG_BLUE = 186,
        /// <summary>
        /// 应用切换键
        /// 用于调出应用程序切换对话框
        /// </summary>
        AKEYCODE_APP_SWITCH = 187,
        /// <summary>
        /// 游戏手柄通用按钮#1
        /// </summary>
        AKEYCODE_BUTTON_1 = 188,
        /// <summary>
        /// 游戏手柄通用按钮#2
        /// </summary>
        AKEYCODE_BUTTON_2 = 189,
        /// <summary>
        /// 游戏手柄通用按钮#3
        /// </summary>
        AKEYCODE_BUTTON_3 = 190,
        /// <summary>
        /// 游戏手柄通用按钮#4
        /// </summary>
        AKEYCODE_BUTTON_4 = 191,
        /// <summary>
        /// 游戏手柄通用按钮#5
        /// </summary>
        AKEYCODE_BUTTON_5 = 192,
        /// <summary>
        /// 游戏手柄通用按钮#6
        /// </summary>
        AKEYCODE_BUTTON_6 = 193,
        /// <summary>
        /// 游戏手柄通用按钮#7
        /// </summary>
        AKEYCODE_BUTTON_7 = 194,
        /// <summary>
        /// 游戏手柄通用按钮#8
        /// </summary>
        AKEYCODE_BUTTON_8 = 195,
        /// <summary>
        /// 游戏手柄通用按钮#9
        /// </summary>
        AKEYCODE_BUTTON_9 = 196,
        /// <summary>
        /// 游戏手柄通用按钮#10
        /// </summary>
        AKEYCODE_BUTTON_10 = 197,
        /// <summary>
        /// 游戏手柄通用按钮#11
        /// </summary>
        AKEYCODE_BUTTON_11 = 198,
        /// <summary>
        /// 游戏手柄通用按钮#12
        /// </summary>
        AKEYCODE_BUTTON_12 = 199,
        /// <summary>
        /// 游戏手柄通用按钮#13
        /// </summary>
        AKEYCODE_BUTTON_13 = 200,
        /// <summary>
        /// 游戏手柄通用按钮#14
        /// </summary>
        AKEYCODE_BUTTON_14 = 201,
        /// <summary>
        /// 游戏手柄通用按钮#15
        /// </summary>
        AKEYCODE_BUTTON_15 = 202,
        /// <summary>
        /// 游戏手柄通用按钮#16
        /// </summary>
        AKEYCODE_BUTTON_16 = 203,
        /// <summary>
        /// 语言切换键
        /// 用于切换当前输入语言，如在QWERTY键盘上在英语和日语之间切换
        /// 在某些设备上，可以通过按下Shift+空格键实现相同功能
        /// </summary>
        AKEYCODE_LANGUAGE_SWITCH = 204,
        /// <summary>
        /// 礼仪模式键
        /// 用于切换静音或振动模式，使设备在某些场合（如拥挤的火车上）表现得更加礼貌
        /// 在某些设备上，此键可能需要长按才能生效
        /// </summary>
        AKEYCODE_MANNER_MODE = 205,
        /// <summary>
        /// 3D模式键
        /// 用于在2D和3D显示模式之间切换
        /// </summary>
        AKEYCODE_3D_MODE = 206,
        /// <summary>
        /// 联系人特殊功能键
        /// 用于启动通讯录应用程序
        /// </summary>
        AKEYCODE_CONTACTS = 207,
        /// <summary>
        /// 日历特殊功能键
        /// 用于启动日历应用程序
        /// </summary>
        AKEYCODE_CALENDAR = 208,
        /// <summary>
        /// 音乐特殊功能键
        /// 用于启动音乐播放器应用程序
        /// </summary>
        AKEYCODE_MUSIC = 209,
        /// <summary>
        /// 计算器特殊功能键
        /// 用于启动计算器应用程序
        /// </summary>
        AKEYCODE_CALCULATOR = 210,
        /// <summary>
        /// 日语全角/半角切换键
        /// 用于在日语输入时切换全角和半角字符
        /// </summary>
        AKEYCODE_ZENKAKU_HANKAKU = 211,
        /// <summary>
        /// 日语英数字符键
        /// 用于切换到英文字母和数字输入模式
        /// </summary>
        AKEYCODE_EISU = 212,
        /// <summary>
        /// 日语无变换键
        /// 用于在日语输入时保持原始输入，不进行假名汉字转换
        /// </summary>
        AKEYCODE_MUHENKAN = 213,
        /// <summary>
        /// 日语变换键
        /// 用于在日语输入时将假名转换为汉字
        /// </summary>
        AKEYCODE_HENKAN = 214,
        /// <summary>
        /// 日语片假名/平假名切换键
        /// 用于在片假名和平假名输入模式之间切换
        /// </summary>
        AKEYCODE_KATAKANA_HIRAGANA = 215,
        /// <summary>
        /// 日语日元符号键
        /// 用于输入日元货币符号（¥）
        /// </summary>
        AKEYCODE_YEN = 216,
        /// <summary>
        /// 日语Ro键
        /// 用于输入日语罗马字「ろ」
        /// </summary>
        AKEYCODE_RO = 217,
        /// <summary>
        /// 日语假名键
        /// 用于切换到假名输入模式
        /// </summary>
        AKEYCODE_KANA = 218,
        /// <summary>
        /// 辅助键
        /// 用于启动全局辅助活动，不会传递给应用程序
        /// </summary>
        AKEYCODE_ASSIST = 219,
        /// <summary>
        /// 亮度降低键
        /// 用于调低屏幕亮度
        /// </summary>
        AKEYCODE_BRIGHTNESS_DOWN = 220,
        /// <summary>
        /// 亮度提高键
        /// 用于调高屏幕亮度
        /// </summary>
        AKEYCODE_BRIGHTNESS_UP = 221,
        /// <summary>
        /// 音频轨道键
        /// 用于切换音频轨道
        /// </summary>
        AKEYCODE_MEDIA_AUDIO_TRACK = 222,
        /// <summary>
        /// 睡眠键
        /// 使设备进入睡眠状态，类似于电源键（AKEYCODE_POWER）
        /// 但当设备已处于睡眠状态时，此键无效
        /// </summary>
        AKEYCODE_SLEEP = 223,
        /// <summary>
        /// 唤醒键
        /// 唤醒设备，类似于电源键（AKEYCODE_POWER）
        /// 但当设备已处于唤醒状态时，此键无效
        /// </summary>
        AKEYCODE_WAKEUP = 224,
        /// <summary>
        /// 配对键
        /// 启动外设配对模式，用于配对遥控器或游戏控制器
        /// 特别是在没有其他输入模式可用时非常有用
        /// </summary>
        AKEYCODE_PAIRING = 225,
        /// <summary>
        /// 媒体顶部菜单键
        /// 跳转到媒体菜单顶部
        /// </summary>
        AKEYCODE_MEDIA_TOP_MENU = 226,
        /// <summary>
        /// 数字键'11'
        /// </summary>
        AKEYCODE_11 = 227,
        /// <summary>
        /// 数字键'12'
        /// </summary>
        AKEYCODE_12 = 228,
        /// <summary>
        /// 上一频道键
        /// 跳转到上次观看的频道
        /// </summary>
        AKEYCODE_LAST_CHANNEL = 229,
        /// <summary>
        /// 电视数据服务键
        /// 显示天气、体育等数据服务
        /// </summary>
        AKEYCODE_TV_DATA_SERVICE = 230,
        /// <summary>
        /// 语音助手键
        /// 启动全局语音助手活动，不会传递给应用程序
        /// </summary>
        AKEYCODE_VOICE_ASSIST = 231,
        /// <summary>
        /// 广播键
        /// 在电视服务和广播服务之间切换
        /// </summary>
        AKEYCODE_TV_RADIO_SERVICE = 232,
        /// <summary>
        /// 图文电视键
        /// 显示图文电视服务
        /// </summary>
        AKEYCODE_TV_TELETEXT = 233,
        /// <summary>
        /// 数字输入键
        /// 当每个数字键被分配用于选择不同频道时，用于输入多位数字频道号
        /// 对应CEC用户控制代码的数字输入模式（0x1D）
        /// </summary>
        AKEYCODE_TV_NUMBER_ENTRY = 234,
        /// <summary>
        /// 模拟地面广播键
        /// 切换到模拟地面广播服务
        /// </summary>
        AKEYCODE_TV_TERRESTRIAL_ANALOG = 235,
        /// <summary>
        /// 数字地面广播键
        /// 切换到数字地面广播服务
        /// </summary>
        AKEYCODE_TV_TERRESTRIAL_DIGITAL = 236,
        /// <summary>
        /// 卫星广播键
        /// 切换到数字卫星广播服务
        /// </summary>
        AKEYCODE_TV_SATELLITE = 237,
        /// <summary>
        /// BS卫星广播键
        /// 切换到日本BS数字卫星广播服务
        /// </summary>
        AKEYCODE_TV_SATELLITE_BS = 238,
        /// <summary>
        /// CS卫星广播键
        /// 切换到日本CS数字卫星广播服务
        /// </summary>
        AKEYCODE_TV_SATELLITE_CS = 239,
        /// <summary>
        /// BS/CS切换键
        /// 在BS和CS数字卫星服务之间切换
        /// </summary>
        AKEYCODE_TV_SATELLITE_SERVICE = 240,
        /// <summary>
        /// 网络切换键
        /// 用于切换选择广播服务
        /// </summary>
        AKEYCODE_TV_NETWORK = 241,
        /// <summary>
        /// 天线/有线切换键
        /// 在天线和有线电视输入源之间切换
        /// </summary>
        AKEYCODE_TV_ANTENNA_CABLE = 242,
        /// <summary>
        /// HDMI输入源1键
        /// 切换到HDMI输入源1
        /// </summary>
        AKEYCODE_TV_INPUT_HDMI_1 = 243,
        /// <summary>
        /// HDMI输入源2键
        /// 切换到HDMI输入源2
        /// </summary>
        AKEYCODE_TV_INPUT_HDMI_2 = 244,
        /// <summary>
        /// HDMI输入源3键
        /// 切换到HDMI输入源3
        /// </summary>
        AKEYCODE_TV_INPUT_HDMI_3 = 245,
        /// <summary>
        /// HDMI输入源4键
        /// 切换到HDMI输入源4
        /// </summary>
        AKEYCODE_TV_INPUT_HDMI_4 = 246,
        /// <summary>
        /// 复合视频输入源1键
        /// 切换到复合视频输入源1
        /// </summary>
        AKEYCODE_TV_INPUT_COMPOSITE_1 = 247,
        /// <summary>
        /// 复合视频输入源2键
        /// 切换到复合视频输入源2
        /// </summary>
        AKEYCODE_TV_INPUT_COMPOSITE_2 = 248,
        /// <summary>
        /// 分量视频输入源1键
        /// 切换到分量视频输入源1
        /// </summary>
        AKEYCODE_TV_INPUT_COMPONENT_1 = 249,
        /// <summary>
        /// 分量视频输入源2键
        /// 切换到分量视频输入源2
        /// </summary>
        AKEYCODE_TV_INPUT_COMPONENT_2 = 250,
        /// <summary>
        /// VGA输入源1键
        /// 切换到VGA（模拟RGB）输入源1
        /// </summary>
        AKEYCODE_TV_INPUT_VGA_1 = 251,
        /// <summary>
        /// 音频描述键
        /// 切换音频描述功能的开启/关闭
        /// </summary>
        AKEYCODE_TV_AUDIO_DESCRIPTION = 252,
        /// <summary>
        /// 音频描述混音音量增大键
        /// 相对于正常音频音量增大音频描述的音量
        /// </summary>
        AKEYCODE_TV_AUDIO_DESCRIPTION_MIX_UP = 253,
        /// <summary>
        /// 音频描述混音音量减小键
        /// 相对于正常音频音量减小音频描述的音量
        /// </summary>
        AKEYCODE_TV_AUDIO_DESCRIPTION_MIX_DOWN = 254,
        /// <summary>
        /// 缩放模式键
        /// 更改缩放模式（正常、全屏、缩放、宽屏缩放等）
        /// </summary>
        AKEYCODE_TV_ZOOM_MODE = 255,
        /// <summary>
        /// 内容菜单键
        /// 跳转到标题列表，对应CEC用户控制代码的内容菜单（0x0B）
        /// </summary>
        AKEYCODE_TV_CONTENTS_MENU = 256,
        /// <summary>
        /// 媒体上下文菜单键
        /// 跳转到媒体内容的上下文菜单，对应CEC用户控制代码的媒体上下文菜单（0x11）
        /// </summary>
        AKEYCODE_TV_MEDIA_CONTEXT_MENU = 257,
        /// <summary>
        /// 定时器编程键
        /// 跳转到定时录制菜单，对应CEC用户控制代码的定时器编程（0x54）
        /// </summary>
        AKEYCODE_TV_TIMER_PROGRAMMING = 258,
        /** Help key. */
        AKEYCODE_HELP = 259,
        /// <summary>
        /// 导航到上一项
        /// 用于在界面元素间向后导航
        /// </summary>
        AKEYCODE_NAVIGATE_PREVIOUS = 260,
        /// <summary>
        /// 导航到下一项
        /// 用于在界面元素间向前导航
        /// </summary>
        AKEYCODE_NAVIGATE_NEXT = 261,
        /// <summary>
        /// 导航进入
        /// 用于进入当前选中的界面元素或子菜单
        /// </summary>
        AKEYCODE_NAVIGATE_IN = 262,
        /// <summary>
        /// 导航退出
        /// 用于退出当前界面元素或返回上级菜单
        /// </summary>
        AKEYCODE_NAVIGATE_OUT = 263,
        /// <summary>
        /// Wear设备的主要按钮
        /// 智能手表上的主电源/重置按钮
        /// </summary>
        AKEYCODE_STEM_PRIMARY = 264,
        /// <summary>
        /// Wear设备的通用按钮1
        /// 智能手表上的可自定义功能按钮
        /// </summary>
        AKEYCODE_STEM_1 = 265,
        /// <summary>
        /// Wear设备的通用按钮2
        /// 智能手表上的可自定义功能按钮
        /// </summary>
        AKEYCODE_STEM_2 = 266,
        /// <summary>
        /// Wear设备的通用按钮3
        /// 智能手表上的可自定义功能按钮
        /// </summary>
        AKEYCODE_STEM_3 = 267,
        /// <summary>
        /// 方向键左上
        /// 组合方向键，同时表示向上和向左的方向
        /// </summary>
        AKEYCODE_DPAD_UP_LEFT = 268,
        /// <summary>
        /// 方向键左下
        /// 组合方向键，同时表示向下和向左的方向
        /// </summary>
        AKEYCODE_DPAD_DOWN_LEFT = 269,
        /// <summary>
        /// 方向键右上
        /// 组合方向键，同时表示向上和向右的方向
        /// </summary>
        AKEYCODE_DPAD_UP_RIGHT = 270,
        /// <summary>
        /// 方向键右下
        /// 组合方向键，同时表示向下和向右的方向
        /// </summary>
        AKEYCODE_DPAD_DOWN_RIGHT = 271,
        /// <summary>
        /// 媒体快进键
        /// 用于快速跳过当前媒体内容
        /// </summary>
        AKEYCODE_MEDIA_SKIP_FORWARD = 272,
        /// <summary>
        /// 媒体快退键
        /// 用于快速回退媒体内容
        /// </summary>
        AKEYCODE_MEDIA_SKIP_BACKWARD = 273,
        /// <summary>
        /// 媒体逐帧前进键
        /// 每次按下向前移动一帧媒体内容
        /// </summary>
        AKEYCODE_MEDIA_STEP_FORWARD = 274,
        /// <summary>
        /// 媒体逐帧后退键
        /// 每次按下向后移动一帧媒体内容
        /// </summary>
        AKEYCODE_MEDIA_STEP_BACKWARD = 275,
        /// <summary>
        /// 软休眠键
        /// 使设备进入休眠状态，除非有唤醒锁
        /// </summary>
        AKEYCODE_SOFT_SLEEP = 276,
        /// <summary>
        /// 剪切键
        /// 用于剪切选中的内容
        /// </summary>
        AKEYCODE_CUT = 277,
        /// <summary>
        /// 复制键
        /// 用于复制选中的内容
        /// </summary>
        AKEYCODE_COPY = 278,
        /// <summary>
        /// 粘贴键
        /// 用于粘贴已复制的内容
        /// </summary>
        AKEYCODE_PASTE = 279,
        /// <summary>
        /// 系统导航向上键
        /// 指纹导航的向上手势
        /// </summary>
        AKEYCODE_SYSTEM_NAVIGATION_UP = 280,
        /// <summary>
        /// 系统导航向下键
        /// 指纹导航的向下手势
        /// </summary>
        AKEYCODE_SYSTEM_NAVIGATION_DOWN = 281,
        /// <summary>
        /// 系统导航向左键
        /// 指纹导航的向左手势
        /// </summary>
        AKEYCODE_SYSTEM_NAVIGATION_LEFT = 282,
        /// <summary>
        /// 系统导航向右键
        /// 指纹导航的向右手势
        /// </summary>
        AKEYCODE_SYSTEM_NAVIGATION_RIGHT = 283,
        /// <summary>
        /// 所有应用程序键
        /// 用于打开所有应用程序列表或应用程序抽屉
        /// 通常在Android设备的主屏幕或启动器中使用
        /// </summary>
        AKEYCODE_ALL_APPS = 284
    }
}
