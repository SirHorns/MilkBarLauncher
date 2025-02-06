using BOTWM.Server.ServerClasses;
using Newtonsoft.Json;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using BOTWM.Library;
using BOTWM.Library.DTO;
using BOTWM.Library.HelperTypes;
using BOTWM.Library.JSONBuilder;
using BOTWM.Library.Settings;
using BOTWM.Logging;

namespace BOTWM.Server
{
    public class Host
    {
        bool serverOpen = false;

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
        public string Gamemode = "";

        public bool EnemyLog { get; set; }

        public int ClientLog { get; set; }
        public bool ServerLog { get; set; }

        Socket listen;
        Thread? listenThread;
        List<Thread> clientThreads = new List<Thread>();
        private string IpAddress;
        private int Port;
        private string Password;
        private string Description; 
        ServerSettings Settings;
        
        
        public void Initialize(string ip, int port, string password, string description, ServerSettings settings)
        {

            Gamemode = settings.SettingsName;
            IpAddress = ip;
            Port = port;
            Password = password;
            Description = description;
            Settings = settings;
            
            /*
            var ipAddresses = new Dictionary<string, string>();

            if (ip == "localhost")
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());

                foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    var uips = networkInterface.GetIPProperties().UnicastAddresses;
                    foreach (var uip in uips)
                    {
                        if (uip.Address.AddressFamily == AddressFamily.InterNetwork && host.AddressList.Contains(uip.Address))
                        {
                            ipAddresses.Add(networkInterface.Name, uip.Address.ToString());
                        }
                    }
                }
            }
            else
            {
                ipAddresses.Add("custom IP", ip);
            }

            foreach (var key in ipAddresses.Keys)
            {
                try
                {
                    listen = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    listen.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                    var ipAddress = ipAddresses[key];

                    var ipEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                    listen.Bind(ipEndPoint);

                    Logger.LogInformation("Server opened on " + key + ".");

                    ServerData.Startup(ip, port, password, description, settings);

                    serverOpen = true;

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex.ToString());
                    continue;
                }
            }

            return false;*/
        }

        public bool Bind()
        {
            try
            {
                listen = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                listen.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

                var ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAddress), Port);
                listen.Bind(ipEndPoint);

                Logger.LogInformation($"Server opened on: {IpAddress}:{Port}");

                ServerData.Startup(IpAddress, Port, Password, Description, Settings);

                serverOpen = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
            return false;
        }

        public void Start()
        {
            listenThread = new Thread(Listen)
            {
                IsBackground = true
            };
            listenThread.Start();
        }
        
        public void Stop()
        {
            serverOpen = false;
            listen.Close();

            //TODO: implement cancelation tokens
            listenThread?.Interrupt();

            foreach (var clientThread in clientThreads)
            {
                clientThread.Interrupt();
            }
        }
        
        private void Listen()
        {
            while(true)
            {
                Socket connection;

                listen.Listen(100);

                connection = listen.Accept();

                var clientThread = new Thread(() => HandleClient(connection));
                clientThread.Start();
                clientThreads.Add(clientThread);
            }
        }

        private void HandleClient(Socket connection)
        {
            int SIZE = 10240;

            byte[] info = new byte[SIZE];
            List<byte> data = new List<byte>();
            int totalLength = 0;
            int retries = 0;
            Stopwatch RetryWatch = new Stopwatch();

            bool ClientConnected = true;
            int PlayerNumber = -1;
            string PlayerName = "";
            
            while(serverOpen && ClientConnected)
            {
                try
                {
                    if (retries == 0)
                        RetryWatch.Restart();

                    totalLength += connection.Receive(info, 0, info.Length, 0);

                    data.AddRange(info.ToList());

                    if (totalLength < 6144)
                    {
                        retries++;

                        if (retries > 10 && totalLength == 0)
                            throw new ApplicationException("Connection lost with player.");

                        continue;
                    }

                    Tuple<MessageType, object> ClientMessage = new JSONBuilder().BuildFromBytes(data.ToArray());

                    data.Clear();
                    totalLength = 0;

                    if(retries > 0)
                    {
                        Logger.LogInformation($"[{PlayerName}] Retried {retries} times and took {RetryWatch.ElapsedMilliseconds} milliseconds");
                        retries = 0;
                    }

                    if (ClientMessage.Item1 == MessageType.error)
                    {
                        throw new Exception($"[{PlayerName}] Error receiving message. Disconnecting player...");
                    }
                    else if(ClientMessage.Item1 == MessageType.ping)
                    {
                        var PingResult = new PingDTO();

                        if(ServerData.Configuration.PASSWORD != (string)ClientMessage.Item2)
                            PingResult = new PingDTO()
                            {
                                CorrectPassword = false,
                                Description = "",
                                PlayerList = new NamesDTO(),
                                GameMode = "",
                                PlayerLimit = 32
                            };
                        else
                            PingResult = new PingDTO()
                            {
                                CorrectPassword = true,
                                Description = ServerData.Configuration.DESCRIPTION,
                                PlayerList = ServerData.GetPlayers(),
                                GameMode = this.Gamemode,
                                PlayerLimit = 32
                            };

                        connection.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(PingResult)));

                        connection.Close();
                        ClientConnected = false;

                    }
                    else if(ClientMessage.Item1 == MessageType.connect)
                    {
                        ConnectDTO UserConfiguration = (ConnectDTO)ClientMessage.Item2;
                        ConnectResponseDTO AssignationResult = ServerData.TryAssigning(UserConfiguration);

                        if(AssignationResult.Response != 1)
                        {
                            connection.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(AssignationResult)));
                            connection.Close();
                            ClientConnected = false;
                            Logger.LogInformation($"Player {UserConfiguration.Name} tried to connect but failed with error {AssignationResult.Response}");
                            break;
                        }

                        PlayerNumber = AssignationResult.PlayerNumber;
                        PlayerName = ServerData.PlayerList[PlayerNumber].Name;
                        connection.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(AssignationResult)));

                        Logger.LogInformation($"Player {UserConfiguration.Name} joined the server. Assigned to player {AssignationResult.PlayerNumber + 1}.");
                    }
                    else if(ClientMessage.Item1 == MessageType.update)
                    {
                        ServerData.SetConnection(PlayerNumber, true);

                        ClientDTO UserInformation = (ClientDTO)ClientMessage.Item2;

                        ServerData.UpdateWorldData(UserInformation.WorldData, PlayerNumber);
                        ServerData.UpdatePlayerData(UserInformation.PlayerData, PlayerNumber);
                        ServerData.UpdateEnemyData(UserInformation.EnemyData);
                        ServerData.UpdateQuestData(UserInformation.QuestData);

                        ServerDTO serverDTO = ServerData.GetData(PlayerNumber);
                        serverDTO.NetworkData.Map(this);

                        connection.Send(new JSONBuilder().BuildArrayOfBytes(serverDTO));

                        ServerData.ClearDeathSwap(PlayerNumber);
                    }
                    else if(ClientMessage.Item1 == MessageType.disconnect)
                    {
                        Logger.LogInformation($"Player {ServerData.GetPlayer(PlayerNumber).Name} disconnected. {(string)ClientMessage.Item2}");
                        connection.Close();
                        ClientConnected = false;
                        ServerData.SetConnection(PlayerNumber, false);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogInformation($"Player {ServerData.GetPlayer(PlayerNumber).Name} disconnected.", ex.Message);
                    Logger.LogDebug(ex.StackTrace);
                    ServerData.SetConnection(PlayerNumber, false);
                    connection.Close();
                    ClientConnected = false;
                }
            }
        }
    }
}
