using System.Text;
using BOTWM.Library.DTO;
using Newtonsoft.Json;

namespace BOTWM.Server.Packets;

public class ConnectPacket: BasePacket
{
    
    public ConnectDTO ConnectDTO { get; set; }
    public string Json { get; set; }

    public ConnectPacket() : base() { }
    public ConnectPacket(byte[] bytes) : base(bytes) { }
    
    public override void ReadBody(BufferReader reader)
    {
        ConnectDTO = new ConnectDTO();

        int stringSize = reader.ReadByte();
        ConnectDTO.Name = Encoding.UTF8.GetString(reader.ReadBytes(stringSize));

        stringSize = reader.ReadByte();
        ConnectDTO.Password = Encoding.UTF8.GetString(reader.ReadBytes(stringSize));

        ConnectDTO.ModelData = new ModelDataDTO();

        stringSize = reader.ReadByte();
        ConnectDTO.ModelData.ModelType = byte.Parse(Encoding.UTF8.GetString(reader.ReadBytes(stringSize)));

        stringSize = BitConverter.ToInt16(reader.ReadBytes(2), 0);
        var model = Encoding.UTF8.GetString(reader.ReadBytes(stringSize));

        if(ConnectDTO.ModelData.ModelType < 2)
        {
            ConnectDTO.ModelData.Model = model;
            ConnectDTO.ModelData.Mii = new BumiiDTO();
        }
        else
        {
            var mii = JsonConvert.DeserializeObject<BumiiDTO>(model) ?? new BumiiDTO();
            ConnectDTO.ModelData.Model = "";
            ConnectDTO.ModelData.Mii = mii;
        }
        
        Json = JsonConvert.SerializeObject(ConnectDTO);
    }
}