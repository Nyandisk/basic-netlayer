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
        public void AddData(int value) {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        public void AddData(uint value) {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        public void AddData(float value) {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        public void AddData(double value) {
            _data.AddRange(BitConverter.GetBytes(value));
        }
        public void AddData(bool value) {
            _data.Add((byte)(value ? 1 : 0));
        }
        public void AddData(byte value) {
            _data.Add(value);
        }

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
            byte[] packet = new byte[sizeof(ushort) + raw.Length];
            BitConverter.GetBytes((ushort)raw.Length).CopyTo(packet, 0);
            raw.CopyTo(packet, 2);
            return packet;
        }
        public static Packet FromStream(NetworkStream stream) {
            Span<byte> bytes = stackalloc byte[2];
            stream.ReadExactly(bytes);

            ushort packetSize = BitConverter.ToUInt16(bytes);
            byte[] raw = new byte[packetSize];
            stream.ReadExactly(raw);

            PacketID packetId = (PacketID)BitConverter.ToUInt16(raw, 0);

            byte[] payload = new byte[packetSize - 2];
            Buffer.BlockCopy(raw, 2, payload, 0, packetSize - 2);


            return new Packet(packetId, payload);
        }
        public static Packet FromUDP(UdpClient client, ref IPEndPoint? riep) {
            IPEndPoint iep = new(IPAddress.Any, 0);
            Span<byte> bytes = client.Receive(ref iep).AsSpan();

            ushort packetSize = BitConverter.ToUInt16(bytes[..2]);
            byte[] raw = bytes[2..].ToArray();

            PacketID packetId = (PacketID)BitConverter.ToUInt16(raw, 0);

            byte[] payload = new byte[packetSize - 2];
            Buffer.BlockCopy(raw, 2, payload, 0, packetSize - 2);

            riep = iep;
            return new Packet(packetId, payload);
        }
    }
}