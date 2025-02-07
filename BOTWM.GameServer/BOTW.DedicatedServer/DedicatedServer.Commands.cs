using System.Reflection;
using BOTWM.Library;
using BOTWM.Library.DataTypes;
using BOTWM.Library.DTO;
using BOTWM.Logging;
using BOTWM.Server.ServerClasses;
using Newtonsoft.Json;

namespace BOTWM.DedicatedServer;

public partial class DedicatedServer
{
    [ServerCommand]
    [AlternateName("Commands")]
    [AlternateName("H")]
    [Description("Shows available commands")]
    public void Help()
    {
        Logger.LogInformation("---Showing available commands---", color: _commandColors);

        Console.ForegroundColor = ConsoleColor.White;

        foreach (var command in _commandList)
        {
            if (((ServerCommand)command.Method.GetCustomAttribute(typeof(ServerCommand), false)).Debug) continue;

            string param = "";

            if (command.Method.GetParameters().Count() > 0)
            {
                param += " ";

                foreach (var parameter in command.Method.GetParameters())
                {
                    param += "<" + parameter.Name + "> ";
                }

                param = param.Substring(0, param.Length - 1);
            }

            Logger.LogInformation($"{command.Name}{param}: {command.Description}");
        }
    }

    private Dictionary<int, string> GetLandmarks(string filter = "") => filter == ""
        ? LandmarkPositions.Keys
            .Select((key, index) => new { key, index })
            .ToDictionary(x => x.index + 1, x => x.key)
        : LandmarkPositions.Keys
            .Select((key, index) => new { key, index })
            .Where(x => x.key.ToLower().Contains(filter.ToLower()))
            .ToDictionary(x => x.index + 1, x => x.key);

    [ServerCommand]
    [Description("Gets the available landmarks to teleport to")]
    [ExtraHelp("")]
    public void Landmarks(string filter = "")
    {
        Dictionary<int, string> FilteredLandmarks = GetLandmarks(filter);

        if (FilteredLandmarks.Count == 0)
        {
            Logger.LogError($"No landmark found with the filter {filter}");
        }

        foreach (KeyValuePair<int, string> Landmark in FilteredLandmarks)
        {
            string spaces = Landmark.Key < 10 ? "  " : Landmark.Key < 100 ? " " : "";

            Logger.LogInformation($"[{Landmark.Key}]{spaces} {Landmark.Value}", color: _commandColors);
        }
    }

