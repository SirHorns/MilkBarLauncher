namespace BOTWM.Server;

public class PacketBytesIo
{
    public static byte ReadByte(byte[] bytes)
    {
        var res = bytes.Take(1).ToArray();

        return res[0];
    }
    
    public static byte[] ReadBytes(byte[] bytes, int length)
    {
        var res = bytes.Take(length).ToArray();

        return res;
    }
}