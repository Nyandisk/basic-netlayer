using System.Net;
using System.Net.Sockets;

namespace Vikinet2.Networking {
    public class NetClient {
        public List<NetworkPlayer> Players { get; } = new();
        public uint LocalIdentifier { get; private set; } = 0;
        public string LocalUsername { get; private set; } = string.Empty;
        public TcpClient TcpClient { get; }
        public UdpClient UdpClient { get; }

        private NetworkStream _stream;
        private readonly PacketRouter _router = new();
        public PacketRouter Router => _router;
        private readonly IPEndPoint _serverEndpoint = null!;

        public NetClient(string username, string ip, ushort port) {
            if (!Vikinet.IsClientInstance) {
                throw new Exception("Only accessible from client instance");
            }

            LocalUsername = username;
            TcpClient = new(ip, port) { NoDelay = true };
            UdpClient = new(ip, port) { DontFragment = true };
            _serverEndpoint = new(IPAddress.Parse(ip), port);

            _stream = TcpClient.GetStream();

            if (string.IsNullOrEmpty(username) || username.Length < 3 || username.Length > 20) {
                Log.Write($"Invalid username {username} (must be between 3 and 20 characters long)");
                TcpClient.Close();
                UdpClient.Close();
                return;
            }
            Log.Write($"Connected to server at {ip}:{port} with username {username}");

            Send(new(PacketID.CS_Discovery), ProtocolType.Udp);
            Packet pck = Read(ProtocolType.Udp);
            if (pck.PacketId != PacketID.SC_RespondDiscovery) {
                Log.Write($"Server sent weird packet {pck.PacketId} on discovery");
                TcpClient.Close();
                UdpClient.Close();
                return;
            }
            Log.Write($"Server acknowledged UDP discovery");

            pck = Read();
            if (pck.PacketId == PacketID.SC_Kick) {
                Log.Write($"Server kicked client for reason {(KickReason)pck.ReadUInt()}");
                TcpClient.Close();
                UdpClient.Close();
                return;
            } else if (pck.PacketId != PacketID.Acknowledge) {
                Log.Write($"Server sent weird packet {pck.PacketId} on connection");
                TcpClient.Close();
                UdpClient.Close();
                return;
            }
            LocalIdentifier = pck.ReadUInt();
            Log.Write($"Server acknowledged connection with identifier {LocalIdentifier}");

            pck = new(PacketID.CS_RequestRegistration);
            pck.AddData(username);
            Send(pck);
            Log.Write($"Sent registration request to server with username {username}");

            pck = Read();
            
            if(pck.PacketId == PacketID.SC_Kick) {
                Log.Write($"Server kicked client for reason {(KickReason)pck.ReadUInt()}");
                TcpClient.Close();
                UdpClient.Close();
                return;
            } else if (pck.PacketId != PacketID.SC_ResponseRegistration) {
                Log.Write($"Server sent weird packet {pck.PacketId} on registration");
                TcpClient.Close();
                UdpClient.Close();
                return;
            }

            string[] playerStrings = pck.ReadString().Split('|');
            foreach(string playerString in playerStrings) {
                string[] playerData = playerString.Split('@');
                uint identifier = uint.Parse(playerData[0]);
                string playerUsername = playerData[1];
                NetworkPlayer player = new(identifier, playerUsername);
                Players.Add(player);
            }
            Log.Write($"Server response OK, Synced {Players.Count} players");   

            Log.Write($"[TCP] Starting network read thread");
            Thread readThreadTCP = new(NetworkReadThreadTCP);
            readThreadTCP.Start();

            Log.Write($"[UDP] Starting network read thread");
            Thread readThreadUDP = new(NetworkReadThreadUDP);
            readThreadUDP.Start();

            Log.Write($"Network read threads started");
        }
        private void NetworkReadThreadTCP() {
            try {
                while (TcpClient.Connected) {
                    Packet packet = Read();
                    if (!_router.TryHandle(packet, null)) {
                        Log.Error($"[TCP] Packet {packet.PacketId} sent by server does not have a handler");
                    }
                }
            } catch (Exception ex) {
                Log.Error($"[TCP] Exception while reading packet from server: {ex}");
            } finally {
                TcpClient.Close();
                UdpClient.Close();
            }
        }
        private void NetworkReadThreadUDP() {
            try {
                while (true) {
                    // Packet packet = Read();
                    Packet packet = Read(ProtocolType.Udp);
                    if (!_router.TryHandle(packet, null)) {
                        Log.Error($"[UDP] Packet {packet.PacketId} sent by server does not have a handler");
                    }
                }
            } catch (Exception ex) {
                Log.Error($"[UDP] Exception while reading packet from server: {ex}");
            } finally {
                TcpClient.Close();
                UdpClient.Close();
            }
        }
        public Packet Read(ProtocolType protocol = ProtocolType.Tcp) {
            if (Vikinet.IsServerInstance) {
                throw new Exception("Server instance cannot read packets from NetClient container");
            }
            if (protocol == ProtocolType.Tcp) {
                Packet pck = Packet.FromStream(_stream);

                Log.Debug($"[TCP] Received packet {pck.PacketId} from server");
                return pck;
            }else if(protocol == ProtocolType.Udp) {
                IPEndPoint? iep = new(IPAddress.Any, 0);
                Packet pck = Packet.FromUDP(UdpClient, ref iep);
                if (!iep!.Equals(_serverEndpoint)) {
                    Log.Error($"[UDP] Received transmission from unknown source");
                    return new(PacketID.Invalid);
                }
                Log.Debug($"[UDP] Received packet {pck.PacketId} from server");
                return pck;
            } else {
                throw new InvalidOperationException($"Invalid protocol provided {protocol}");
            }
        }
        public void Send(Packet pck, ProtocolType protocol = ProtocolType.Tcp) {
            if (Vikinet.IsServerInstance) {
                throw new Exception("Server instance cannot send packets from NetClient container");
            }
            if (protocol == ProtocolType.Tcp) {
                _stream.Write(pck.GetBytes());
                Log.Debug($"[TCP] Sent packet {pck.PacketId} to server");
            } else if (protocol == ProtocolType.Udp) {
                UdpClient.Send(pck.GetBytes());
                Log.Debug($"[UDP] Sent packet {pck.PacketId} to server");
            } else {
                throw new InvalidOperationException($"Invalid protocol provided {protocol}");
            }
        }
    }
}