    [ServerCommand]
    [Description("Teleport player to position or other player")]
    [AlternateName("Tp")]
    [ExtraHelp("Usage 1: Tp <Source> <Destination>")]
    [ExtraHelp("<Source>: Player number, player name or @a for everyone")]
    [ExtraHelp("Use \"p#\" to teleport a player by its number. ")]
    [ExtraHelp("<Destination>: Player number, player name or landmark")]
    [ExtraHelp("Use \"p#\" to teleport to a player by its number. Otherwise, use \"l#\" to teleport to a landmark")]
    [ExtraHelp("Usage 2: Tp <Source> <Destination_x> <Destination_y> <Destination_z>")]
    [ExtraHelp("<Source>: Player number, player name or @a for everyone")]
    [ExtraHelp("<Destination_x>: Destination x axis")]
    [ExtraHelp("<Destination_x>: Destination y axis")]
    [ExtraHelp("<Destination_x>: Destination z axis")]
    public void Teleport(string source, string destination, string destination_y = "", string destination_z = "")
    {
        Dictionary<byte, string> PlayerList = ServerData.GetPlayers().Names;

        List<int> SourcePlayers = new List<int>();

        if (source.StartsWith("p") && source.Count() < 4)
        {
            int player = 0;
            if (Int32.TryParse(source.Replace("p", ""), out player) && PlayerList.ContainsKey((byte)(player - 1)))
                SourcePlayers.Add(player - 1);
            else
                SourcePlayers.AddRange(PlayerList.Where(p => p.Value == source).Select(p => (int)p.Key));
        }
        else if (source == "@a")
            SourcePlayers.AddRange(PlayerList.Select(p => (int)p.Key));
        else
            SourcePlayers.AddRange(PlayerList.Where(p => p.Value == source).Select(p => (int)p.Key));

        SourcePlayers = SourcePlayers.Where(p => ServerData.GetPlayer(p).Connected).ToList();

        if (SourcePlayers.Count == 0)
        {
            Logger.LogError($"Could not find player that matches {source}");
            return;
        }

        Vec3f Destination = new Vec3f();

        if (!string.IsNullOrEmpty(destination_y) && !string.IsNullOrEmpty(destination_z))
        {
            float x, y, z;

            if (!float.TryParse(destination, out x) || !float.TryParse(destination_y, out y) ||
                !float.TryParse(destination_z, out z))
            {
                Logger.LogError($"Destination input was not valid. Use \"tp help\" for extra information");
                return;
            }

            Destination = new Vec3f(x, y, z);
        }
        else
        {
            if (destination.StartsWith("p") && destination.Count() < 4)
            {
                int player = 0;
                if (Int32.TryParse(destination.Replace("p", ""), out player))
                    Destination = ServerData.GetPlayer(player - 1).Position;
                else
                    Destination = ServerData.GetPlayer(PlayerList.Where(p => p.Value == destination)
                        .Select(p => (int)p.Key).FirstOrDefault()).Position;
            }
            else if (destination.StartsWith("l") && destination.Count() < 5)
            {
                int landmark = 0;
                if (Int32.TryParse(destination.Replace("l", ""), out landmark) && GetLandmarks().ContainsKey(landmark))
                    Destination = LandmarkPositions[GetLandmarks()[landmark]];
                else
                    Destination = ServerData.GetPlayer(PlayerList.Where(p => p.Value == destination)
                        .Select(p => (int)p.Key).FirstOrDefault()).Position;
            }
            else
            {
                if (PlayerList.Any(p => p.Value == destination))
                    Destination = ServerData.GetPlayer(PlayerList.Where(p => p.Value == destination)
                        .Select(p => (int)p.Key).FirstOrDefault()).Position;
            }
        }

        if (Destination == null || (Destination.x == 0 && Destination.y == 0 && Destination.z == 0))
        {
            Logger.LogError($"Destination input was not valid. Use \"tp help\" for extra information");
            return;
        }

        ServerData.TeleportData.AddTp(SourcePlayers, Destination);

        Logger.LogInformation(
            $"Requested teleport of {SourcePlayers.Count} players to {Destination.x}, {Destination.y}, {Destination.z}");
    }

    [ServerCommand]
    [Description("Stops enemy and quest sync")]
    public void Stop()
    {
        isEnemySync = false;
        isQuestSync = false;

        Logger.LogInformation("Deactivated quest and enemy sync", color: _commandColors);
    }

    [ServerCommand]
    [Description("Starts enemy and quest sync")]
    public void Start()
    {
        isEnemySync = true;
        isQuestSync = true;

        Logger.LogInformation("Activated quest and enemy sync", color: _commandColors);
    }

