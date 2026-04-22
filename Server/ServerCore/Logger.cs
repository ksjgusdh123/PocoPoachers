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

        static void Write(LogLevel level, string msg)
        {
            var ts = DateTime.Now.ToString("HH:mm:ss");
            var tag = Tag(level);
            var line = string.IsNullOrEmpty(msg)
                ? $"[{ts}] {tag}"
                : $"[{ts}] {tag} {msg}";

            var prev = Console.ForegroundColor;
            if (level == LogLevel.Warn)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (level == LogLevel.Error)
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(line);
            Console.ForegroundColor = prev;
        }

        public static void LOG(string msg = "") => Write(LogLevel.Log, msg);
        public static void LOG_W(string msg = "") => Write(LogLevel.Warn, msg);
        public static void LOG_E(string msg = "") => Write(LogLevel.Error, msg);
    }
}
