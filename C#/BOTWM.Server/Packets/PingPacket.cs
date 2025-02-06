using System.Text;

namespace BOTWM.Server.Packets;

public class PingPacket: BasePacket
{
    public string Password { get; set; }
    
    public PingPacket() { }

    public PingPacket(byte[] bytes) : base(bytes) { }

    public override void ReadBody(BufferReader reader)
    {
        Password = Encoding.UTF8.GetString(reader.Bytes).Replace("\0", "");
    }
}