    [ServerCommand]
    [Description("Get or set limits for DeathSwap")]
    [ExtraHelp("Usage 1: DeathSwap <state>")]
    [ExtraHelp("<state>: on or off")]
    [ExtraHelp("Usage 2: DeathSwap <LowerLimit>:<UpperLimit>")]
    [ExtraHelp("<LowerLimit> and <UpperLimit> should be integers")]
    [ExtraHelp("Usage 3: DeathSwap <Value>")]
    [ExtraHelp("<Value> should be an integer")]
    [ExtraHelp("Usage 4: DeathSwap")]
    [ExtraHelp("Get current state and current limits")]
    [AlternateName("DS")]
    public void DeathSwap(string parameter = "")
    {
        if (parameter == "on")
        {
            ServerData.DeathSwapMutex.WaitOne(100);

            ServerData.DeathSwap.Enabled = true;

            Logger.LogInformation("Enabled death swap.", color: _commandColors);

            ServerData.DeathSwapMutex.ReleaseMutex();

            return;
        }
        else if (parameter == "off")
        {
            ServerData.DeathSwapMutex.WaitOne(100);

            ServerData.DeathSwap.Enabled = false;
            ServerData.DeathSwap.Running = false;

            Logger.LogInformation("Disabled death swap.", color: _commandColors);

            ServerData.DeathSwapMutex.ReleaseMutex();

            return;
        }
        else if (parameter.Contains(':'))
        {
            if (parameter.Split(":").Length != 2)
            {
                Logger.LogError("Invalid parameter. Use DeathSwap Help to get information on how to use the command.");
                return;
            }

            int Lower;
            int Upper;

            if (!int.TryParse(parameter.Split(":")[0], out Lower))
            {
                Logger.LogError(
                    "Invalid Lower limit. Use DeathSwap Help to get information on how to use the command.");
                return;
            }

            if (!int.TryParse(parameter.Split(":")[1], out Upper))
            {
                Logger.LogError(
                    "Invalid Upper limit. Use DeathSwap Help to get information on how to use the command.");
                return;
            }

            ServerData.DeathSwapMutex.WaitOne(100);

            ServerData.DeathSwap.ChangeLimits(Lower, Upper, -1, 1);
            ServerData.DeathSwap.CalculateNewLimit();

            Logger.LogInformation($"Set limits to: {Lower}:{Upper}", color: _commandColors);

            ServerData.DeathSwapMutex.ReleaseMutex();

            return;
        }
        else if (parameter == "")
        {
            string ExtraMessage = "";

            if (ServerData.DeathSwap.TimerLimit.random)
                ExtraMessage =
                    $", Lower limit: {ServerData.DeathSwap.TimerLimit.Lower}, Upper limit: {ServerData.DeathSwap.TimerLimit.Upper}";

            Double TimeLeft = ServerData.DeathSwap.TimeLeft();

            Logger.LogInformation(
                $"Time until next swap: {Math.Truncate(TimeLeft)} min {(int)Math.Round((TimeLeft - Math.Truncate(TimeLeft)) * 60, 0)} sec",
                color: _commandColors);
            Logger.LogInformation(
                $"Current DeathSwap settings => Enabled: {ServerData.DeathSwap.Enabled}, Is random: {ServerData.DeathSwap.TimerLimit.random}{ExtraMessage}",
                color: _commandColors);
            return;
        }
        else
        {
            int NewValue;

            if (int.TryParse(parameter, out NewValue))
            {
                ServerData.DeathSwapMutex.WaitOne(100);

                //server.DeathSwap.TimerLimit.random = true;
                ServerData.DeathSwap.ChangeLimits(-1, -1, NewValue, 0);

                Logger.LogInformation($"Set death swap timer to {NewValue}", color: _commandColors);

                ServerData.DeathSwapMutex.ReleaseMutex();

                return;
            }

            Logger.LogError("Invalid parameter. Use DeathSwap Help to get information on how to use the command.");
            return;
        }
    }


    [ServerCommand]
    [Description("Change Hunter vs Speedrunner glyph settings")]
    [ExtraHelp("Usage: Glyph <time (in seconds)> <distance>")]
    [ExtraHelp("To leave a value unchanged, set it to -1")]
    public void Glyph(string time = "", string distance = "")
    {
        short Time;
        short Distance;
        List<string> message = new List<string>();

        if (time == "" || time == "-1")
        {
            Time = GlyphTime;
        }
        else
        {
            if (!Int16.TryParse(time, out Time))
            {
                Logger.LogError("Invalid time value. Time should be an integer");
                return;
            }
            else
            {
                message.Add($" time to {Time} ");
            }
        }

        if (distance == "" || distance == "-1")
        {
            Distance = GlyphDistance;
        }
        else
        {
            if (!Int16.TryParse(distance, out Distance))
            {
                Logger.LogError("Invalid distance value. Distance should be an integer");
                return;
            }
            else
            {
                message.Add($" distance to {Distance} ");
            }
        }

        GlyphTime = Time;
        GlyphDistance = Distance;

        Logger.LogInformation($"Changed the{string.Join("and", message)}", color: _commandColors);
    }

