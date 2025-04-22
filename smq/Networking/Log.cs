namespace Vikinet2.Networking {
    public static class Log {
        public static void Write(object? msg) {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} {msg}");
        }
    }
}
