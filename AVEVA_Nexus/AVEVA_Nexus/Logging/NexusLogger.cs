using System;

namespace AVEVA_Nexus.Logging
{
    public static class NexusLogger
    {
        public static void Info(string message)
        {
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] [INFO] {message}");
        }

        public static void Error(Exception ex)
        {
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] [ERROR] {ex}");
        }
    }
}