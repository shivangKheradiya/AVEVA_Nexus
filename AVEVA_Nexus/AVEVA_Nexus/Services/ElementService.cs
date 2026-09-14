using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AVEVA_Nexus.Integration;

namespace AVEVA_Nexus.Services
{
    public class ElementService
    {
        private readonly IAvevaProvider _provider;

        public ElementService()
        {
            _provider = new AvevaProvider();
        }

        public object GetAttribute(
            string dbref,
            string attribute)
        {
            return _provider.GetAttribute(
                dbref,
                attribute);
        }
    }
}
