using System.Runtime.CompilerServices;

namespace ServerCore
{
    public enum LogLevel
    {
        Log,
        Warn,
        Error,
    }

    public static class Logger
    {
        static string Tag(LogLevel level) => level switch
        {
            LogLevel.Log => "[LOG]",
            LogLevel.Warn => "[WARN]",
            LogLevel.Error => "[ERROR]",
            _ => "[LOG]",
        };

        static void Write(LogLevel level, string msg, string member)
        {
            var ts = DateTime.Now.ToString("HH:mm:ss");
            var tag = Tag(level);
            var line = string.IsNullOrEmpty(msg)
                ? $"[{ts}] {tag} {member}"
                : $"[{ts}] {tag} {member} | {msg}";

            var prev = Console.ForegroundColor;
            if (level == LogLevel.Warn)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (level == LogLevel.Error)
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(line);
            Console.ForegroundColor = prev;
        }

        public static void LOG(string msg = "", [CallerMemberName] string member = "")
            => Write(LogLevel.Log, msg, member);

        public static void LOG_W(string msg = "", [CallerMemberName] string member = "")
            => Write(LogLevel.Warn, msg, member);

        public static void LOG_E(string msg = "", [CallerMemberName] string member = "")
            => Write(LogLevel.Error, msg, member);
    }
}
