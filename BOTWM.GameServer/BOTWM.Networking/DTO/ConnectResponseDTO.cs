using BOTWM.Library.Settings;

namespace BOTWM.Library.DTO
{
    public class ConnectResponseDTO
    {
        public int Response;
        public int PlayerNumber;
        public ServerSettings Settings;
        public bool QuestSync;
        public string EnemySyncList;
    }
}
