namespace Vikinet2.Networking {
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerPacketHandlerAttribute(PacketID packetId) : Attribute {
        public PacketID PacketId { get; } = packetId;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientPacketHandlerAttribute(PacketID packetId) : Attribute {
        public PacketID PacketId { get; } = packetId;
    }
    /// <summary>
    /// Packet router for handling packets
    /// </summary>
    public class PacketRouter {
        private readonly Dictionary<PacketID, Action<Packet, NetworkPlayer?>> _handlers = new();
        /// <summary>
        /// Register a packet handler method for a specific packet ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="handler"></param>
        /// <exception cref="Exception">Thrown if already registered</exception>
        public void Register(PacketID id, Action<Packet, NetworkPlayer?> handler) {
            if (_handlers.ContainsKey(id)) {
                throw new Exception($"Handler for packet {id} already registered");
            }
            _handlers.Add(id, handler);
        }
        public bool TryHandle(Packet packet, NetworkPlayer? player) {
            if (_handlers.TryGetValue(packet.PacketId, out var handler)) {
                handler.Invoke(packet, player);
                return true;
            }
            return false;
        }
    }
}
