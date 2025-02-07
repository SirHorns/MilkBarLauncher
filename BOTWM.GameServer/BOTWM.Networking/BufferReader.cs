using System.Text;
using BOTWM.Library.DataTypes;
using BOTWM.Library.DTO;
using Newtonsoft.Json;

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

    public List<object> ReadList(Type original)
    {
        int listSize = ReadByte();

        var result = new List<object>();

        for (var i = 0; i < listSize; i++)
        {
            result.Add(ReadValue(original.GetGenericArguments()[0]));
        }

        return result;
    }
    
    public Dictionary<byte, ModelDataDTO> ReadModelDataDictionary()
    {
        int dictionarySize = ReadByte();

        var result = new Dictionary<byte, ModelDataDTO>();

        for (var i = 0; i < dictionarySize; i++)
        {
            var key = ReadByte();

            var modelData = new ModelDataDTO
            {
                ModelType = ReadByte()
            };

            if (modelData.ModelType < 2)
            {
                var stringSize = ReadByte();
                var model = Encoding.UTF8.GetString(ReadBytes(stringSize));
                modelData.Model = model;
                modelData.Mii = new BumiiDTO();
            }
            else
            {
                modelData.Model = "";
                var sBumii = JsonConvert.SerializeObject(ReadValue(typeof(BumiiDTO)));
                modelData.Mii = JsonConvert.DeserializeObject<BumiiDTO>(sBumii);
            }

            result.Add(key, modelData);
        }

        return result;
    }
    
    public Dictionary<object, object> ReadDictionary(Type original)
    {
        int dictionarySize = ReadByte();

        var result = new Dictionary<object, object>();

        for (var i = 0; i < dictionarySize; i++)
        {
            var key = ReadValue(original.GetGenericArguments()[0]);
            var value = ReadValue(original.GetGenericArguments()[1]);

            result.Add(key, value);
        }

        return result;
    }
    
    public Dictionary<string, object> CreateValueDictionary(Type original)
    {
        var result = new Dictionary<string, object>();

        foreach (var item in original.GetFields())
        {
            if (item.Name == "Schedule")
            {
                continue;
            }

            var res = ReadValue(item.FieldType);
            result.Add(item.Name, res);
        }

        return result;
    }

    
    
    public object ReadValue(Type original)
    {
        object value;

        if (original == typeof(int))
        {
            value = ReadInt();
        }
        else if (original == typeof(float))
        {
            value = ReadFloat();
        }
        else if (original == typeof(bool))
        {
            value = ReadBool();
        }
        else if (original == typeof(byte))
        {
            value = ReadByte();
        }
        else if (original == typeof(short))
        {
            value = ReadShort();
        }
        else if (original == typeof(string))
        {
            value = ReadString();
        }
        else if (original == typeof(Vec3f))
        {
            value = ReadVec3f();
        }
        else if (original == typeof(Quaternion))
        {
            value = ReadQuaternion();
        }
        else if (original == typeof(CharacterLocation))
        {
            value = ReadCharacterLocation();
        }
        else if (original == typeof(CharacterEquipment))
        {
            value = ReadCharacterEquipment();
        }
        else if (original.IsGenericType && typeof(System.Collections.IList).IsAssignableFrom(original))
        {
            value = ReadList(original);
        }
        else if (original == typeof(Dictionary<byte, ModelDataDTO>))
        {
            value = ReadModelDataDictionary();
        }
        else if (original.IsGenericType && typeof(System.Collections.IDictionary).IsAssignableFrom(original))
        {
            value = ReadDictionary(original);
        }
        else
        {
            value = CreateValueDictionary(original);
        }

        return value;
    }
}