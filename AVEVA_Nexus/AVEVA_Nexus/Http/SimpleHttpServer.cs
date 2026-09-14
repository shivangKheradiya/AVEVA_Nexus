using AVEVA_Nexus.Exceptions;
using AVEVA_Nexus.Logging;
using AVEVA_Nexus.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AVEVA_Nexus.Http
{
    public class SimpleHttpServer
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;

        public static string Prefix = "http://localhost:5000/";

        public void Start()
        {
            _cts = new CancellationTokenSource();

            _listener = new HttpListener();
            _listener.Prefixes.Add(Prefix);

            _listener.Start();

            Task.Run(() => ListenLoop());

            Console.WriteLine( "HTTP Server Started : " + Prefix);
        }

        public void Stop()
        {
            _cts?.Cancel();

            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
            }

            Console.WriteLine("HTTP Server Stopped");
        }

        private async Task ListenLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var context =
                        await _listener.GetContextAsync();

                    _ = Task.Run(
                        () => ProcessRequest(context));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private async Task ProcessRequest(
            HttpListenerContext context)
        {
            try
            {
                Console.WriteLine("========================================");
                Console.WriteLine("HTTP REQUEST RECEIVED");
                Console.WriteLine("========================================");

                Console.WriteLine($"RawUrl       : {context.Request.RawUrl}");
                Console.WriteLine($"AbsolutePath : {context.Request.Url.AbsolutePath}");
                Console.WriteLine($"HttpMethod   : {context.Request.HttpMethod}");
                Console.WriteLine($"Url          : {context.Request.Url}");

                string path =
                    context.Request.Url.AbsolutePath;

                NexusLogger.Info(
                    $"{context.Request.HttpMethod} {path}");

                var router = new Router();

                string body = await ReadBody( context.Request);

                var routeContext = new RouteContext
                {
                    Method = context.Request.HttpMethod,
                    Path = context.Request.Url.AbsolutePath,
                    Query = context.Request.QueryString,
                    Body = body,
                };

                var result = router.Route(routeContext);

                await WriteResponse(
                    context,
                    200,
                    result);
            }
            catch (NexusException ex)
            {
                NexusLogger.Error(ex);

                await WriteResponse(
                    context,
                    ex.StatusCode,
                    new ApiResponse
                    {
                        Success = false,
                        Error = ex.Message
                    });
            }
            catch (Exception ex)
            {
                NexusLogger.Error(ex);

                await WriteResponse(
                    context,
                    500,
                    new ApiResponse
                    {
                        Success = false,
                        Error = "Internal Server Error"
                    });
            }
        }

        private async Task WriteResponse(
            HttpListenerContext context,
            int statusCode,
            object payload)
        {
            string json =
                System.Text.Json.JsonSerializer.Serialize(
                    payload,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

            byte[] buffer =
                Encoding.UTF8.GetBytes(json);

            context.Response.StatusCode =
                statusCode;

            context.Response.ContentType =
                "application/json";

            context.Response.ContentLength64 =
                buffer.Length;

            await context.Response.OutputStream
                .WriteAsync(
                    buffer,
                    0,
                    buffer.Length);

            context.Response.Close();
        }

        private async Task<string> ReadBody(
            HttpListenerRequest request)
        {
            using (var reader =
                new StreamReader(
                    request.InputStream,
                    request.ContentEncoding))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}