using System.Reflection;
using System.Text;
using BOTWM.Library.DTO;
using BOTWM.Library.HelperTypes;
using BOTWM.Library.JSONBuilder;
using BOTWM.Library.Settings;
using BOTWM.Logging;
using BOTWM.Networking;
using BOTWM.Server;
using BOTWM.Server.ServerClasses;
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
        ServerSettings Settings;
        ServerData ServerData;
        
        public string Version = "0.20.0";
        public short SerializationRate = 60;
        public short TargetFPS = 60;
        public short SleepMultiplier = 1;
        public bool isLocalTest = false;
        public bool ischaracterSpawn = true;
        public bool DisplayNames = true;
        public short GlyphDistance = 250;
        public short GlyphTime = 60;
        public bool isQuestSync = false;
        public bool isEnemySync = false;
        public string GameMode = "";
        public bool EnemyLog { get; set; }
        public int ClientLog { get; set; }
        public bool ServerLog { get; set; }

        
        public void Setup()
        {
            ServerData = new ServerData();
            var svConfig = new ServerConfig();
            Settings = GetServerSettings(svConfig);
            _host.Initialize("127.0.0.1", svConfig.Connection.Port);
            GameMode = svConfig.Gamemode.ToString();
            ServerData.Startup("127.0.0.1", svConfig.Connection.Port, svConfig.Connection.Password, svConfig.ServerInformation.Description, Settings);
            _host.OnReceive += Handle;
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
            var appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\BOTWM";
            var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(resource => resource.Contains("AppdataFiles")).ToList();

            if (!Directory.Exists(appdataFolder))
            {
                Directory.CreateDirectory(appdataFolder);
            }

            foreach (var resource in resources)
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
                if (stream is null)
                {
                    continue;
                }
                var output = $"{appdataFolder}\\{resource.Replace("BOTW.DedicatedServer.AppdataFiles.", "")}";
                using var appdataFile = new FileStream(output, FileMode.Create);
                var buffer = new byte[stream.Length + 1];
                stream.ReadExactly(buffer, 0, Convert.ToInt32(stream.Length));
                appdataFile.Write(buffer, 0, Convert.ToInt32(buffer.Length - 1));
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
                                          (GameModes)svConfig.DefaultGamemode.Special);

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

            var gm = GameModes.NoGamemode;

            if (Int32.TryParse(GMInput, out int value))
            {
                if (value == 1)
                    gm = GameModes.HunterVsSpeedrunner;
                if (value == 2)
                    gm = GameModes.DeathSwap;
            }

            ServerSettings selectedServerSettings = new ServerSettings("Custom", enemySync, questSync, korokSync, towerSync, shrineSync, locationSync, dungeonSync, gm);

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

        private void Handle(Peer peer, Tuple<PacketTypes, object>? request)
        {
            var socket = peer.Socket;
            try
            {
                var type = request.Item1;
                var dto = request.Item2;
                switch (type)
                {
                    case PacketTypes.Error:
                        throw new Exception($"[{peer.PlayerName}] Error receiving message. Disconnecting player...");
                    case PacketTypes.Ping:
                        PingDTO pingResult;

                        if (ServerData.Configuration.PASSWORD != (string)dto)
                        {
                            pingResult = new PingDTO()
                            {
                                CorrectPassword = false,
                                Description = "",
                                PlayerList = new NamesDTO(),
                                GameMode = "",
                                PlayerLimit = 32
                            };
                        }
                        else
                        {
                            pingResult = new PingDTO()
                            {
                                CorrectPassword = true,
                                Description = ServerData.Configuration.DESCRIPTION,
                                PlayerList = ServerData.GetPlayers(),
                                GameMode = GameMode,
                                PlayerLimit = 32
                            };
                        }

                        peer.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pingResult)));
                        socket.Close();
                        peer.Connected = false;
                        break;
                    case PacketTypes.Connect:
                        var userConfiguration = (ConnectDTO)dto;
                        var assignationResult = ServerData.TryAssigning(userConfiguration);

                        if (assignationResult.Response != 1)
                        {
                            peer.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(assignationResult)));
                            socket.Close();
                            peer.Connected = false;
                            Logger.LogInformation(
                                $"Player {userConfiguration.Name} tried to connect but failed with error {assignationResult.Response}");
                            break;
                        }

                        peer.PlayerNumber = assignationResult.PlayerNumber;
                        peer.PlayerName = ServerData.PlayerList[peer.PlayerNumber].Name;
                        peer.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(assignationResult)));

                        Logger.LogInformation(
                            $"Player {userConfiguration.Name} joined the server. Assigned to player {assignationResult.PlayerNumber + 1}.");
                        break;
                    case PacketTypes.Update:
                        ServerData.SetConnection(peer.PlayerNumber, true);

                        var userInformation = (ClientDTO)dto;

                        ServerData.UpdateWorldData(userInformation.WorldData, peer.PlayerNumber);
                        ServerData.UpdatePlayerData(userInformation.PlayerData, peer.PlayerNumber);
                        ServerData.UpdateEnemyData(userInformation.EnemyData);
                        ServerData.UpdateQuestData(userInformation.QuestData);

                        var serverDto = ServerData.GetData(peer.PlayerNumber);
                        serverDto.NetworkData.Map(this);

                        try
                        {
                            var bytes = new BufferWriter().Write(serverDto);
                            var bytes2 = new JsonBuilder().BuildArrayOfBytes(serverDto);
                            peer.Send(bytes2);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }

                        ServerData.ClearDeathSwap(peer.PlayerNumber);
                        break;
                    case PacketTypes.Disconnect:
                        Logger.LogInformation(
                            $"Player {ServerData.GetPlayer(peer.PlayerNumber).Name} disconnected. {(string)request.Item2}");
                        socket.Close();
                        peer.Connected = false;
                        ServerData.SetConnection(peer.PlayerNumber, false);
                        break;
                    default: 
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning(e.Message);
                throw;
            }
        }
    }
}
