using System.Net;
using System.Net.Sockets;

namespace Vikinet2.Networking {
    public class NetworkPlayer {
        public uint Identifier { get; }
        public string Username { get; set; }
        /// <summary>
        /// Only set on the server instance
        /// </summary>
        public TcpClient TcpClient { get; }
        public IPEndPoint? UdpEndpoint { get; }
        /// <summary>
        /// Only set on the server instance
        /// </summary>
        private readonly NetworkStream _tcpStream;
        /// <summary>
        /// Server initializer
        /// </summary>
        public NetworkPlayer(uint identifier, string username, TcpClient tcpClient, IPEndPoint? udpEndpoint) {
            Identifier = identifier;
            Username = username;
            
            TcpClient = tcpClient;
            TcpClient.NoDelay = true;
            _tcpStream = TcpClient.GetStream();

            UdpEndpoint = udpEndpoint;
        }
        /// <summary>
        /// Client initializer
        /// </summary>
        public NetworkPlayer(uint identifier, string username) {
            Identifier = identifier;
            Username = username;
            TcpClient = null!;
            _tcpStream = null!;
        }
        public void Send(Packet packet, ProtocolType protocol = ProtocolType.Tcp, NetServer? server = null) {
            if (Vikinet2.IsClientInstance) return;
            if (protocol == ProtocolType.Tcp) {
                _tcpStream.Write(packet.GetBytes());
                Log.Debug($"[TCP] Sent packet {packet.PacketId} to player {Identifier}({Username})");
            } else if (protocol == ProtocolType.Udp) {
                if (server == null) { throw new Exception("Send didn't provide NetServer instance for UDP transmission"); }
                server.ServerUdp.Send(packet.GetBytes(), UdpEndpoint!);
                Log.Debug($"[UDP] Sent packet {packet.PacketId} to player {Identifier}({Username})");
            } else {
                throw new InvalidOperationException($"Invalid protocol provided {protocol}");
            }
            
        }
        public Packet Read(ProtocolType protocol = ProtocolType.Tcp) {
            if (Vikinet2.IsClientInstance) {
                throw new Exception("Client instance cannot read packets from NetworkPlayer container");
            }
            if (protocol == ProtocolType.Tcp) {
                Packet pck = Packet.FromStream(_tcpStream);
                Log.Debug($"[TCP] Received packet {pck.PacketId} from player {Identifier}({Username})");
                return pck;
            } else if (protocol == ProtocolType.Udp) {
                throw new InvalidOperationException($"Reading UDP from individual clients cannot be done through Read (NetServer)");
            } else {
                throw new InvalidOperationException($"Invalid protocol provided {protocol}");
            }
        }
    }
}
