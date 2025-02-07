using BOTWM.Library.DataTypes;

namespace BOTWM.Library.DTO
{
    public enum PlayerStatus : byte
    {
        Close,
        Far
    }

    public class PlayerBaseDTO
    {
        public byte PlayerNumber;
        public Vec3f Position;
        public bool Updated;
        public CharacterLocation Location;
    }

    public class ClosePlayerDTO: PlayerBaseDTO
    {
        public PlayerStatus Status => PlayerStatus.Close;
        public Quaternion Rotation1;
        public Quaternion Rotation2;
        public Quaternion Rotation3;
        public Quaternion Rotation4;
        public int Animation;
        public int Health;
        public float AtkUp;
        public bool IsEquipped;
        public CharacterEquipment Equipment;
        public Vec3f Bomb;
        public Vec3f Bomb2;
        public Vec3f BombCube;
        public Vec3f BombCube2;
    }

    public class FarPlayerDTO: PlayerBaseDTO
    {
        public PlayerStatus Status => PlayerStatus.Far;
    }

    public class ClientPlayerDTO
    {
        public Vec3f Position;
        public Quaternion Rotation1;
        public Quaternion Rotation2;
        public Quaternion Rotation3;
        public Quaternion Rotation4;
        public int Animation;
        public int Health;
        public float AtkUp;
        public bool IsEquipped;
        public CharacterEquipment Equipment;
        public CharacterLocation Location;
        public Vec3f Bomb;
        public Vec3f Bomb2;
        public Vec3f BombCube;
        public Vec3f BombCube2;
    }
}