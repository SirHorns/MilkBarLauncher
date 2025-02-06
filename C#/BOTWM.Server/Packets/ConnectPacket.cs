using System.Text;
using BOTWM.Library.DTO;
using Newtonsoft.Json;

namespace BOTWM.Server.Packets;

public class ConnectPacket: BasePacket
{
    public int PlayerId { get; set; }
    public string PlayerName { get; set; }
    public ConnectDTO ConnectDTO { get; set; }

    public override void ReadBytes(byte[] rawBytes)
    {
        ConnectDTO = new ConnectDTO();

        int stringSize = PacketBytesIo.ReadByte(rawBytes);
        ConnectDTO.Name = Encoding.UTF8.GetString(PacketBytesIo.ReadBytes(rawBytes, stringSize));

        stringSize = PacketBytesIo.ReadByte(rawBytes);
        ConnectDTO.Password = Encoding.UTF8.GetString(PacketBytesIo.ReadBytes(rawBytes, stringSize));

        ConnectDTO.ModelData = new ModelDataDTO();

        stringSize = PacketBytesIo.ReadByte(rawBytes);
        ConnectDTO.ModelData.ModelType = byte.Parse(Encoding.UTF8.GetString(PacketBytesIo.ReadBytes(rawBytes, stringSize)));

        stringSize = BitConverter.ToInt16(PacketBytesIo.ReadBytes(rawBytes, 2), 0);
        string model = Encoding.UTF8.GetString(PacketBytesIo.ReadBytes(rawBytes, stringSize));

        if(ConnectDTO.ModelData.ModelType < 2)
        {
            ConnectDTO.ModelData.Model = model;
            ConnectDTO.ModelData.Mii = new BumiiDTO();
        }
        else
        {
            ConnectDTO.ModelData.Model = "";
            ConnectDTO.ModelData.Mii = JsonConvert.DeserializeObject<BumiiDTO>(model);
        }
    }
}