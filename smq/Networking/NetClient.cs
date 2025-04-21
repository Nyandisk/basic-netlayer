using System.Net.Sockets;

namespace smq.Networking {
    public class NetClient {
        public List<NetworkPlayer> Players { get; } = new();
        public uint LocalIdentifier { get; private set; } = 0;
        public string LocalUsername { get; private set; } = string.Empty;
        public TcpClient Client { get; }
        private NetworkStream _stream;
        private readonly PacketRouter _router = new();

        public NetClient(string username, string ip, ushort port) {
            if (!Program.IsClientInstance) {
                throw new Exception("Only accessible from client instance");
            }

            LocalUsername = username;
            Client = new(ip, port) { NoDelay = true };
            _stream = Client.GetStream();

            if (string.IsNullOrEmpty(username) || username.Length < 3 || username.Length > 20) {
                Console.WriteLine($"Invalid username {username} (must be between 3 and 20 characters long)");
                Client.Close();
                return;
            }

            _router.RegisterHandlers();
            Console.WriteLine($"Connected to server at {ip}:{port} with username {username}");

            Packet pck = Read();
            if (pck.PacketId != PacketID.Acknowledge) {
                Console.WriteLine($"Server sent weird packet {pck.PacketId} on connection");
                Client.Close();
                return;
            } else if (pck.PacketId == PacketID.SC_Kick) {
                Console.WriteLine($"Server kicked client for reason {(KickReason)pck.ReadUInt()}");
                Client.Close();
                return;
            }
            LocalIdentifier = pck.ReadUInt();
            Console.WriteLine($"Server acknowledged connection with identifier {LocalIdentifier}");

            pck = new(PacketID.CS_RequestRegistration);
            pck.AddData(username);
            Send(pck);
            Console.WriteLine($"Sent registration request to server with username {username}");

            pck = Read();
            if (pck.PacketId != PacketID.SC_ResponseRegistration) {
                Console.WriteLine($"Server sent weird packet {pck.PacketId} on registration");
                Client.Close();
                return;
            }else if(pck.PacketId == PacketID.SC_Kick) {
                Console.WriteLine($"Server kicked client for reason {(KickReason)pck.ReadUInt()}");
                Client.Close();
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
            Console.WriteLine($"Starting network read thread");
            Thread readThread = new(NetworkReadThread);
            readThread.Start();
        }
        private void NetworkReadThread() {
            try {
                while (Client.Connected) {
                    Packet packet = Read();
                    if (!_router.TryHandle(packet, null)) {
                        Console.WriteLine($"Packet {packet.PacketId} sent by server does not have a handler");
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine($"Exception while reading packet from server: {ex}");
            } finally {
                Client.Close();
            }
        }
        public Packet Read() {
            if (Program.IsServerInstance) {
                throw new Exception("Server instance cannot read packets from NetClient container");
            }
            Packet pck = Packet.FromStream(_stream);
            Console.WriteLine($"Received packet {pck.PacketId} from server");
            return pck;
        }
        public void Send(Packet pck) {
            if (Program.IsServerInstance) {
                throw new Exception("Server instance cannot send packets from NetClient container");
            }
            _stream.Write(pck.GetBytes());
            Console.WriteLine($"Sent packet {pck.PacketId} to server");
        }
    }
}
