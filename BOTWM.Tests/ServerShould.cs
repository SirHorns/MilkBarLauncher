using BOTWM.Server;
using System.Net;
using System.Net.Sockets;

namespace BOTWM.Tests
{
    public class ServerShould
    {

        public ServerShould()
        {

            IPEndPoint íp = new IPEndPoint(IPAddress.Parse("25.79.17.118"), 5050);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Connect(íp);

        }

        [Fact]
        public void Test1()
        {

            

        }
    }
}