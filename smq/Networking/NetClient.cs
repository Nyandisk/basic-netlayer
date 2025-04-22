using System.Net;
using System.Net.Sockets;

namespace smq.Networking {
    public class NetClient {
        public List<NetworkPlayer> Players { get; } = new();
        public uint LocalIdentifier { get; private set; } = 0;
        public string LocalUsername { get; private set; } = string.Empty;
        public TcpClient TcpClient { get; }
        public UdpClient UdpClient { get; }

        private NetworkStream _stream;
        private readonly PacketRouter _router = new();

        public NetClient(string username, string ip, ushort port) {
            if (!Program.IsClientInstance) {
                throw new Exception("Only accessible from client instance");
            }

            LocalUsername = username;
            TcpClient = new(ip, port) { NoDelay = true };
            UdpClient = new(ip, port) { DontFragment = true };
            
            _stream = TcpClient.GetStream();

            if (string.IsNullOrEmpty(username) || username.Length < 3 || username.Length > 20) {
                Console.WriteLine($"Invalid username {username} (must be between 3 and 20 characters long)");
                TcpClient.Close();
                UdpClient.Close();
                return;
            }

            _router.RegisterHandlers();
            Console.WriteLine($"Connected to server at {ip}:{port} with username {username}");

            Send(new(PacketID.CS_Discovery), ProtocolType.Udp);
            Packet pck = Read(ProtocolType.Udp);
            if (pck.PacketId != PacketID.SC_RespondDiscovery) {
                Console.WriteLine($"Server sent weird packet {pck.PacketId} on discovery");
                TcpClient.Close();
                UdpClient.Close();
                return;
            }
            Console.WriteLine($"Server acknowledged UDP discovery");

            pck = Read();
            if (pck.PacketId == PacketID.SC_Kick) {
                Console.WriteLine($"Server kicked client for reason {(KickReason)pck.ReadUInt()}");
                TcpClient.Close();
                UdpClient.Close();
                return;
            } else if (pck.PacketId != PacketID.Acknowledge) {
                Console.WriteLine($"Server sent weird packet {pck.PacketId} on connection");
                TcpClient.Close();
                UdpClient.Close();
                return;
            }
            LocalIdentifier = pck.ReadUInt();
            Console.WriteLine($"Server acknowledged connection with identifier {LocalIdentifier}");

            pck = new(PacketID.CS_RequestRegistration);
            pck.AddData(username);
            Send(pck);
            Console.WriteLine($"Sent registration request to server with username {username}");

            pck = Read();
            
            if(pck.PacketId == PacketID.SC_Kick) {
                Console.WriteLine($"Server kicked client for reason {(KickReason)pck.ReadUInt()}");
                TcpClient.Close();
                UdpClient.Close();
                return;
            } else if (pck.PacketId != PacketID.SC_ResponseRegistration) {
                Console.WriteLine($"Server sent weird packet {pck.PacketId} on registration");
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
            Console.WriteLine($"Server response OK, Synced {Players.Count} players");   

            Console.WriteLine($"[TCP] Starting network read thread");
            Thread readThreadTCP = new(NetworkReadThreadTCP);
            readThreadTCP.Start();

            Console.WriteLine($"[UDP] Starting network read thread");
            Thread readThreadUDP = new(NetworkReadThreadUDP);
            readThreadUDP.Start();

            Console.WriteLine($"Network read threads started");
        }
        private void NetworkReadThreadTCP() {
            try {
                while (TcpClient.Connected) {
                    Packet packet = Read();
                    if (!_router.TryHandle(packet, null)) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[TCP] Packet {packet.PacketId} sent by server does not have a handler");
                        Console.ResetColor();
                    }
                }
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[TCP] Exception while reading packet from server: {ex}");
                Console.ResetColor();
            } finally {
                TcpClient.Close();
                UdpClient.Close();
            }
        }
        private void NetworkReadThreadUDP() {
            try {
                while (true) {
                    Packet packet = Read();
                    if (!_router.TryHandle(packet, null)) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[UDP] Packet {packet.PacketId} sent by server does not have a handler");
                        Console.ResetColor();
                    }
                }
            } catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[UDP] Exception while reading packet from server: {ex}");
                Console.ResetColor();
            } finally {
                TcpClient.Close();
                UdpClient.Close();
            }
        }
        public Packet Read(ProtocolType protocol = ProtocolType.Tcp) {
            if (Program.IsServerInstance) {
                throw new Exception("Server instance cannot read packets from NetClient container");
            }
            if (protocol == ProtocolType.Tcp) {
                Packet pck = Packet.FromStream(_stream);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[TCP] Received packet {pck.PacketId} from server");
                Console.ResetColor();
                return pck;
            }else if(protocol == ProtocolType.Udp) {
                IPEndPoint? iep = new IPEndPoint(IPAddress.Any, 0);
                Packet pck = Packet.FromUDP(UdpClient, ref iep);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[UDP] Received packet {pck.PacketId} from server");
                Console.ResetColor();
                return pck;
            } else {
                throw new InvalidOperationException($"Invalid protocol provided {protocol}");
            }
        }
        public void Send(Packet pck, ProtocolType protocol = ProtocolType.Tcp) {
            if (Program.IsServerInstance) {
                throw new Exception("Server instance cannot send packets from NetClient container");
            }
            if (protocol == ProtocolType.Tcp) {
                _stream.Write(pck.GetBytes());
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[TCP] Sent packet {pck.PacketId} to server");
                Console.ResetColor();
            } else if (protocol == ProtocolType.Udp) {
                UdpClient.Send(pck.GetBytes());
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"[UDP] Sent packet {pck.PacketId} to server");
                Console.ResetColor();
            } else {
                throw new InvalidOperationException($"Invalid protocol provided {protocol}");
            }
        }
    }
}
