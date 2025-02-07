using System.Net;
using System.Net.Sockets;
using BOTWM.Library.JSONBuilder;
using BOTWM.Logging;
using BOTWM.Server.Packets;

namespace BOTWM.Server
{
    public class Host
    {
        public delegate void NetEvent(Peer peer, BasePacket packet);
        private int BUFFER_SIZE = 10240;
        
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private Task _listenThread;
        private List<Thread> _peerThreads;
        private Socket _hostSocket;
        private string _ipAddress;
        private int _port;
        private List<Peer> _peers;
        
        
        public event EventHandler<Peer> OnPeerConnect;
        public event NetEvent OnPeerReceive;
        public event EventHandler<Peer> OnPeerDisconnect;

        public void Initialize(string ip, int port)
        {
            _cts = new CancellationTokenSource();
            _ipAddress = ip;
            _port = port;
            _peerThreads = [];
            _peers = [];

            try
            {
                _hostSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                _hostSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                var ipEndPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);
                _hostSocket.Bind(ipEndPoint);
                Logger.LogInformation($"Server opened on: {_ipAddress}:{_port}");
                _isRunning = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
            }
            
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

        public void Start()
        {
            //create main network thread
            _listenThread = new Task(Listen);
            _listenThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _hostSocket.Close();
            _cts.Cancel();
        }

        private void Listen()
        {
            try
            {
                while (_isRunning)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    _hostSocket.Listen(100);
                    var socket = _hostSocket.Accept();
                    OnConnect(socket);
                }
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                {
                    // ignore
                }
                else
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
        
        

        private void OnConnect(Socket socket)
        {
            var peer = new Peer()
            {
                Gamemode = "",
                Socket = socket,
                Token = _cts.Token,
            };

            peer.OnReceiveSuccess += OnReceive;
            
            //create new client thread
            try
            {
                var peerThread = new Thread(() => peer.Handle());
                peerThread.Start();
                _peerThreads.Add(peerThread);
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
            OnPeerConnect?.Invoke(this, peer);
        }

        private void OnReceive(Peer peer, byte[] bytes)
        {
            var pkt = BasePacket.Create(bytes);
            OnPeerReceive?.Invoke(peer, pkt);
        }
        
        private void OnDisconnect(Peer peer)
        {
            OnPeerDisconnect?.Invoke(this, peer);
        }
    }
}