using Yuandl.Common.Helper;

namespace AdAuto.Extensions
{
    public static class ArrayExtensions
    {
        private static readonly Random random = new Random();

        public static T GetRandomElement<T>(this T[] array)
        {
            return array[random.Next(array.Length)];
        }
        public static string[] GetArray(this string? str, char split = '|')
        {
            string[] values = new string[] { };
            if (!str.IsNotEmptyOrNull())
            {
                values = str.Split(split, StringSplitOptions.RemoveEmptyEntries);
            }
            return values;
        }
    }
}
