using BOTWM.Server.ServerClasses;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using BOTWM.Library.DTO;
using BOTWM.Library.HelperTypes;
using BOTWM.Library.JSONBuilder;
using BOTWM.Library.Settings;
using BOTWM.Logging;
using BOTWM.Server.Packets;

namespace BOTWM.Server
{
    public class Host
    {
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

        bool serverOpen = false;
        
        Thread? listenThread;
        List<Thread> clientThreads = new List<Thread>();
        
        Socket socket;
        private string IpAddress;
        private int Port;
        private string Password;
        private string Description;
        ServerSettings Settings;
        int BUFFER_SIZE = 10240;


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
                socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

                var ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAddress), Port);
                socket.Bind(ipEndPoint);

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
            //create main network thread
            listenThread = new Thread(Listen)
            {
                IsBackground = true
            };
            listenThread.Start();
        }

        public void Stop()
        {
            serverOpen = false;
            socket.Close();

            //TODO: implement cancelation tokens
            listenThread?.Interrupt();

            foreach (var clientThread in clientThreads)
            {
                clientThread.Interrupt();
            }
        }

        private void Listen()
        {
            while (true)
            {
                socket.Listen(100);

                var clientConnection = socket.Accept();

                //create new client thread
                var clientThread = new Thread(() => HandleClient(clientConnection));
                clientThread.Start();
                clientThreads.Add(clientThread);
            }

            foreach (var thread in clientThreads)
            {
                thread?.Interrupt();
            }
        }

        private bool TryReceive(Socket clientConnection, byte[] buffer)
        {
            var retries = 0;
            var stopwatch = new Stopwatch();
            var totalLength = 0;

            stopwatch.Start();
            
            while (true)
            {
                if (retries > 10)
                {
                    Logger.LogWarning("Failed to receive data from client...");
                    return false;
                }
                
                totalLength += clientConnection.Receive(buffer, 0, buffer.Length, 0);
                    
                if (totalLength < 6144)
                {
                    retries++;
                    
                    continue;
                }

                break;
            }

            if (retries > 0)
            {
                Logger.LogInformation($"[Client Retried {retries} times and took {stopwatch.ElapsedMilliseconds} milliseconds");
            }
            
            return true;
        }
        
        private void HandleClient(Socket connection)
        {
            var clientConnected = true;
            var playerNumber = -1;
            var playerName = "";
            Tuple<MessageTypes, object>? clientMessage = null;

            while (serverOpen && clientConnected)
            {
                var buffer = new byte[BUFFER_SIZE];
                
                try
                {
                    if (!TryReceive(connection, buffer))
                    {
                        break;
                    }
                    
                    var pkt = BasePacket.Create(buffer);

                    switch (pkt)
                    {
                        case PingPacket ping:
                            clientMessage = new Tuple<MessageTypes, object>(MessageTypes.Ping, ping.Password);
                            break;
                        case ConnectPacket connect:
                            clientMessage = new Tuple<MessageTypes, object>(MessageTypes.Connect, connect.ConnectDTO);
                            break;
                        case DisconnectPacket disconnect:
                            clientMessage = new Tuple<MessageTypes, object>(MessageTypes.Disconnect, disconnect.Reason);
                            break;
                        case UpdatePacket update:
                            clientMessage = new JsonBuilder().BuildFromBytes(buffer); ;//new Tuple<MessageTypes, object>(MessageTypes.Update, update.ClientDto);
                            break;
                        default: 
                            continue;
                    }
                }
                catch (Exception ex)
                {
                    //Logger.LogInformation($"Player {ServerData.GetPlayer(PlayerNumber).Name} disconnected.", ex.Message);
                    Logger.LogDebug(ex.StackTrace);
                    //ServerData.SetConnection(PlayerNumber, false);
                    connection.Close();
                    clientConnected = false;
                }

                if (clientMessage is null)
                {
                    return;
                }

                try
                {
                    switch (clientMessage.Item1)
                    {
                        case MessageTypes.Error:
                            throw new Exception($"[{playerName}] Error receiving message. Disconnecting player...");
                        case MessageTypes.Ping:
                            PingDTO pingResult;

                            if (ServerData.Configuration.PASSWORD != (string)clientMessage.Item2)
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
                                    GameMode = this.Gamemode,
                                    PlayerLimit = 32
                                };
                            }
                            connection.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(pingResult)));
                            connection.Close();
                            clientConnected = false;
                            break;
                        case MessageTypes.Connect:
                            var userConfiguration = (ConnectDTO)clientMessage.Item2;
                            var assignationResult = ServerData.TryAssigning(userConfiguration);

                            if (assignationResult.Response != 1)
                            {
                                connection.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(assignationResult)));
                                connection.Close();
                                clientConnected = false;
                                Logger.LogInformation($"Player {userConfiguration.Name} tried to connect but failed with error {assignationResult.Response}");
                                break;
                            }

                            playerNumber = assignationResult.PlayerNumber;
                            playerName = ServerData.PlayerList[playerNumber].Name;
                            connection.Send(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(assignationResult)));

                            Logger.LogInformation($"Player {userConfiguration.Name} joined the server. Assigned to player {assignationResult.PlayerNumber + 1}.");
                            break;
                        case MessageTypes.Update:
                            ServerData.SetConnection(playerNumber, true);

                            var userInformation = (ClientDTO)clientMessage.Item2;

                            ServerData.UpdateWorldData(userInformation.WorldData, playerNumber);
                            ServerData.UpdatePlayerData(userInformation.PlayerData, playerNumber);
                            ServerData.UpdateEnemyData(userInformation.EnemyData);
                            ServerData.UpdateQuestData(userInformation.QuestData);

                            var serverDto = ServerData.GetData(playerNumber);
                            serverDto.NetworkData.Map(this);

                            connection.Send(new JsonBuilder().BuildArrayOfBytes(serverDto));

                            ServerData.ClearDeathSwap(playerNumber);
                            break;
                        case MessageTypes.Disconnect:
                            Logger.LogInformation($"Player {ServerData.GetPlayer(playerNumber).Name} disconnected. {(string)clientMessage.Item2}");
                            connection.Close();
                            clientConnected = false;
                            ServerData.SetConnection(playerNumber, false);
                            break;
                        default: break;
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
}