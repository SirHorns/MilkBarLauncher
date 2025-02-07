using BOTWM.Library.Settings;

namespace BOTWM.Server.ServerClasses;

public struct ServerConfiguration
{
    public string IP;
    public int PORT;
    public string PASSWORD;
    public string DESCRIPTION;
    public ServerSettings Settings;

    public ServerConfiguration(string ip, int port, string password, string description, ServerSettings settings)
    {
        this.IP = ip;
        this.PORT = port;
        this.PASSWORD = password;
        this.DESCRIPTION = description;
        this.Settings = settings;
    }
}