using System.Text;
using BOTWM.Library.DataTypes;

namespace BOTWM.Server;

public class BufferReader
{
    public byte[] Bytes;

    public BufferReader(byte[] bytes)
    {
        Bytes = bytes;
    }
    
    public byte ReadByte()
    {

        var bytes = Bytes.Take(1).ToArray();

        Bytes = Bytes.Skip(1).ToArray();

        return bytes[0];
    }
        
    public byte[] ReadBytes(int length)
    {

        var bytes = Bytes.Take(length).ToArray();

        Bytes = Bytes.Skip(length).ToArray();

        return bytes;
    }

    public int ReadInt()
    {
        return BitConverter.ToInt32(ReadBytes(4), 0);
    }

    public float ReadFloat()
    {
        return BitConverter.ToSingle(ReadBytes(4), 0);
    }

    public bool ReadBool()
    {
        return ReadByte() != 0x0;
    }

    public short ReadShort()
    {
        return BitConverter.ToInt16(ReadBytes(2), 0);
    }

    public string ReadString()
    {
        var stringSize = ReadInt();
        var text = stringSize == 0 ? "" : Encoding.UTF8.GetString(ReadBytes(stringSize));
        return text;
    }

    public Vec3f ReadVec3f()
    {
        var result = new Vec3f
        {
            x = BitConverter.ToSingle(ReadBytes(4), 0),
            y = BitConverter.ToSingle(ReadBytes(4), 0),
            z = BitConverter.ToSingle(ReadBytes(4), 0)
        };

        return result;
    }

    public Quaternion ReadQuaternion()
    {
        var result = new Quaternion
        {
            q1 = BitConverter.ToSingle(ReadBytes(4), 0),
            q2 = BitConverter.ToSingle(ReadBytes(4), 0),
            q3 = BitConverter.ToSingle(ReadBytes(4), 0),
            q4 = BitConverter.ToSingle(ReadBytes(4), 0)
        };

        return result;
    }

    public CharacterLocation ReadCharacterLocation()
    {
        var result = new CharacterLocation
        {
            Map = ReadByte(),
            Section = ReadByte()
        };

        return result;
    }

    public CharacterEquipment ReadCharacterEquipment()
    {
        var result = new CharacterEquipment
        {
            WType = ReadByte(),
            Sword = BitConverter.ToInt16(ReadBytes(2), 0),
            Shield = BitConverter.ToInt16(ReadBytes(2), 0),
            Bow = BitConverter.ToInt16(ReadBytes(2), 0),
            Head = BitConverter.ToInt16(ReadBytes(2), 0),
            Upper = BitConverter.ToInt16(ReadBytes(2), 0),
            Lower = BitConverter.ToInt16(ReadBytes(2), 0)
        };

        return result;
    }
}