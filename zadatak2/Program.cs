using System;
using System.Net;
using System.Threading;
using System.IO;
using DotNetEnv;

namespace zadatak2
{
    class Program
    {
        private static int Port;
        private static readonly int BufferSize = 10;
        private static readonly int WorkerThreads = 4;

        private static RequestBuffer _buffer;
        private static SearchCache _cache;
        private static FileSearcher _searcher;

        static void Main(string[] args)
        {
            Env.Load();
            string envPort = Environment.GetEnvironmentVariable("PORT");
            if (!int.TryParse(envPort, out Port))
            {
                Port = 5050;
            }
            string rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "root");
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
            }

            _buffer = new RequestBuffer(BufferSize);
            _cache = new SearchCache();
            _searcher = new FileSearcher(rootPath);

            for (int i = 0; i < WorkerThreads; i++)
            {
                ThreadPool.QueueUserWorkItem(state => ConsumerLoop());
            }

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{Port}/");

            try
            {
                listener.Start();
                ThreadSafeLogger.Log($"Server pokrenut na portu {Port}...");
                ThreadSafeLogger.Log($"Root direktorijum: {rootPath}");

                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    ThreadSafeLogger.Log($"Pristigao zahtev: {context.Request.Url.AbsolutePath}");

                    _buffer.Add(context);
                }
            }
            catch (Exception ex)
            {
                ThreadSafeLogger.LogError($"Greška u radu servera: {ex.Message}");
            }
            finally
            {
                listener.Stop();
            }
        }

        private static void ConsumerLoop()
        {
            while (true)
            {
                HttpListenerContext context = _buffer.Remove();

                try
                {
                    ProcessRequest(context);
                }
                catch (Exception ex)
                {
                    ThreadSafeLogger.LogError($"Greška pri obradi zahteva: {ex.Message}");
                }
            }
        }

        private static void ProcessRequest(HttpListenerContext context)
        {
            string query = context.Request.Url.AbsolutePath.TrimStart('/');

            if (string.IsNullOrEmpty(query) || query == "favicon.ico")
            {
                context.Response.Close();
                return;
            }

            ThreadSafeLogger.Log($"Obrada zapoceta za upit: {query}");

            string htmlResponse = _cache.GetOrAdd(query, () =>
            {
                return _searcher.Search(query);
            });

            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(htmlResponse);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.OutputStream.Close();

            ThreadSafeLogger.Log($"Obrada zavrsena za upit: {query}");
        }
    }
}