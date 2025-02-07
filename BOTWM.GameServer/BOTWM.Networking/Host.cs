using System.Net;
using System.Net.Sockets;
using BOTWM.Library.JSONBuilder;
using BOTWM.Logging;

namespace BOTWM.Server
{
    public class Host
    {
        public delegate void NetEvent(Peer peer, Tuple<PacketTypes, object>? request);
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        
        bool IsRunning = false;
        Thread? listenThread;
        List<Thread> peerThreads = [];
        Socket HostSocket;
        private string IpAddress;
        private int Port;
        int BUFFER_SIZE = 10240;
        private List<Peer> Peers = [];
        
        public event NetEvent OnReceive;

        public void Initialize(string ip, int port)
        {
            IpAddress = ip;
            Port = port;

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
                HostSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                HostSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

                var ipEndPoint = new IPEndPoint(IPAddress.Parse(IpAddress), Port);
                HostSocket.Bind(ipEndPoint);

                Logger.LogInformation($"Server opened on: {IpAddress}:{Port}");

                

                IsRunning = true;
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
            IsRunning = false;
            HostSocket.Close();

            //TODO: implement cancelation tokens
            listenThread?.Interrupt();
            
            _cts.Cancel();
        }

        private void Listen()
        {
            while (IsRunning)
            {
                HostSocket.Listen(100);

                var peerSocket = HostSocket.Accept();

                var peer = new Peer()
                {
                    Gamemode = "",
                    Socket = peerSocket,
                    Token = _cts.Token,
                };

                peer.OnReceiveSuccess += PeerEvent;

                //create new client thread
                try
                {
                    var peerThread = new Thread(() => peer.Handle());
                    peerThread.Start();
                    peerThreads.Add(peerThread);
                }
                catch (Exception e)
                {
                    if (e is OperationCanceledException)
                    {
                        //ignore
                    }
                    else
                    {
                        Console.WriteLine(e);
                        throw; 
                    }
                }
                
            }
        }

        private void PeerEvent(Peer peer, Tuple<PacketTypes, object>? request)
        {
            OnReceive?.Invoke(peer, request);
        }
    }
}