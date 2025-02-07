using System.Text;
using BOTWM.Library.DataTypes;
using BOTWM.Library.DTO;
using Newtonsoft.Json;

namespace BOTWM.Networking;

public class BufferWriter
{
    List<byte> Buffer;

    public BufferWriter()
    {
        Buffer = [];
    }

    public byte[] Write(object original, bool debug = false)
    {
        GetBuffer(original);

        if(!debug)
        {
            Buffer.InsertRange(0, BitConverter.GetBytes((short)Buffer.Count));
        }

        return Buffer.ToArray();
    }
    
    private void GetBuffer(object original)
        {
            var type = original.GetType();
            
            if (type == typeof(int))
            {
                WriteBytes(BitConverter.GetBytes((int)original));
            }
            else if (type == typeof(float))
            {
                WriteBytes(BitConverter.GetBytes((float)original));
            }
            else if (type == typeof(bool))
            {
                WriteByte((bool)original ? (byte)1 : (byte)0);
            }
            else if (type == typeof(byte))
            {
                WriteByte((byte)original);
            }
            else if (type == typeof(short))
            {
                WriteBytes(BitConverter.GetBytes((short)original));
            }
            else if (type == typeof(string))
            { 
                WriteByte((byte)((string)original).Length);
                WriteBytes(Encoding.UTF8.GetBytes((string)original));
            }
            else if(type == typeof(ConnectDTO))
            {
                var origDTO = (ConnectDTO)original;

                WriteByte((byte)((string)origDTO.Name).Length);
                WriteBytes(Encoding.UTF8.GetBytes((string)origDTO.Name));

                WriteByte((byte)((string)origDTO.Password).Length);
                WriteBytes(Encoding.UTF8.GetBytes((string)origDTO.Password));

                var modelType = origDTO.ModelData.ModelType.ToString();

                
                WriteByte((byte)modelType.Length);
                WriteBytes(Encoding.UTF8.GetBytes(modelType));

                if (origDTO.ModelData.ModelType < 2)
                { 
                    WriteBytes(BitConverter.GetBytes((short)origDTO.ModelData.Model.Length));
                    WriteBytes(Encoding.UTF8.GetBytes((string)origDTO.ModelData.Model));
                }
                else
                {
                    var miiData = JsonConvert.SerializeObject(origDTO.ModelData.Mii);
                    WriteBytes(BitConverter.GetBytes((short)miiData.Length));
                    WriteBytes(Encoding.UTF8.GetBytes(miiData));
                }
            }
            else if(type == typeof(ModelDataDTO))
            {
                var dtoObj = (ModelDataDTO)original;
                WriteByte(dtoObj.ModelType);

                if(dtoObj.ModelType < 2)
                {
                    WriteBytes(BitConverter.GetBytes((dtoObj.Model).Length));
                    WriteBytes(Encoding.UTF8.GetBytes(dtoObj.Model));
                }
                else
                {
                    GetBuffer(dtoObj.Mii);
                }
            }
            else if (type == typeof(Vec3f))
            {
                var originalVec3F = (Vec3f)original;

                WriteBytes(BitConverter.GetBytes(originalVec3F.x));
                WriteBytes(BitConverter.GetBytes(originalVec3F.y));
                WriteBytes(BitConverter.GetBytes(originalVec3F.z));
            }
            else if(type == typeof(Quaternion))
            {
                var originalQuaternion = (Quaternion)original;

                WriteBytes(BitConverter.GetBytes(originalQuaternion.q1));
                WriteBytes(BitConverter.GetBytes(originalQuaternion.q2));
                WriteBytes(BitConverter.GetBytes(originalQuaternion.q3));
                WriteBytes(BitConverter.GetBytes(originalQuaternion.q4));
            }
            else if (type == typeof(CharacterLocation))
            {
                var originalLocation = (CharacterLocation)original;

                WriteByte(originalLocation.Map);
                WriteByte(originalLocation.Section);
            }
            else if (type == typeof(CharacterEquipment))
            {
                var originalEquipment = (CharacterEquipment)original;

                //AddBytes(BitConverter.GetBytes(originalEquipment.WType));
                WriteByte(originalEquipment.WType);
                WriteBytes(BitConverter.GetBytes(originalEquipment.Sword));
                WriteBytes(BitConverter.GetBytes(originalEquipment.Shield));
                WriteBytes(BitConverter.GetBytes(originalEquipment.Bow));
                WriteBytes(BitConverter.GetBytes(originalEquipment.Head));
                WriteBytes(BitConverter.GetBytes(originalEquipment.Upper));
                WriteBytes(BitConverter.GetBytes(originalEquipment.Lower));
            }
            else switch (type.IsGenericType)
            {
                case true when typeof(System.Collections.IList).IsAssignableFrom(type):
                    var listMethod = typeof(BufferWriter).GetMethod("AddListData");
                    listMethod?.MakeGenericMethod(type.GenericTypeArguments).Invoke(this, [original]);
                    break;
                case true when typeof(System.Collections.IDictionary).IsAssignableFrom(type):
                    var dictionaryMethod = typeof(BufferWriter).GetMethod("AddDictData");
                    dictionaryMethod?.MakeGenericMethod(type.GenericTypeArguments).Invoke(this, [original]);
                    break;
                default:
                {
                    foreach (var item in type.GetFields())
                    {
                        var value = item.GetValue(original);
                        if (value is null)
                        {
                            continue;
                        }
                        GetBuffer(value);
                    }
                    break;
                }
            }

        }
    
    private void WriteByte(byte b)
    {
        Buffer.Add(b);
    }
    
    private void WriteBytes(byte[] bytes)
    {
        Buffer.AddRange(bytes);
    }
    
    public void AddListData<T>(object original)
    {

        var originalList = (List<T>)original;

        Buffer.Add((byte)originalList.Count);

        foreach (var t in originalList)
        {
            GetBuffer(t);
        }

    }

    public void AddDictData<K, V>(object original)
    {
        var originalDict = (Dictionary<K, V>)original;

        Buffer.Add((byte)originalDict.Count);

        for (var i = 0; i < originalDict.Count; i++)
        {
            GetBuffer(originalDict.ElementAt(i).Key);
            GetBuffer(originalDict.ElementAt(i).Value);
        }
    }
}