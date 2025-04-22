using Vikinet2.Networking;

namespace Vikinet2{
    public class Vikinet{
        // technically could be made constant for like, different builds such as dedicated server and client
        /// <summary>
        /// Should be set to true if this is a server instance
        /// </summary>
        public static bool IsServerInstance { get; private set; } = false;
        /// <summary>
        /// Should be set to true if this is a client instance
        /// </summary>
        public static bool IsClientInstance { get; private set; } = false;
        /// <summary>
        /// Creates a server instance and starts listening for connections
        /// </summary>
        /// <param name="port">Specified server port</param>
        /// <param name="maxPlayers">Maximum amount of players allowed</param>
        /// <returns>Connected and initialized NetServer instance, can be used to route through PacketRouter</returns>
        public static NetServer StartServer(ushort port, uint maxPlayers) {
            IsServerInstance = true;
            NetServer server = new(port, maxPlayers);
            server.Start();
            return server;
        }
        /// <summary>
        /// Creates a client instance and connects to the server
        /// </summary>
        /// <param name="port">Port to connect to</param>
        /// <param name="ip">IP to connect to</param>
        /// <param name="username">Username to use for connection</param>
        /// <returns>Connected and initialized NetClient instance, can be used to route through PacketRouter</returns>
        public static NetClient StartClient(ushort port, string ip, string username) {
            IsClientInstance = true;
            return new NetClient(username, ip, port);
        }
    }
}
