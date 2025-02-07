using MadMilkman.Ini;

namespace BOTWM.Library.HelperTypes
{
    public class ServerConfig
    {

        public class ConnectionData
        {
            public string IP;
            public int Port;
            public string Password;
        }

        public class ServerInformationData
        {
            public string Description;
        }

        public class GamemodeData
        {
            public bool DefaultGamemode;
        }

        public class DefaultGamemodeData
        {
            public string Name;
            public bool EnemySync;
            public bool QuestSync;
            public bool KorokSync;
            public bool TowerSync;
            public bool ShrineSync;
            public bool LocationSync;
            public bool DungeonSync;
            public int Special;
        }

        public ConnectionData Connection;
        public ServerInformationData ServerInformation;
        public GamemodeData Gamemode;
        public DefaultGamemodeData DefaultGamemode;

        public ServerConfig()
        {
            
        }

        public void LoadIni(IniFile iniFile)
        {
            
            foreach(var section in typeof(ServerConfig).GetFields())
            {
                var obj = Activator.CreateInstance(section.FieldType);

                foreach (var key in section.FieldType.GetFields())
                {
                    var value = Convert.ChangeType(
                        iniFile.Sections[section.Name].Keys[key.Name].Value, 
                        key.FieldType);
                    key.SetValue(obj, value);
                }

                section.SetValue(this, obj);
            }
        }
    }
}