namespace Vikinet2.Networking {
    public static class Log {
        /// <summary>
        /// Set to false to disable debug messages
        /// </summary>
        public static bool WriteDebug { get; set; } = true;
        /// <summary>
        /// Set to false to disable info messages
        /// </summary>
        public static bool WriteInfo { get; set; } = true;
        /// <summary>
        /// Set to false to disable error messages
        /// </summary>
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
