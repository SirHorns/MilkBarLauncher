using System.Reflection;
using BOTWM.Library.HelperTypes;
using BOTWM.Library.Settings;
using BOTWM.Logging;
using BOTWM.Server;
using Newtonsoft.Json;

namespace BOTWM.DedicatedServer
{
    public partial class DedicatedServer
    {

        Host _host = new();
        List<Command> _commandList = new();
        ConsoleColor _commandColors = ConsoleColor.Cyan;
        Dictionary<string, string> _serverVariables = new();
        Dictionary<string, List<string>> _questData = new();

        //Dictionary<string, bool[]> Gamemodes = new Dictionary<string, bool[]>();
        List<ServerSettings> _gamemodes = new();

        

        
        public void Setup()
        {
            var svConfig = new ServerConfig();
            _host.Initialize("127.0.0.1", svConfig.Connection.Port, svConfig.Connection.Password, svConfig.ServerInformation.Description, GetServerSettings(svConfig));
        }

        public void Run()
        {
            _host.Bind();
            
            _host.Start();

            Logger.LogInformation("Type help to see available commands");
        }
        
        public void process_commands(string input)
        {
            try
            {
                string input_command = input.Split(" ")[0];
                List<string> unprocessed_input_parameters = input.Split(" ").ToList();
                unprocessed_input_parameters.RemoveAt(0);

                List<string> input_parameters = new List<string>();

                for (int i = 0; i < unprocessed_input_parameters.Count; i++)
                {
                    if (unprocessed_input_parameters[i].First() != '\"' || (unprocessed_input_parameters[i].First() == '\"' && unprocessed_input_parameters[i].Last() == '\"'))
                    {
                        input_parameters.Add(unprocessed_input_parameters[i].Replace("\"", ""));
                        continue;
                    }

                    string new_input = unprocessed_input_parameters[i].Replace("\"", "");

                    for (int j = i + 1; j < unprocessed_input_parameters.Count; j++)
                    {
                        new_input += $" {unprocessed_input_parameters[j]}".Replace("\"", "");

                        if (unprocessed_input_parameters[j].Last() != '\"')
                            continue;

                        i = j;
                        input_parameters.Add(new_input);
                        break;

                    }
                }

                foreach (Command command in _commandList)
                {
                    if (input_command.ToLower() == command.Name.ToLower() || command.LowerAlternateNames.Contains(input_command.ToLower()))
                    {
                        if (input_parameters.Count() > 0 && input_parameters[0].ToLower() == "help")
                        {
                            string param = " ";

                            foreach (var parameter in command.Method.GetParameters())
                            {
                                param += "<" + parameter.Name + "> ";
                            }

                            param = param.Substring(0, param.Length - 1);

                            Logger.LogInformation($"{command.Name}{param}: {command.Description}", color: _commandColors);

                            foreach (ExtraHelp extraHelp in command.Method.GetCustomAttributes(typeof(ExtraHelp), false))
                            {
                                Logger.LogInformation($"{extraHelp.Help}");
                            }

                            return;
                        }
                        else
                        {
                            if (input_parameters.Count() >= command.Method.GetParameters().Where(x => !x.IsOptional).Count() && input_parameters.Count() <= command.Method.GetParameters().Count())
                            {
                                List<object> parameters = new List<object>();

                                foreach (string param in input_parameters)
                                {
                                    parameters.Add(param);
                                }

                                for (int i = 0; i < command.Method.GetParameters().Count() - input_parameters.Count(); i++)
                                {
                                    parameters.Add(Type.Missing);
                                }

                                command.Method.Invoke(this, parameters.ToArray());
                                return;
                            }
                            else
                            {

                                string param = " ";

                                foreach (var parameter in command.Method.GetParameters())
                                {
                                    param += parameter.Name + " ";
                                }

                                Logger.LogError($"Correct usage: {command.Name}{param}");
                                return;

                            }
                        }
                    }
                }

                Logger.LogError($"Command {input_command} was not found. Type help to see available commands");
            }
            catch(Exception ex)
            {
                Logger.LogError($"Command failed {ex.ToString()}");
            }
        }
        
        public void SetupCommands()
        {
            string AppdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BOTWM";
            string fileName = "\\QuestFlagsNames.txt";

            string text = File.ReadAllText(AppdataFolder + fileName);

            _questData = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(text);

            _serverVariables.Add("time", "t");
            _serverVariables.Add("day", "d");
            _serverVariables.Add("weather", "w");

            //Gamemodes.Add("Game Completion", new bool[] { true, true, true, false, true, true, true, false, false });
            //Gamemodes.Add("Hunter VS Speedrunner", new bool[] { true, true, false, false, true, true, true, false, true });
            //Gamemodes.Add("Any% Speedrun", new bool[] { true, true, false, false, true, true, true, false, false });
            //Gamemodes.Add("Bingo???", new bool[] { true, true, false, false, true, true, true, false, false });
            //Gamemodes.Add("Hide n' Seek", new bool[] { true, true, false, false, false, false, false, false, false });

            _gamemodes = JsonConvert.DeserializeObject<List<ServerSettings>>(File.ReadAllText(Directory.GetCurrentDirectory() + "/Gamemodes.json"));

            var methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass)
                .SelectMany(x => x.GetMethods())
                .Where(x => x.GetCustomAttributes(typeof(ServerCommand), false).FirstOrDefault() != null && x.GetCustomAttributes(typeof(Description), false).FirstOrDefault() != null);

