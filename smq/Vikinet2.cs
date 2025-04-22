using Vikinet2.Networking;

namespace Vikinet2{
    public class Vikinet2{
        // technically could be made constant for like, different builds such as dedicated server and client
        public static bool IsServerInstance { get; private set; } = false;
        public static bool IsClientInstance { get; private set; } = false;
        public static NetServer StartServer(ushort port, uint maxPlayers) {
            IsServerInstance = true;
            NetServer server = new(port, maxPlayers);
            server.Start();
            return server;
        }
        public static NetClient StartClient(ushort port, string ip, string username) {
            IsClientInstance = true;
            return new NetClient(username, ip, port);
        }
    }
}
