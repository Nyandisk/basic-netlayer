using smq.Networking;

namespace smq{
    public class Program{
        // technically could be made constant for like, different builds such as dedicated server and client
        public static bool IsServerInstance { get; private set; } = false;
        public static bool IsClientInstance { get; private set; } = false;
        public static void Main(string[] args){
            try {
                if (Console.ReadLine()! == "server") {
                    Console.Title = "Server";
                    IsServerInstance = true;
                    NetServer server = new(7070, 1);
                    server.Start();
                } else {
                    Console.Title = "Client";
                    IsClientInstance = true;
                    _ = new NetClient(Console.ReadLine()!, "127.0.0.1", 7070);
                }
            } finally {
                Console.ReadLine();
            }
        }
    }
}
