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
        ClientDto = new ClientDTO();
        return;
        ClientDto.WorldData = new WorldDTO()
        {
            Time = reader.ReadFloat(),
            Day = reader.ReadInt(),
            Weather = reader.ReadInt() 
        };
        
        ClientDto.PlayerData = new ClientPlayerDTO()
        {
            Position = reader.ReadVec3f(),
            Rotation1 = reader.ReadQuaternion(),
            Rotation2 = reader.ReadQuaternion(),
            Rotation3 = reader.ReadQuaternion(),
            Rotation4 = reader.ReadQuaternion(),
            Animation = reader.ReadInt(),
            Health = reader.ReadInt(),
            AtkUp = reader.ReadFloat(),
            IsEquipped = reader.ReadBool(),
            Equipment = reader.ReadCharacterEquipment(),
            Location = reader.ReadCharacterLocation(),
            Bomb = reader.ReadVec3f(),
            Bomb2 = reader.ReadVec3f(),
            BombCube = reader.ReadVec3f(),
            BombCube2 = reader.ReadVec3f()
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