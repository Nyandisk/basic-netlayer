using System.Net.Sockets;

namespace smq.Networking {
    public class NetServer(ushort port, uint maxConnections) {
        public List<NetworkPlayer> Players { get; } = new();
        public uint MaxConnections { get; set; } = maxConnections;

        private readonly PacketRouter _router = new();
        private readonly TcpListener _listener = new(System.Net.IPAddress.Any, port);
        private uint _nextPlayerIdentifier = 0;

        public NetworkPlayer? GetPlayer(TcpClient client) {
            return Players.Where(x => x.Client == client).FirstOrDefault();
        }
        public void RemovePlayer(NetworkPlayer player) {
            Console.WriteLine($"Removing player {player.Identifier} ({player.Username}) from server");

            Packet notification = new(PacketID.SC_NotifyPlayerLeft);
            notification.AddData(player.Identifier);

            Players.Remove(player);
            SendToAll(notification);
        }
        public void AddPlayer(NetworkPlayer player) {
            Console.WriteLine($"Adding player {player.Identifier} ({player.Username}) to server");

            Packet notification = new(PacketID.SC_NotifyPlayerJoined);
            notification.AddData(player.Identifier);
            notification.AddData(player.Username);

            SendToAll(notification);
            Players.Add(player);
        }
        public void Kick(NetworkPlayer player, KickReason reason, bool removeFromPlayers = true) {
            Console.WriteLine($"Kicking player {player.Identifier} ({player.Username}) for reason: {reason}");

            Packet packet = new(PacketID.SC_Kick);
            packet.AddData((uint)reason);
            player.Send(packet);

            player.Client.Close();
            if (removeFromPlayers) RemovePlayer(player);
        }
        public void SendToAll(Packet packet) {
            Console.WriteLine($"Sending packet {packet.PacketId} to all players");
            foreach(NetworkPlayer player in Players) {
                player.Send(packet);
            }
        }
        private void AcceptPlayer(NetworkPlayer player) {
            Packet pck = new(PacketID.Acknowledge);
            pck.AddData(player.Identifier);
            player.Send(pck);
            Console.WriteLine($"Player {player.Identifier} ({player.Username}) acknowledged");

            pck = player.Read();
            if (pck.PacketId != PacketID.CS_RequestRegistration) {
                Console.WriteLine($"Invalid packet received from player {player.Identifier} ({player.Username})");
                Kick(player, KickReason.QuestionableActivity, false);
                return;
            }

            player.Username = pck.ReadString();
            if (player.Username.Contains('|') || player.Username.Length < 3 || player.Username.Length > 20) {
                Console.WriteLine($"Invalid username received from player {player.Identifier} ({player.Username})");
                Kick(player, KickReason.QuestionableActivity, false);
                return;
            }
            AddPlayer(player);

            pck = new(PacketID.SC_ResponseRegistration);
            pck.AddData(string.Join("|",Players.Select(x => x.Identifier + "@" + x.Username)));
            player.Send(pck);

            Console.WriteLine($"Player {player.Identifier} ({player.Username}) registered successfully");
            try {
                while (player.Client.Connected) {
                    Packet packet = player.Read();
                    if (!_router.TryHandle(packet, player)) {
                        Console.WriteLine($"Packet {packet.PacketId} sent by {player.Identifier} ({player.Username}) does not have a handler");
                    }
                }
            }catch(Exception ex) {
                Console.WriteLine($"Exception while reading packet from player {player.Identifier} ({player.Username}): {ex}");
            } finally {
                RemovePlayer(player);
            }
        }
        public void Start() {
            if (Program.IsClientInstance) {
                throw new Exception("Cannot start server on client instance");
            }
            _router.RegisterHandlers();
            _listener.Start();
            Console.WriteLine($"Server started");
            while (true) {
                _nextPlayerIdentifier++;
                TcpClient client = _listener.AcceptTcpClient();
                NetworkPlayer player = new(_nextPlayerIdentifier, Guid.NewGuid().ToString(), client);
                Console.WriteLine($"Created temporary NetworkPlayer for client {player.Identifier} ({player.Username})");

                if (Players.Count >= MaxConnections) {
                    Console.WriteLine("Max connections reached. Rejecting client.");
                    Kick(player, KickReason.ServerFull, false);
                    continue;
                }
                Thread thread = new(() => { AcceptPlayer(player); });
                Console.WriteLine($"Handing client off to thread {thread.ManagedThreadId}");
                thread.Start();
            }
        }
    }
}
