using AdAuto.Extensions.enums;
using System.Windows.Input;

namespace AdAuto.Extensions.Helper
{
    /// <summary>
    /// 键码辅助类，用于将WPF的按键和修饰键转换为Android对应的键码和状态
    /// </summary>
    public static class KeycodeHelper
    {
        /// <summary>
        /// WPF按键到Android键码的映射字典，包含了常用的特殊按键映射关系
        /// </summary>
        private static readonly Dictionary<Key, AndroidKeycode> keycodeDict = new Dictionary<Key, AndroidKeycode>
        {
            { Key.Space, AndroidKeycode.AKEYCODE_SPACE },
            { Key.Back, AndroidKeycode.AKEYCODE_DEL },
            { Key.Left, AndroidKeycode.AKEYCODE_DPAD_LEFT },
            { Key.Up, AndroidKeycode.AKEYCODE_DPAD_UP },
            { Key.Right, AndroidKeycode.AKEYCODE_DPAD_RIGHT },
            { Key.Down, AndroidKeycode.AKEYCODE_DPAD_DOWN },
            { Key.Delete, AndroidKeycode.AKEYCODE_FORWARD_DEL },
            { Key.Tab, AndroidKeycode.AKEYCODE_TAB },
            { Key.Enter, AndroidKeycode.AKEYCODE_ENTER },
        };

        /// <summary>
        /// 将WPF按键转换为Android键码
        /// </summary>
        /// <param name="key">WPF按键枚举值</param>
        /// <returns>对应的Android键码，如果没有对应关系则返回AKEYCODE_UNKNOWN</returns>
        public static AndroidKeycode ConvertKey(Key key)
        {
            // A - Z
            if (key >= Key.A && key <= Key.Z)
            {
                int offset = (int)AndroidKeycode.AKEYCODE_A - (int)Key.A;
                return (AndroidKeycode)((int)key + offset);
            }
            // Digits 0-9
            else if (key >= Key.D0 && key <= Key.D9)
            {
                int offset = (int)AndroidKeycode.AKEYCODE_0 - (int)Key.D0;
                return (AndroidKeycode)((int)key + offset);
            }
            else if (keycodeDict.TryGetValue(key, out var androidKey))
            {
                return androidKey;
            }

            return AndroidKeycode.AKEYCODE_UNKNOWN;
        }

        /// <summary>
        /// 将WPF修饰键状态转换为Android元键状态
        /// </summary>
        /// <param name="keyModifiers">WPF修饰键状态</param>
        /// <returns>对应的Android元键状态，包含Shift、Control、Alt等修饰键的组合状态</returns>
        public static AndroidMetastate ConvertModifiers(ModifierKeys keyModifiers)
        {
            AndroidMetastate metastate = AndroidMetastate.AMETA_NONE;

            if (keyModifiers.HasFlag(ModifierKeys.Shift))
                metastate |= AndroidMetastate.AMETA_SHIFT_ON;

            if (keyModifiers.HasFlag(ModifierKeys.Control))
                metastate |= AndroidMetastate.AMETA_CTRL_ON;

            if (keyModifiers.HasFlag(ModifierKeys.Alt))
                metastate |= AndroidMetastate.AMETA_ALT_ON;

            return metastate;
        }
    }
}
