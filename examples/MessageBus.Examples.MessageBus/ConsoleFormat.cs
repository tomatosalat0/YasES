using System;
using System.Linq;

namespace MessageBus.Examples.MessageBus
{
    public sealed class ConsoleFormat
    {
        public static string Format(object value, params object[] formats)
        {
            return Format(value.ToString() ?? string.Empty, formats);
        }

        public static string Format(string value, params object[] formats)
        {
            return $"\u001b[{ApplyFormat(formats)}m{value}\u001b[0m\u00A0";
        }

        private static string ApplyFormat(params object[] formats)
        {
            return string.Join(";", formats.Select(p => p is Enum ? (int)p : p));
        }

        public static string OrderIdForLog(string orderId)
        {
            return Format(orderId, BackgroundColor.Green, ForegroundColor.White);
        }

        public static string OrderIdForOutput(string orderId)
        {
            return Format(orderId, BackgroundColor.Blue, ForegroundColor.White, TextStyle.Underline, TextStyle.Bold);
        }

        public static string Duration(TimeSpan duration)
        {
            return Format($"(duration: {duration})", ForegroundColor.BrightBlack);
        }

        public enum ForegroundColor
        {
            Black = 30,
            Red = 31,
            Green = 32,
            Yellow = 33,
            Blue = 34,
            Magenta = 35,
            Cyan = 36,
            White = 37,
            BrightBlack = 90,
            BrightRed = 91,
            BrightGreen = 92,
            BrightYellow = 93,
            BrightBlue = 94,
            BrightMagenta = 95,
            BrightCyan = 96,
            BrightWhite = 97
        }

        public enum BackgroundColor
        {
            Black = 40,
            Red = 41,
            Green = 42,
            Yellow = 43,
            Blue = 44,
            Magenta = 45,
            Cyan = 46,
            White = 47,
            BrightBlack = 100,
            BrightRed = 101,
            BrightGreen = 102,
            BrightYellow = 103,
            BrightBlue = 104,
            BrightMagenta = 105,
            BrightCyan = 106,
            BrightWhite = 107
        }

        public enum TextStyle
        {
            Bold = 1,
            Underline = 4,
            Reversed = 7
        }
    }
}
