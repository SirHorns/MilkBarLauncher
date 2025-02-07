using System.Text;

namespace BOTWM.Server.Packets;

public class DisconnectPacket: BasePacket
{
    public string Reason { get; set; } = string.Empty;
    public DisconnectPacket()
    {
    }

    public DisconnectPacket(byte[] bytes) : base(bytes)
    {
    }

    public override void ReadBody(BufferReader reader)
    {
       Reason = Encoding.UTF8.GetString(reader.Bytes).Replace("\0", "");
    }
}