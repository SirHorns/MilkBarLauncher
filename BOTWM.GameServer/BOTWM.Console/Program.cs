using BOTWM.Logging;

namespace BOTWM.Console;

public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            System.Console.WriteLine("***************************************************************");
            System.Console.WriteLine("*                                                             *");
            System.Console.WriteLine("*             Milk Bar Launcher Dedicated Server              *");
            System.Console.WriteLine("*                                                             *");
            System.Console.WriteLine("***************************************************************\n");

            var dedicatedServer = new DedicatedServer.DedicatedServer();
            Logger.Start(LogLevelEnum.DEBUG, LogLevelEnum.WARNING);

            

            dedicatedServer.Initialize();
    
            dedicatedServer.Run();

            while (true)
            {
                var input = Logger.LogInput("");
                dedicatedServer.process_commands(input);
            }
        }
        catch (Exception e)
        {
            Logger.LogCritical(e.ToString());
            Logger.LogInput("Press any key to continue.");
        }
    }
}