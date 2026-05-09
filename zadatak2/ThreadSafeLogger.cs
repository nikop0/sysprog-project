using System;

namespace zadatak2
{
    internal static class ThreadSafeLogger
    {
        private static readonly object _lock = new object();
        public static void Log(string message)
        {
            lock (_lock)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                Console.WriteLine($"{timestamp} Thread {Environment.CurrentManagedThreadId} {message}");
            }
        }

        public static void LogError(string error)
        {
            lock (_lock)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                Console.WriteLine($"{timestamp} ERROR Thread {Environment.CurrentManagedThreadId} {error}");
            }
        }
    }
}