            foreach (var method in methods)
            {
                List<string> alternateNames = new List<string>();

                foreach (AlternateName attribute in method.GetCustomAttributes(typeof(AlternateName), false))
                {
                    alternateNames.Add(attribute.name);
                }

                bool shouldAdd = true;

                foreach (var parameter in method.GetParameters())
                {
                    if (parameter.ParameterType != typeof(string))
                    {
                        shouldAdd = false;
                    }
                }

                if (!shouldAdd) continue;

                _commandList.Add(new Command(method, method.Name, ((Description)method.GetCustomAttribute(typeof(Description), false)).description, alternateNames));
            }
        }

        public void CopyAppdataFiles()
        {
            string AppdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BOTWM";
            List<string> Resources = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(resource => resource.Contains("AppdataFiles")).ToList();

            if (!Directory.Exists(AppdataFolder))
                Directory.CreateDirectory(AppdataFolder);

            foreach (string resource in Resources)
            {
                Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
                string output = $"{AppdataFolder}\\{resource.Replace("BOTW.DedicatedServer.AppdataFiles.", "")}";
                using (FileStream AppdataFile = new FileStream(output, FileMode.Create))
                {
                    byte[] b = new byte[s.Length + 1];
                    s.Read(b, 0, Convert.ToInt32(s.Length));
                    AppdataFile.Write(b, 0, Convert.ToInt32(b.Length - 1));
                }
            }
        }
        
        //
        

        private ServerSettings GetServerSettings(ServerConfig svConfig)
        {
            if (svConfig.Gamemode.DefaultGamemode)
                return new ServerSettings(svConfig.DefaultGamemode.Name,
                                          svConfig.DefaultGamemode.EnemySync,
                                          svConfig.DefaultGamemode.QuestSync,
                                          svConfig.DefaultGamemode.KorokSync,
                                          svConfig.DefaultGamemode.TowerSync,
                                          svConfig.DefaultGamemode.ShrineSync,
                                          svConfig.DefaultGamemode.LocationSync,
                                          svConfig.DefaultGamemode.DungeonSync,
                                          (Gamemode)svConfig.DefaultGamemode.Special);

            bool isGamemode = Logger.LogInput("Are you playing a gamemode? (1 for true, 0 for false): ") == "1" ? true : false;

            if (isGamemode)
            {
                Logger.LogInformation("---Available gamemodes---", color: _commandColors);

                int counter = 0;

                foreach (ServerSettings Gamemode in _gamemodes)
                {
                    Logger.LogInformation($"({counter}) {Gamemode.SettingsName}");
                    counter++;
                }

                int optionSelected = -1;

                while (optionSelected == -1)
                {
                    if (!Int32.TryParse(Logger.LogInput("Type the number corresponding to the gamemode you want to play: "), out optionSelected))
                    {
                        Logger.LogError($"Invalid gamemode. Correct values go from 0 to {_gamemodes.Count() - 1}");
                        continue;
                    }
                    else
                    {
                        if (optionSelected > _gamemodes.Count() - 1 || optionSelected < 0)
                        {
                            Logger.LogError($"Invalid gamemode. Correct values go from 0 to {_gamemodes.Count() - 1}");
                            optionSelected = -1;
                            continue;
                        }

                        Logger.LogInformation($"Selected gamemode {_gamemodes[optionSelected].SettingsName}", color: _commandColors);

                        return _gamemodes[optionSelected];
                    }
                }
            }

            //V | K | T | O | C | L | D

            bool enemySync = InputToBoolean("Enemy sync (1 for true, 0 for false): ");
            bool questSync = InputToBoolean("Quest sync (1 for true, 0 for false): ");
            bool korokSync = InputToBoolean("Korok sync (1 for true, 0 for false): ");
            bool towerSync = InputToBoolean("Tower sync (1 for true, 0 for false): ");
            bool shrineSync = InputToBoolean("Shrine sync (1 for true, 0 for false): ");
            bool locationSync = InputToBoolean("Location sync (1 for true, 0 for false): ");
            bool dungeonSync = InputToBoolean("Dungeon sync (1 for true, 0 for false): ");
            string GMInput = Logger.LogInput("Gamemode selection (0 for no gamemode, 1 for Hunter vs Speedrunner, 2 for DeathSwap): ");

            Gamemode GM = Gamemode.NoGamemode;

            if (Int32.TryParse(GMInput, out int value))
            {
                if (value == 1)
                    GM = Gamemode.HunterVsSpeedrunner;
                if (value == 2)
                    GM = Gamemode.DeathSwap;
            }

            ServerSettings selectedServerSettings = new ServerSettings("Custom", enemySync, questSync, korokSync, towerSync, shrineSync, locationSync, dungeonSync, GM);

            bool Match = false;

            foreach (ServerSettings gamemode in _gamemodes)
            {
                if (selectedServerSettings.CompareSettings(gamemode))
                {
                    Logger.LogWarning($"Your selected server settings match \"{gamemode.SettingsName}\" gamemode. Next time you want to play with these settings, you can select that gamemode.");
                    Match = true;
                    selectedServerSettings.SettingsName = gamemode.SettingsName;
                    break;
                }
            }

            if (!Match)
            {
                if (Logger.LogInput("Do you wish to save your selected server settings? (1 for yes, 0 for no): ") == "1")
                {
                    selectedServerSettings.SettingsName = Logger.LogInput("Select a name for your settings: ");

                    _gamemodes.Add(selectedServerSettings);

                    string GamemodeJson = JsonConvert.SerializeObject(_gamemodes);

                    File.WriteAllText(Directory.GetCurrentDirectory() + "/Gamemodes.json", GamemodeJson);

                    Logger.LogInformation($"Saved gamemode: {selectedServerSettings.SettingsName}");
                }
                else
                {
                    selectedServerSettings.SettingsName = "Custom";
                }
            }

            return selectedServerSettings;
        }

        private bool InputToBoolean(string message) => Logger.LogInput(message) == "1" ? true : false;
    }
}
