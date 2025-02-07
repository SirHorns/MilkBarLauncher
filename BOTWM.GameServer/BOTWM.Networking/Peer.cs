using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using BOTWM.Library.DTO;
using BOTWM.Library.HelperTypes;
using BOTWM.Library.JSONBuilder;
using BOTWM.Logging;
using BOTWM.Server.Packets;
using BOTWM.Server.ServerClasses;
using Newtonsoft.Json;

namespace BOTWM.Server;

public class Peer
{
    public delegate void PeerNetEvent(Peer peer, byte[] bytes);
    int BUFFER_SIZE = 10240;
    
    public Socket Socket;
    public CancellationToken Token;
    public bool Connected;
    public int PlayerNumber = -1;
    public string PlayerName = "";
    public string Gamemode = "";
    
    
    public event PeerNetEvent OnReceive;
    public event PeerNetEvent OnReceiveSuccess;
    public event PeerNetEvent OnReceiveFailed;

    internal void Handle()
    {
        Connected = true;
        while (Connected)
        {
            Token.ThrowIfCancellationRequested();

            var buffer = new byte[BUFFER_SIZE];
            
            try
            {
                if (!TryReceive(buffer))
                {
                    ReceiveFailed();
                    break;
                }

                ReceiveSuccess(buffer);
            }
            catch (Exception ex)
            {
                //Logger.LogInformation($"Player {ServerData.GetPlayer(PlayerNumber).Name} disconnected.", ex.Message);
                Logger.LogDebug(ex.StackTrace);
                //ServerData.SetConnection(PlayerNumber, false);
                Socket.Close();
                Connected = false;
            }

           
        }
    }
    
    private bool TryReceive(byte[] buffer)
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
                
            totalLength += Socket.Receive(buffer, 0, buffer.Length, 0);
                    
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

    public void Send(byte[] bytes)
    {
        Socket.Send(bytes);
    }
    
    private void ReceiveSuccess(byte[] bytes)
    {
        OnReceiveSuccess?.Invoke(this, bytes);
    }
    private void ReceiveFailed()
    {
        OnReceiveFailed?.Invoke(this, null);
    }
}