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
        /// <summary>
        /// Only set on the server instance
        /// </summary>
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
        /// <summary>
        /// Sends a packet to the player
        /// </summary>
        /// <param name="packet">Packet to send</param>
        /// <param name="protocol">Protocol to use</param>
        /// <param name="server">Server variable for UDP</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Send(Packet packet, ProtocolType protocol = ProtocolType.Tcp, NetServer? server = null) {
            if (Vikinet.IsClientInstance) return;
            if (protocol == ProtocolType.Tcp) {
                _tcpStream.Write(packet.GetBytes());
                Log.Debug($"[TCP] Sent packet {packet.PacketId} to player {Identifier}({Username})");
            } else if (protocol == ProtocolType.Udp) {
                if (server == null) { throw new InvalidOperationException("Send didn't provide NetServer instance for UDP transmission"); }
                server.ServerUdp.Send(packet.GetBytes(), UdpEndpoint!);
                Log.Debug($"[UDP] Sent packet {packet.PacketId} to player {Identifier}({Username})");
            } else {
                throw new InvalidOperationException($"Invalid protocol provided {protocol}");
            }
            
        }
        /// <summary>
        /// Reads a packet from the player
        /// </summary>
        /// <param name="protocol">Protocol to read from</param>
        /// <returns>Packet instance</returns>
        /// <exception cref="Exception">Thrown if called from client</exception>
        /// <exception cref="InvalidOperationException"></exception>
        public Packet Read(ProtocolType protocol = ProtocolType.Tcp) {
            if (Vikinet.IsClientInstance) {
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