    [ServerCommand(true)]
    [Description("Shows available debug commands")]
    [AlternateName("_H")]
    public void _Help()
    {
        Logger.LogInformation("---Showing available debug commands---", color: _commandColors);

        Console.ForegroundColor = ConsoleColor.White;

        foreach (var command in _commandList)
        {
            if (!((ServerCommand)command.Method.GetCustomAttribute(typeof(ServerCommand), false)).Debug) continue;

            string param = "";

            if (command.Method.GetParameters().Count() > 0)
            {
                param += " ";

                foreach (var parameter in command.Method.GetParameters())
                {
                    param += "<" + parameter.Name + "> ";
                }

                param = param.Substring(0, param.Length - 1);
            }

            Logger.LogInformation($"{command.Name}{param}: {command.Description}");
        }
    }

    //[ServerCommand(true)]
    //[Description("Get or set next client log print")]
    //[AlternateName("_CL")]
    //[ExtraHelp("Usage: ClientLog <value>")]
    //[ExtraHelp("<value> = 0 for get current value")]
    //public void _ClientLog(string value = "0")
    //{

    //    int val;

    //    if (!Int32.TryParse(value, out val))
    //    {
    //        server.LogInfo($"The parameter <value> has to be a number. Correct usage: ClientLog <value>", ConsoleColor.DarkRed);
    //        return;
    //    }

    //    if(val == 0)
    //    {
    //        server.LogInfo($"{server.ClientLog}", commandColors);
    //    }else if(val > 0)
    //    {
    //        server.LogInfo($"Succesfuly updated client log to {val}", commandColors);
    //        server.ClientLog = val;
    //    }

    //}

    //[ServerCommand(true)]
    //[Description("Set next server log print")]
    //[AlternateName("_SL")]
    //public void _ServerLog()
    //{

    //    server.LogInfo($"Succesfuly activated server log", commandColors);
    //    server.ServerLog = true;

    //}

    [ServerCommand]
    [Description("Get or set time")]
    [ExtraHelp("Usage: Time Get or Time Set <value>")]
    [ExtraHelp("<value> format: HH:MM")]
    public void Time(string action = "", string value = "")
    {
        if (action.ToLower() == "get" || action == "")
        {
            if (value != "") Logger.LogWarning($"Ignored the value {value}. Correct usage: Time Get");

            double serverTime = ServerData.WorldData.Time;
            int serverDay = ServerData.WorldData.Day;

            if (serverTime == -1)
            {
                Logger.LogWarning("Time has not been set yet.");
                return;
            }

            string Hour;
            string Minute;

            if (serverTime == 0)
            {
                Hour = "00";
                Minute = "00";
            }
            else
            {
                Hour = Math.Truncate((serverTime / 15)).ToString();
                Minute = ((((serverTime / 15) - Math.Truncate(serverTime / 15)) * 60).ToString() + "0").Substring(0, 2);

                if (Minute[1] == '.') Minute = "0" + Minute[0];
            }

            Logger.LogInformation($"Server time is {Hour}:{Minute} and the current day is {serverDay}",
                color: _commandColors);
            return;
        }
        else if (action.ToLower() == "set")
        {
            int Hour;
            int Minute;

            if (value == "")
            {
                Logger.LogError($"The parameter <value> cannot be empty. Correct usage: Time Set HH:MM");
                return;
            }

            if (value.Split(":").Length != 2)
            {
                Logger.LogError($"Invalid time format. Correct usage: Time Set HH:MM");
                return;
            }

            if (!Int32.TryParse(value.Split(":")[0], out Hour))
            {
                Logger.LogError($"The parameter <value> has to be a time value. Correct usage: Time Set HH:MM");
                return;
            }

            if (!Int32.TryParse(value.Split(":")[1], out Minute))
            {
                Logger.LogError($"The parameter <value> has to be a time value. Correct usage: Time Set HH:MM");
                return;
            }

            if (Hour < 0 || Hour > 24)
            {
                Logger.LogError($"Invalid Hour value. Hours can only take values from 0 to 24");
                return;
            }

            if (Minute < 0 || Minute > 60)
            {
                Logger.LogError($"Invalid Minute value. Minutes can only take values from 0 to 60");
                return;
            }

            float newTime = ((float)Hour + ((float)Minute / 60)) * 360 / 24;
            double serverTime = ServerData.WorldData.Time;
            int serverDay = ServerData.WorldData.Day;
            int serverWeather = ServerData.WorldData.Weather;

            if (serverTime == -1)
            {
                ServerData.UpdateWorldData(new WorldDTO() { Time = newTime, Day = 0, Weather = serverWeather }, -1);
            }
            else
            {
                if (newTime > serverTime)
                {
                    ServerData.UpdateWorldData(
                        new WorldDTO() { Time = newTime, Day = serverDay, Weather = serverWeather }, -1);
                }
                else
                {
                    ServerData.UpdateWorldData(
                        new WorldDTO() { Time = newTime, Day = serverDay + 1, Weather = serverWeather }, -1);
                }
            }

            Logger.LogInformation($"Time set to {value}", color: _commandColors);
        }
        else
        {
            Logger.LogError($"Invalid action. Correct usage: Time Get or Time Set <value>");
        }
    }

