namespace Vikinet2.Networking {
    public static class Log {
        public static bool WriteDebug { get; set; } = true;
        public static bool WriteInfo { get; set; } = true;
        public static bool WriteError { get; set; } = true;
        public static void Write(object? msg) {
            if (!WriteInfo) return;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss:fff} {msg}");
        }
        public static void Debug(object? msg) {
            if (!WriteDebug) return;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss:fff} {msg}");
            Console.ResetColor();
        }
        public static void Error(object? msg) {
            if (!WriteError) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now:HH:mm:ss:fff} {msg}");
            Console.ResetColor();
        }
    }
}
