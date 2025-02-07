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
            IniFile ini = new IniFile();
            ini.Load("ServerConfig.ini");

            foreach(var section in this.GetType().GetFields())
            {
                var field = Activator.CreateInstance(section.FieldType);

                foreach (var key in section.FieldType.GetFields())
                    key.SetValue(field, Convert.ChangeType(ini.Sections[section.Name].Keys[key.Name].Value, key.FieldType));

                section.SetValue(this, field);
            }
        }
    }
}