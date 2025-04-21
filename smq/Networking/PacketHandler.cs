namespace smq.Networking {
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerPacketHandlerAttribute(PacketID packetId) : Attribute {
        public PacketID PacketId { get; } = packetId;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientPacketHandlerAttribute(PacketID packetId) : Attribute {
        public PacketID PacketId { get; } = packetId;
    }
    public class PacketRouter {
        private readonly Dictionary<PacketID, Action<Packet, NetworkPlayer?>> _handlers = new();
        public void RegisterHandlers() {
            Console.WriteLine($"Registering handlers");
            if (Program.IsServerInstance) {
                //Register();
            } else {
                //Register();
            }
            Console.WriteLine($"Registered {_handlers.Count} handlers for {(Program.IsServerInstance ? "server" : "client")}");
        }
        private void Register(PacketID id, Action<Packet, NetworkPlayer?> handler) {
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
