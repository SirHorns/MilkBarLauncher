namespace BOTWM.Logging
{
    public static class Logger
    {
        private static LogLevelEnum CMDLevel;
        private static LogLevelEnum LogLevel;

        private static string CurrentDirectory = Directory.GetCurrentDirectory();
        private static string LogPath = $"{CurrentDirectory}\\LatestLog.txt";
        private static string LogsFolder = $"{CurrentDirectory}\\Logs";

        private static Mutex logMutex = new Mutex();

        public static void Start(LogLevelEnum logLevel, LogLevelEnum cmdLevel)
        {
            LogLevel = logLevel;
            CMDLevel = cmdLevel;

            SetupLogFile();
        }

        public static void LogCritical(string message, string details = "", ConsoleColor color = ConsoleColor.DarkRed) => WriteToLog(LogWriteLevelEnum.CRT, message, details, color);
        public static void LogError(string message, string details = "", ConsoleColor color = ConsoleColor.DarkRed) => WriteToLog(LogWriteLevelEnum.ERR, message, details, color);
        public static void LogInformation(string message, string details = "", ConsoleColor color = ConsoleColor.White) => WriteToLog(LogWriteLevelEnum.INF, message, details, color);
        public static void LogWarning(string message, string details = "", ConsoleColor color = ConsoleColor.DarkYellow) => WriteToLog(LogWriteLevelEnum.WRN, message, details, color);
        public static void LogDebug(string message, string details = "", ConsoleColor color = ConsoleColor.Magenta) => WriteToLog(LogWriteLevelEnum.DBG, message, details, color);

        public static string LogInput(string message)
        {
            if(!string.IsNullOrEmpty(message))
                WriteToLog(LogWriteLevelEnum.INF, message, "", ConsoleColor.White, false);

            string userInput = Console.ReadLine();

            using (StreamWriter LogFile = new StreamWriter(LogPath, true))
            {
                LogFile.WriteLine(userInput);
            }

            return userInput;
        }

        private static void SetupLogFile()
        {
            if(File.Exists(LogPath))
            {
                string creationTime = File.GetCreationTime(LogPath).ToString("yyyy-MM-dd, HH-mm-ss");

                if (!Directory.Exists(LogsFolder))
                    Directory.CreateDirectory(LogsFolder);

                File.Move(LogPath, $"{LogsFolder}\\{creationTime}.txt");
            }

            File.CreateText(LogPath);
        }

        private static void WriteToLog(LogWriteLevelEnum writeLevel, string message, string details, ConsoleColor color, bool newLine = true, bool writeToCMD = true)
        {
            string MessageTime = DateTime.Now.ToString("HH:mm:ss");

            if(writeToCMD)
                WriteToConsole(writeLevel, message, MessageTime, color, newLine);

            if ((int)writeLevel > (int)LogLevel)
                return;

            logMutex.WaitOne(100);

            using (StreamWriter LogFile = new StreamWriter(LogPath, true))
            {
                LogFile.Write($"[{MessageTime}] {message}");

                if (!string.IsNullOrEmpty(details))
                    LogFile.Write($" - Details: {details}");

                if(newLine)
                    LogFile.WriteLine();
            }

            logMutex.ReleaseMutex();
        }

        private static void WriteToConsole(LogWriteLevelEnum writeLevel, string message, string datetime, ConsoleColor color, bool newLine)
        {
            if ((int)writeLevel > (int)CMDLevel)
                return;

            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Write($"[{datetime}] ");

            Console.ForegroundColor = color;

            if (newLine)
                Console.WriteLine(message);
            else
                Console.Write(message);


            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}