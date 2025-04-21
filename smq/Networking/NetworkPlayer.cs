using System.Net.Sockets;

namespace smq.Networking {
    public class NetworkPlayer {
        public uint Identifier { get; }
        public string Username { get; set; }
        /// <summary>
        /// Only set on the server instance
        /// </summary>
        public TcpClient Client { get; }
        /// <summary>
        /// Only set on the server instance
        /// </summary>
        private readonly NetworkStream _stream;
        /// <summary>
        /// Server initializer
        /// </summary>
        public NetworkPlayer(uint identifier, string username, TcpClient client) {
            Identifier = identifier;
            Username = username;
            Client = client;
            Client.NoDelay = true;
            _stream = Client.GetStream();
        }
        /// <summary>
        /// Client initializer
        /// </summary>
        public NetworkPlayer(uint identifier, string username) {
            Identifier = identifier;
            Username = username;
            Client = null!;
            _stream = null!;
        }
        public void Send(Packet packet) {
            if (Program.IsClientInstance) return;
            Console.WriteLine($"Sending packet {packet.PacketId} to player {Identifier}({Username})");
            _stream.Write(packet.GetBytes());
        }
        public Packet Read() {
            if (Program.IsClientInstance) {
                throw new Exception("Client instance cannot read packets from NetworkPlayer container");
            }
            Packet pck = Packet.FromStream(_stream);
            Console.WriteLine($"Received packet {pck.PacketId} from player {Identifier}({Username})");
            return pck;
        }
    }
}
