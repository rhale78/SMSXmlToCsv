using Spectre.Console;

namespace SMSXmlToCsv.Utils
{
    /// <summary>
    /// Console output helper using Spectre.Console for proper UTF-8/emoji support
    /// </summary>
    public static class ConsoleHelper
    {
        /// <summary>
        /// Write colored text with emoji support
        /// </summary>
        public static void Write(string text, ConsoleColor color)
        {
            string spectreColor = MapColor(color);
            AnsiConsole.Markup($"[{spectreColor}]{Markup.Escape(text)}[/]");
        }

        /// <summary>
        /// Write colored line with emoji support
        /// </summary>
        public static void WriteLine(string text, ConsoleColor color)
        {
            string spectreColor = MapColor(color);
            AnsiConsole.MarkupLine($"[{spectreColor}]{Markup.Escape(text)}[/]");
        }

        /// <summary>
        /// Write text without color
        /// </summary>
        public static void WriteLine(string text)
        {
            AnsiConsole.WriteLine(text);
        }

        /// <summary>
        /// Clear the current console line
        /// </summary>
        public static void ClearLine()
        {
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
        }

        /// <summary>
        /// Map ConsoleColor to Spectre color name
        /// </summary>
        private static string MapColor(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Red => "red",
                ConsoleColor.Green => "green",
                ConsoleColor.Yellow => "yellow",
                ConsoleColor.Blue => "blue",
                ConsoleColor.Cyan => "cyan",
                ConsoleColor.Magenta => "magenta",
                ConsoleColor.Gray => "grey",
                ConsoleColor.White => "white",
                ConsoleColor.DarkGray => "grey",
                ConsoleColor.DarkRed => "maroon",
                ConsoleColor.DarkGreen => "green",
                ConsoleColor.DarkYellow => "olive",
                ConsoleColor.DarkBlue => "navy",
                ConsoleColor.DarkCyan => "teal",
                ConsoleColor.DarkMagenta => "purple",
                _ => "white"
            };
        }
    }
}
