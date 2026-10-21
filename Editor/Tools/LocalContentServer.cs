using System;
using System.IO;
using System.Net;
using System.Threading;
using UnityEditor;

namespace AddrKit.Editor
{
    /// <summary>
    /// A tiny static file server for testing remote content without any hosting.
    /// Serves the ServerData folder at http://localhost:{port}/.
    /// </summary>
    [InitializeOnLoad]
    public static class LocalContentServer
    {
        static HttpListener listener;
        static Thread thread;

        public static bool IsRunning => listener != null && listener.IsListening;
        public static int Port { get; private set; }

        static LocalContentServer()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;
        }

        public static void Start(string folder, int port)
        {
            Stop();
            Port = port;
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();

            string root = Path.GetFullPath(folder);
            thread = new Thread(() => Serve(listener, root)) { IsBackground = true };
            thread.Start();
        }

        public static void Stop()
        {
            try { listener?.Stop(); listener?.Close(); }
            catch (ObjectDisposedException) { }
            listener = null;
        }

        static void Serve(HttpListener server, string root)
        {
            while (server.IsListening)
            {
                HttpListenerContext context;
                try { context = server.GetContext(); }
                catch (Exception) { return; }   // listener stopped

                try
                {
                    string relative = Uri.UnescapeDataString(context.Request.Url.AbsolutePath).TrimStart('/');
                    string file = Path.GetFullPath(Path.Combine(root, relative));

                    // Stay inside the served folder.
                    if (!file.StartsWith(root) || !File.Exists(file))
                    {
                        context.Response.StatusCode = 404;
                    }
                    else
                    {
                        byte[] bytes = File.ReadAllBytes(file);
                        context.Response.ContentType = "application/octet-stream";
                        context.Response.ContentLength64 = bytes.Length;
                        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception) { context.Response.StatusCode = 500; }
                finally { context.Response.Close(); }
            }
        }
    }
}
