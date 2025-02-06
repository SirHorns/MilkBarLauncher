using BOTWM.Library.DTO;
using BOTWM.Library.JSONBuilder;

namespace BOTWM.Server.Packets;

public class UpdatePacket: BasePacket
{
    public ClientDTO ClientDto { get; set; }
    
    public UpdatePacket()
    {
    }

    public UpdatePacket(byte[] bytes) : base(bytes)
    {
    }

    public override void ReadBody(BufferReader reader)
    {
        var res = new JsonBuilder().BuildFromBytes(reader.Bytes);
        
        ClientDto = (ClientDTO)res.Item2;
        
        return;

        ClientDto.WorldData = new WorldDTO()
        {
            Time = reader.ReadFloat(),
            Day = reader.ReadInt(),
            Weather = reader.ReadInt() 
        };
        ClientDto.PlayerData = new ClientPlayerDTO()
        {
            
        };
        ClientDto.EnemyData = new EnemyDTO()
        {
            Health = new List<EnemyData>()
        };
        ClientDto.QuestData = new QuestsDTO()
        {
            Completed = new List<string>(),
        };

    }
}