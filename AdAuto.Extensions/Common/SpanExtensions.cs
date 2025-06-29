using System;
using System.Text;

/// <summary>
/// Span类型的扩展方法命名空间
/// </summary>
namespace AdAuto.Extensions.Common
{
    /// <summary>
    /// Span类型的扩展方法类
    /// 提供了一些实用的扩展方法来处理Span<byte>类型的数据
    /// </summary>
    public static class SpanExtensions
    {
        /// <summary>
        /// 将字节数组转换为十六进制字符串
        /// </summary>
        /// <param name="bytes">要转换的字节数组</param>
        /// <returns>转换后的十六进制字符串，每个字节用两位十六进制数表示</returns>
        public static string ToHexString(this Span<byte> bytes)
        {
            StringBuilder sb = new(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.AppendFormat("{0:X2}", b);
            }
            return sb.ToString();
        }
    }
}
