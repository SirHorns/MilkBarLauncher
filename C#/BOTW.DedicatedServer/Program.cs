using BOTW.Logging;
using BOTWM.DedicatedServer;

try
{
    Console.WriteLine("***************************************************************");
    Console.WriteLine("*                                                             *");
    Console.WriteLine("*             Milk Bar Launcher Dedicated Server              *");
    Console.WriteLine("*                                                             *");
    Console.WriteLine("***************************************************************\n");

    Console.Write("VERSION: ");
#if (DEBUG)
    Console.WriteLine("DEV");
#else
    Console.WriteLine("DEV 2.0");
#endif

    Console.WriteLine();

    DedicatedServer DedicatedServer = new DedicatedServer();
    Logger.Start(Logger.LogLevelEnum.DEBUG, Logger.LogLevelEnum.WARNING);

    DedicatedServer.CopyAppdataFiles();

    DedicatedServer.setupCommands();

    DedicatedServer.setup();

    while (true)
    {
        string input = Logger.LogInput("");
        DedicatedServer.process_commands(input);
    }
}
catch (Exception e)
{
    Logger.LogCritical(e.ToString());
    Logger.LogInput("Press any key to continue.");
}