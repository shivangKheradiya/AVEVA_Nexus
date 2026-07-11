using Aveva.Core.PMLNet;
using AVEVA_Nexus.Addin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using AVEVA_Nexus.Http;

namespace AVEVA_Nexus.Config
{
    [PMLNetCallable()]
    public class HostConfig
    {
        public static SimpleHttpServer _host = new SimpleHttpServer();

        [PMLNetCallable()]
        public HostConfig() { }

        [PMLNetCallable()]
        public void Assign(HostConfig that) { }

        [PMLNetCallable()]
        public void SetPrefix(string prefix = "http://localhost:5000/") {
            SimpleHttpServer.Prefix = prefix;
        }

        [PMLNetCallable()]
        public void Start()
        {
            _host.Start();
        }

        [PMLNetCallable()]
        public void Stop()
        {
            _host?.Start();
        }
    }
}