    [ServerCommand]
    [Description("Get or set weather")]
    [ExtraHelp("Usage: Weather Get or Weather Set <value>")]
    [ExtraHelp("<value> options:")]
    [ExtraHelp("\t     auto")]
    [ExtraHelp("\t     BlueSky")]
    [ExtraHelp("\t     Cloudy")]
    [ExtraHelp("\t     Rain")]
    [ExtraHelp("\t     HeavyRain")]
    [ExtraHelp("\t     Snow")]
    [ExtraHelp("\t     HeavySnow")]
    [ExtraHelp("\t     Thunderstorm")]
    [ExtraHelp("\t     ThunderRain")]
    [ExtraHelp("\t     BlueSkyRain")]
    public void Weather(string action = "", string value = "")
    {
        if (action.ToLower() == "get" || action == "")
        {
            int ServerWeather = ServerData.WorldData.Weather;

            Logger.LogInformation($"Current server weather is {((Weathers)ServerWeather).ToString()}",
                color: _commandColors);
            return;
        }
        else if (action.ToLower() == "set")
        {
            value = value.ToLower();

            if (value == "auto")
            {
                ServerData.WorldData.isForcedWeather = false;
                Logger.LogInformation("Returned weather control back to players.", color: _commandColors);
                return;
            }

            Weathers UserWeather;

            if (!Enum.TryParse<Weathers>(value, out UserWeather))
            {
                Logger.LogError($"Invalid weather value. Type Weather Help to see the available weather types");
                return;
            }

            double serverTime = ServerData.WorldData.Time;
            int serverDay = ServerData.WorldData.Day;
            int serverWeather = ServerData.WorldData.Weather;

            ServerData.WorldData.isForcedWeather = true;
            ServerData.UpdateWorldData(
                new WorldDTO() { Time = (float)serverTime, Day = serverDay, Weather = (int)UserWeather }, -1);

            Logger.LogInformation($"Weather set to {value}", color: _commandColors);
            return;
        }
        else
        {
            Logger.LogError(
                $"Invalid action. Correct usage: Weather Get or Weather Set <value>. Type Weather Help to see the available weather types");
        }
    }

    //[ServerCommand]
    //[Description("See current mod's version")]
    //[AlternateName("ver")]
    //public void Version()
    //{
    //    server.LogInfo($"Current mod's version is: {server.Version}", ConsoleColor.DarkMagenta);
    //}

    [ServerCommand]
    [Description("Cleans the console")]
    [AlternateName("CLS")]
    public void Clear()
    {
        Console.Clear();
    }

