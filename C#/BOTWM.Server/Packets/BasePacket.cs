using BOTWM.Library.JSONBuilder;
using BOTWM.Server.Packets;

namespace BOTWM.Server;

public class BasePacket
{
    public MessageTypes MessageType { get; set; }
    public int Length { get; set; }
    public byte[] RawBytes { get; set; }

    public void Read(byte[] rawBytes)
    {
        Length = rawBytes.Length;
        RawBytes = rawBytes;
        
        ReadHeader(rawBytes);
        ReadBytes(rawBytes);
    }
    
    public virtual void ReadHeader(byte[] rawBytes)
    {
        MessageType = (MessageTypes)PacketBytesIo.ReadByte(rawBytes);
    }
    
    public virtual void ReadBytes(byte[] rawBytes)
    {
        
    }

    public static BasePacket Create(byte[] rawBytes)
    {
        var type = (MessageTypes)PacketBytesIo.ReadByte(rawBytes);
        BasePacket packet = null;

        switch (type)
        {
            case MessageTypes.error:
                break;
            case MessageTypes.ping:
                break;
            case MessageTypes.connect:
                packet = new ConnectPacket();
                packet.Read(rawBytes);
                break;
            case MessageTypes.update:
                break;
            case MessageTypes.disconnect:
                break;
            default:
                break;
        }
        return packet;
    }
}