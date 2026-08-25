using System;
using System.Globalization;

namespace SP.Runtime.Utilities
{
    public static class SizeFormatter
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

        /// <summary>
        /// Преобразует количество байт в человекочитаемую строку (напр. "1.45 MB")
        /// </summary>
        public static string Format(long bytes)
        {
            switch (bytes)
            {
                case < 0:
                {
                    throw new ArgumentException("Размер не может быть отрицательным.");
                }
                case 0:
                {
                    return "0 B";
                }
            }

            var unitIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && unitIndex < Units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }
            
            return $"{size.ToString("0.##", CultureInfo.InvariantCulture)} {Units[unitIndex]}";
        }
    }
}