    [ServerCommand(true)]
    [Description("Prints the current quest data from the server")]
    public void _Quests(string search = "")
    {
        foreach (string item in ServerData.QuestData.ServerQuests)
        {
            if (string.IsNullOrEmpty(search) || (_questData[item][1].Contains(search)))
            {
                Logger.LogInformation($"{_questData[item][1]}");
            }
        }
    }

    [ServerCommand(true)]
    [Description("Completes quest")]
    [AlternateName("_CQ")]
    [ExtraHelp("Usage: _CompleteQ <quest>")]
    [ExtraHelp("<quest> example: C1,C2,C3")]
    public void _CompleteQ(string quest)
    {
        quest = $"[\"{string.Join("\",\"", quest.Split(","))}\"]";

        try
        {
            ServerData.ProcessExternalQuests(JsonConvert.DeserializeObject<List<string>>(quest));

            Logger.LogInformation("Quests added to the list", color: _commandColors);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            Logger.LogError("Failed to add quests. Use _CompleteQ help to see the correct usage");
        }
    }

    [ServerCommand(true)]
    [Description("Prints a certain property from a player")]
    public void _Get(string playerNumber, string property)
    {
        int PN = 0;

        if (!Int32.TryParse(playerNumber, out PN))
        {
            Logger.LogError("Parameter playerNumber should be an integer value");
            return;
        }

        if (ServerData.PlayerList.Count < PN || !ServerData.PlayerList[PN - 1].Connected)
        {
            Logger.LogInformation($"Player {PN} is not connected");
            return;
        }

        FieldInfo[] PlayerFields = typeof(Player).GetFields();

        if (!PlayerFields.Any(Fld => Fld.Name.ToLower() == property.ToLower()))
        {
            Logger.LogError($"Player{playerNumber} doesn't contain the property {property}");
            return;
        }

        Logger.LogInformation($"---Player{playerNumber}'s {property} is---", color: _commandColors);

        object playerData = PlayerFields.Where(Fld => Fld.Name.ToLower() == property.ToLower()).First()
            .GetValue(ServerData.PlayerList[PN - 1]);

        if (playerData.GetType().ToString() == "System.Collections.Generic.Dictionary`2[System.String,System.Object]")
        {
            foreach (KeyValuePair<string, object> kvp in (Dictionary<string, object>)playerData)
            {
                Logger.LogInformation($"{kvp.Key}: {kvp.Value}");
            }
        }
        else if (playerData.GetType().ToString() ==
                 "System.Collections.Generic.Dictionary`2[System.String,System.String]")
        {
            foreach (KeyValuePair<string, string> kvp in (Dictionary<string, string>)playerData)
            {
                Logger.LogInformation($"{kvp.Key}: {kvp.Value}");
            }
        }
        else if (playerData.GetType().ToString().Contains("List"))
        {
            foreach (object item in (List<object>)playerData)
            {
                Logger.LogInformation($"{item.ToString()}");
            }
        }
        else
        {
            Logger.LogInformation($"{playerData.ToString()}");
        }
    }

    [ServerCommand(true)]
    [Description("Prints all properties available players")]
    public void _Properties()
    {
        Logger.LogInformation($"---Player properties are---", color: _commandColors);

        FieldInfo[] PlayerFields = typeof(Player).GetFields();

        foreach (var property in PlayerFields.Select(Fld => Fld.Name))
        {
            Logger.LogInformation($"{property}");
        }
    }

    //[ServerCommand(true)]
    //[Description("Enable/Disable enemy logging")]
    //public void _EnemyLog()
    //{
    //    server.EnemyLog = !server.EnemyLog;
    //    server.LogInfo(!server.EnemyLog ? "Deactivated enemy log" : "Activated enemy log", commandColors);
    //}

    //[ServerCommand(true)]
    //[AlternateName("_Test")]
    //[AlternateName("_Local")]
    //[Description("Enable/Disable local test")]
    //public void _LocalTest()
    //{
    //    server.isLocalTest = !server.isLocalTest;
    //    server.LogInfo(!server.isLocalTest ? "Deactivated local test" : "Activated local test", commandColors);
    //}

    [ServerCommand]
    [Description("Enable/Disable name tags")]
    public void NameTags()
    {
        DisplayNames = !DisplayNames;
        Logger.LogInformation(!DisplayNames ? "Deactivated Name Tags" : "Activated Name Tags",
            color: _commandColors);
    }

    [ServerCommand(true)]
    [Description("Change player's model")]
    [ExtraHelp("Usage: Model <player> <ModelFolder:ModelName>")]
    public void _Model(string playerNumber, string model)
    {
        byte PN = 0;

        if (!Byte.TryParse(playerNumber, out PN))
        {
            Logger.LogError("Parameter playerNumber should be an integer value");
            return;
        }

        if (ServerData.PlayerList.Count < PN || !ServerData.PlayerList[PN - 1].Connected)
        {
            Logger.LogInformation($"Player {PN} is not connected");
            return;
        }

        ModelDataDTO playerModel = ServerData.ModelData.PlayerModels[(byte)(PN - 1)];

        if (model.ToLower() == "link")
        {
            playerModel.ModelType = 0;
            playerModel.Model = "Jugador1ModelNameLongForASpecificReason";
        }
        else
        {
            playerModel.ModelType = 1;
            playerModel.Model = model;
        }

        ServerData.ModelData.AddModel((byte)(PN - 1), playerModel);

        Logger.LogInformation($"Player {PN} model set to {model}", color: _commandColors);
    }

    private int getDigitCount(int number)
    {
        return number.ToString().Length;
    }

    [ServerCommand]
    [Description("Return locations defined for prophunt")]
    [AlternateName("PHL")]
    public void PropHuntLocations()
    {
        for (int i = 0; i < this.ServerProphuntLocations.Count(); i++)
        {
            Logger.LogInformation($"{this.ServerProphuntLocations[i].Name}");
        }
    }

    [ServerCommand]
    [Description("Starts or stops a match of prophunt")]
    [ExtraHelp("Usage: PropHunt <state> <location>")]
    [ExtraHelp("<state>: start or stop")]
    [ExtraHelp(
        "<location>: location where prophunt is going to be played. Use PropHuntLocations to retrieve the existing locations for prophunt")]
    [AlternateName("PH")]
    public void PropHunt(string state = "", string location = "")
    {
        /*try
        {
            bool iState = this.GetProphuntState(state);

            if (!iState)
            {
                // Call prop hunt stop //
                ServerData.PropHuntData.Stop();
                return;
            }

            if (ServerData.PlayerList.Where(p => p.Connected).Count() < 2)
            {
                Logger.LogError("At least two players are necessary to activate Prop Hunt");
                return;
            }

            // Get prophunt location
            ProphuntLocation pLocation = this.GetLocation(location);

            // Call prop hunt start //
            ServerData.PropHuntData.Start(pLocation, 60);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
            Logger.LogWarning($"Usage: PropHunt <state> <location>");
            Logger.LogWarning("<state>: start or stop");
            Logger.LogWarning(
                "<location>: location where prophunt is going to be played. Use PropHuntLocations to retrieve the existing locations for prophunt");
            return;
        }*/
    }

    private bool GetProphuntState(string state)
    {
        if (state == "" || (state != "start" && state != "stop" && state != "on" && state != "off"))
            throw new Exception("State must be set.");

        return state == "start" || state == "on";
    }

    private ProphuntLocation GetLocation(string location)
    {
        if (location == "")
            throw new Exception("Location must be set.");

        if (Int32.TryParse(location, out int result))
        {
            if (this.ServerProphuntLocations.Count < result)
                throw new Exception("Invalid value for location");

            return this.ServerProphuntLocations[result];
        }

        if (!this.ServerProphuntLocations.Any(loc => loc.Name.ToLower() == location.ToLower()))
            throw new Exception("Location must be part of the list of locations.");

        return this.ServerProphuntLocations.Find(loc => loc.Name.ToLower() == location.ToLower());
    }
}