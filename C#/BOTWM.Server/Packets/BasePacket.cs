using BOTWM.Library.JSONBuilder;
using BOTWM.Server.Packets;

namespace BOTWM.Server;

public class BasePacket
{
    public MessageTypes MessageType { get; set; }
    public int Length { get; set; }
    public byte[] RawBytes { get; set; }

    public BasePacket() {}
    
    public BasePacket(byte[] bytes)
    {
        RawBytes = bytes;
        Read(bytes);
    }

    public void Read(byte[] rawBytes)
    {
        Length = rawBytes.Length;
        RawBytes = rawBytes;
        var reader = new BufferReader(rawBytes);
        ReadHeader(reader);
        ReadBody(reader);
    }

    public virtual void ReadHeader(BufferReader reader)
    {
        MessageType = (MessageTypes)reader.ReadByte();
    }
    
    public virtual void ReadBody(BufferReader reader) { }

    public static BasePacket? Create(byte[] rawBytes)
    {
        var type = (MessageTypes)PacketBytesIo.ReadByte(rawBytes);
        BasePacket? packet = null;

        switch (type)
        {
            case MessageTypes.Ping:
                packet = new PingPacket(rawBytes);
                break;
            case MessageTypes.Connect:
                packet = new ConnectPacket(rawBytes);
                break;
            case MessageTypes.Update:
                break;
            case MessageTypes.Disconnect:
                packet = new DisconnectPacket(rawBytes);
                break;
            case MessageTypes.Error:
            default: 
                break;
        }
        return packet;
    }
}