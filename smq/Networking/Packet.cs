using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace smq.Networking {
    public enum PacketID {
        Invalid = 0x0000,
        Acknowledge = 0x0006,

        CS_RequestRegistration = 0x0001,
        CS_Discovery = 0x0007,

        SC_RespondDiscovery = 0x0008,
        SC_ResponseRegistration = 0x0002,
        SC_Kick = 0x0003,
        SC_NotifyPlayerLeft = 0x0004,
        SC_NotifyPlayerJoined = 0x0005
    }
    public class Packet(PacketID packetId) {
        public PacketID PacketId { get; set; } = packetId;
        private readonly List<byte> _data = new();
        private int _offset = 0;
        private const byte MAGIC_1 = 0x5A;
        private const byte MAGIC_2 = 0xC3;
        public Packet(PacketID packetId, string data) : this(packetId) {
            AddData(data);
        }
        public Packet(PacketID packetId, byte[] data) : this(packetId) {
            _data.AddRange(data);
        }
        public void AddData(string value) {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            _data.AddRange(BitConverter.GetBytes((ushort)bytes.Length));
            _data.AddRange(bytes);
        }
        public void AddData(int value) => _data.AddRange(BitConverter.GetBytes(value));
        public void AddData(uint value) => _data.AddRange(BitConverter.GetBytes(value));
        public void AddData(float value) => _data.AddRange(BitConverter.GetBytes(value));
        public void AddData(double value) => _data.AddRange(BitConverter.GetBytes(value));
        public void AddData(bool value) => _data.Add((byte)(value ? 1 : 0));
        public void AddData(byte value) => _data.Add(value);

        public string ReadString() {
            ushort length = BitConverter.ToUInt16(_data.ToArray(), _offset);
            _offset += sizeof(ushort);
            string value = Encoding.UTF8.GetString(_data.ToArray(), _offset, length);
            _offset += length;
            return value;
        }
        public int ReadInt() {
            int value = BitConverter.ToInt32(_data.ToArray(), _offset);
            _offset += sizeof(int);
            return value;
        }
        public uint ReadUInt() {
            uint value = BitConverter.ToUInt32(_data.ToArray(), _offset);
            _offset += sizeof(uint);
            return value;
        }
        public float ReadFloat() {
            float value = BitConverter.ToSingle(_data.ToArray(), _offset);
            _offset += sizeof(float);
            return value;
        }
        public double ReadDouble() {
            double value = BitConverter.ToDouble(_data.ToArray(), _offset);
            _offset += sizeof(double);
            return value;
        }
        public bool ReadBool() {
            bool value = _data[_offset] == 1;
            _offset += sizeof(byte);
            return value;
        }
        public byte ReadByte() {
            byte value = _data[_offset];
            _offset += sizeof(byte);
            return value;
        }
        public void Reset() {
            _offset = 0;
        }
        private ReadOnlySpan<byte> GetRawData() {
            byte[] raw = new byte[sizeof(ushort) + _data.Count];
            BitConverter.GetBytes((ushort)PacketId).CopyTo(raw, 0); 
            _data.CopyTo(raw.AsSpan(sizeof(ushort)));
            return raw;
        }
        public byte[] GetBytes() {
            byte[] raw = GetRawData().ToArray();
            byte[] packet = new byte[2 + sizeof(ushort) + raw.Length];
            packet[0] = MAGIC_1;
            packet[1] = MAGIC_2;
            BitConverter.GetBytes((ushort)raw.Length).CopyTo(packet, 2);
            raw.CopyTo(packet, 4);
            return packet;
        }
        public static Packet FromStream(NetworkStream stream) {
            Span<byte> header = stackalloc byte[4];
            stream.ReadExactly(header);

            if (header[0] != MAGIC_1 || header[1] != MAGIC_2) {
                throw new InvalidDataException("[TCP] Magic bytes mismatch");
            }

            ushort packetSize = BitConverter.ToUInt16(header.Slice(2, 2));
            if (packetSize < 2) {
                throw new InvalidDataException("[TCP] Invalid packet size");
            }

            byte[] raw = new byte[packetSize];
            stream.ReadExactly(raw);

            PacketID packetId = (PacketID)BitConverter.ToUInt16(raw, 0);
            byte[] payload = new byte[packetSize - 2];
            Buffer.BlockCopy(raw, 2, payload, 0, payload.Length);

            return new Packet(packetId, payload);
        }

        public static Packet FromUDP(UdpClient client, ref IPEndPoint? riep) {
            IPEndPoint iep = new(IPAddress.Any, 0);
            byte[] received = client.Receive(ref iep);

            if (received.Length < 6)
                throw new InvalidDataException("[UDP] Packet too short");

            if (received[0] != MAGIC_1 || received[1] != MAGIC_2)
                throw new InvalidDataException("[UDP] Magic bytes mismatch");

            ushort packetSize = BitConverter.ToUInt16(received, 2);
            if (packetSize + 4 != received.Length)
                throw new InvalidDataException("[UDP] Packet size mismatch");

            byte[] raw = received[4..];
            PacketID packetId = (PacketID)BitConverter.ToUInt16(raw, 0);
            byte[] payload = new byte[packetSize - 2];
            Buffer.BlockCopy(raw, 2, payload, 0, payload.Length);

            riep = iep;
            return new Packet(packetId, payload);
        }
    }
}