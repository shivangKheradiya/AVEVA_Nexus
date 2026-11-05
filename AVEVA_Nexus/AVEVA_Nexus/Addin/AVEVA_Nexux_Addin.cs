using Aveva.ApplicationFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AVEVA_Nexus.Http;
using System.Configuration;

namespace AVEVA_Nexus.Addin
{
    public class AVEVA_Nexux_Addin : IAddinInjected
    {
        public string Name => "AVEVA_Nexux_Addin";

        public string Description => "AVEVA Addin for providing HTTP Gateway for Connecting AVEVA systems, applications, and AI.";

        private static readonly object _lock = new object();
        private static bool _autoStarted;

        // If NEXUS_PORT is set in the environment of the E3D process, the HTTP
        // gateway starts automatically when the addin loads (no PML macro needed).
        private static void AutoStart()
        {
            string port = Environment.GetEnvironmentVariable("NEXUS_PORT");
            if (string.IsNullOrEmpty(port)) return;

            lock (_lock)
            {
                if (_autoStarted) return;
                _autoStarted = true;
            }

            try
            {
                SimpleHttpServer.Prefix = "http://+:" + port + "/";
                new AVEVA_Nexus.Config.HostConfig().Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("AVEVA_Nexux_Addin autostart failed: " + ex.Message);
            }
        }

        public void Start(IDependencyResolver resolver)
        {
            Console.WriteLine("AVEVA_Nexux_Addin Started");
            AutoStart();
        }

        public void Start(ServiceManager serviceManager)
        {
            Console.WriteLine("AVEVA_Nexux_Addin ServiceManager Started");
            AutoStart();
        }

        public void Stop()
        {
            Console.WriteLine("AVEVA_Nexux_Addin Stopped");
        }
    }
}
