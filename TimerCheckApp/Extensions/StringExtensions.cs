namespace TimerCheckApp.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 检查字符串是否不为空或null
        /// </summary>
        /// <param name="str">要检查的字符串</param>
        /// <returns>如果字符串不为空或null，则返回true；否则返回false</returns>
        public static bool IsNotEmptyOrNull(this string? str)
        {
            return !string.IsNullOrEmpty(str);
        }
    }
}