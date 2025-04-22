namespace Vikinet2.Networking {
    public static class Log {
        public static bool WriteDebug { get; set; } = true;
        public static void Write(object? msg) {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss:fff} {msg}");
        }
        public static void Debug(object? msg) {
            if (!WriteDebug) return;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Write(msg);
            Console.ResetColor();
        }
        public static void Error(object? msg) {
            Console.ForegroundColor = ConsoleColor.Red;
            Write(msg);
            Console.ResetColor();
        }
    }
}
