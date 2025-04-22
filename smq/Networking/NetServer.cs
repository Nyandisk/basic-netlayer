using System.Net;
using System.Net.Sockets;

namespace Vikinet2.Networking {
    public class NetServer(ushort port, uint maxConnections) {
        /// <summary>
        /// List of players connected to the server
        /// </summary>
        public List<NetworkPlayer> Players { get; } = new();
        public UdpClient ServerUdp { get; } = new(port) { DontFragment = true };
        public uint MaxConnections { get; set; } = maxConnections;

        private readonly PacketRouter _router = new();
        /// <summary>
        /// PacketRouter instance, used to route packets to handlers
        /// Feel free to add your own handlers by calling Register
        /// </summary>
        public PacketRouter Router => _router;
        private readonly TcpListener _listener = new(System.Net.IPAddress.Any, port);
        private uint _nextPlayerIdentifier = 0;

        /// <summary>
        /// Attempt to get a player by their TcpClient
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public NetworkPlayer? GetPlayer(TcpClient client) {
            return Players.Where(x => x.TcpClient == client).FirstOrDefault();
        }
        /// <summary>
        /// Helper function to remove a player from the server and notify all other players
        /// </summary>
        /// <param name="player">Player to remove</param>
        public void RemovePlayer(NetworkPlayer player) {
            Log.Write($"Removing player {player.Identifier} ({player.Username}) from server");

            Packet notification = new(PacketID.SC_NotifyPlayerLeft);
            notification.AddData(player.Identifier);

            Players.Remove(player);
            SendToAll(notification);
        }
        /// <summary>
        /// Helper function to add a player to the server and notify all other players
        /// </summary>
        /// <param name="player">Player to add</param>
        public void AddPlayer(NetworkPlayer player) {
            Log.Write($"Adding player {player.Identifier} ({player.Username}) to server");

            Packet notification = new(PacketID.SC_NotifyPlayerJoined);
            notification.AddData(player.Identifier);
            notification.AddData(player.Username);

            SendToAll(notification);
            Players.Add(player);
        }
        /// <summary>
        /// Attempt to get a player by their IPEndPoint, useful for UDP
        /// </summary>
        /// <param name="iep"></param>
        /// <returns></returns>
        public NetworkPlayer? GetPlayer(IPEndPoint iep) {
            try {
                return Players.Where(x => x.UdpEndpoint!.Equals(iep)).FirstOrDefault();
            } catch {
                return null;
            }
        }
        /// <summary>
        /// Kicks a player from the server and closes their connection. Before doing so, it sends a kick packet to the player for info.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        /// <param name="removeFromPlayers">Whether to remove from player list or not, as this may be called during initialization</param>
        public void Kick(NetworkPlayer player, KickReason reason, bool removeFromPlayers = true) {
            Log.Write($"Kicking player {player.Identifier} ({player.Username}) for reason: {reason}");

            Packet packet = new(PacketID.SC_Kick);
            packet.AddData((uint)reason);
            player.Send(packet);

            player.TcpClient.Close();
            if (removeFromPlayers) RemovePlayer(player);
        }
        /// <summary>
        /// Sends a packet to all players connected to the server
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="protocol"></param>
        public void SendToAll(Packet packet, ProtocolType protocol = ProtocolType.Tcp) {
            Log.Write($"Sending packet {packet.PacketId} to all players");
            foreach(NetworkPlayer player in Players) {
                player.Send(packet, protocol, this);
            }
        }
        /// <summary>
        /// Accepts a player connection and handles their registration, also continues to read packets from them
        /// </summary>
        /// <param name="player"></param>
        private void AcceptPlayer(NetworkPlayer player) {
            Packet pck = new(PacketID.Acknowledge);
            pck.AddData(player.Identifier);
            player.Send(pck);
            Log.Write($"Player {player.Identifier} ({player.Username}) acknowledged");

            pck = player.Read();
            if (pck.PacketId != PacketID.CS_RequestRegistration) {
                Log.Write($"Invalid packet received from player {player.Identifier} ({player.Username})");
                Kick(player, KickReason.QuestionableActivity, false);
                return;
            }

            player.Username = pck.ReadString();
            if (player.Username.Contains('|') || player.Username.Length < 3 || player.Username.Length > 20) {
                Log.Write($"Invalid username received from player {player.Identifier} ({player.Username})");
                Kick(player, KickReason.QuestionableActivity, false);
                return;
            }
            AddPlayer(player);

            pck = new(PacketID.SC_ResponseRegistration);
            pck.AddData(string.Join("|",Players.Select(x => x.Identifier + "@" + x.Username)));
            player.Send(pck);

            Log.Write($"Player {player.Identifier} ({player.Username}) registered successfully");
            try {
                while (player.TcpClient.Connected) {
                    Packet packet = player.Read();
                    if (!_router.TryHandle(packet, player)) {
                        Log.Error($"[TCP] Packet {packet.PacketId} sent by {player.Identifier} ({player.Username}) does not have a handler");
                    }
                }
            }catch(Exception ex) {
                Log.Error($"[TCP] Exception while reading packet from player {player.Identifier} ({player.Username}): {ex}");
            } finally {
                RemovePlayer(player);
            }
        }
        /// <summary>
        /// UDP discovery queue, used to store IPEndPoints of players who sent discovery packets
        /// </summary>
        private readonly Queue<IPEndPoint> _discoveryQueue = new();
        /// <summary>
        /// Thread that listens for UDP packets and handles them, since UDP is connectionless
        /// </summary>
        private void UdpReceiveThread() {
            while (true) {
                IPEndPoint? iep = new(IPAddress.Any, 0);
                Packet packet;
                try {
                    packet = Packet.FromUDP(ServerUdp, ref iep);
                } catch (Exception ex) {
                    Log.Debug($"[UDP] Error parsing packet from {iep}: {ex}");
                    continue;
                }
                NetworkPlayer? player = GetPlayer(iep!);
                if (player == null) {
                    if (packet.PacketId == PacketID.CS_Discovery) {
                        Log.Debug($"[UDP] Received discovery packet from {iep!.Address}:{iep!.Port}");
                        Packet response = new(PacketID.SC_RespondDiscovery);
                        ServerUdp.Send(response.GetBytes(), iep!);

                        _discoveryQueue.Enqueue(iep);
                        continue;
                    }
                    Log.Debug($"[UDP] Received packet {packet.PacketId} from unknown player {iep!.Address}:{iep!.Port}");
                    continue;
                }
                Log.Debug($"[UDP] Received packet {packet.PacketId} from {iep!.Address}:{iep!.Port}");
                if (_router.TryHandle(packet, player) != true) {
                    Log.Error($"[UDP] Packet {packet.PacketId} sent by {player.Identifier} ({player.Username}) does not have a handler");
                }
            }
        }
        /// <summary>
        /// Starts the server and begins listening for connections
        /// </summary>
        /// <exception cref="Exception">Thrown if called from client</exception>
        public void Start() {
            if (Vikinet.IsClientInstance) {
                throw new Exception("Cannot start server on client instance");
            }

            _listener.Start();
            Thread udpThread = new(UdpReceiveThread);
            udpThread.Start();

            Log.Write($"Server started");
            while (true) {
                _nextPlayerIdentifier++;
                TcpClient tcpClient = _listener.AcceptTcpClient();
                Log.Write($"Accepted connection from {tcpClient.Client.RemoteEndPoint}. Awaiting UDP discovery...");

                while (_discoveryQueue.Count == 0) {
                    Thread.Sleep(50);
                }
                IPEndPoint discoveryIep = _discoveryQueue.Dequeue();
                NetworkPlayer player = new(_nextPlayerIdentifier, Guid.NewGuid().ToString(), tcpClient, discoveryIep);

                Log.Write($"Created temporary NetworkPlayer for client {player.Identifier} ({player.Username})");

                if (Players.Count >= MaxConnections) {
                    Log.Write("Max connections reached. Rejecting client.");
                    Kick(player, KickReason.ServerFull, false);
                    continue;
                }
                Thread thread = new(() => { AcceptPlayer(player); });
                Log.Write($"Handing client off to thread {thread.ManagedThreadId}");
                thread.Start();
            }
        }
    }
}
