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

        public void Start(IDependencyResolver resolver)
        {
            Console.WriteLine("AVEVA_Nexux_Addin Started");
        }

        public void Start(ServiceManager serviceManager)
        {
            Console.WriteLine("AVEVA_Nexux_Addin ServiceManager Started");
        }

        public void Stop()
        {
            Console.WriteLine("AVEVA_Nexux_Addin Stopped");
        }
    }
}
