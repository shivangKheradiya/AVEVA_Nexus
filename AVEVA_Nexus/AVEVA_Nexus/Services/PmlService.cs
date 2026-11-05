using AVEVA_Nexus.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AVEVA_Nexus.Integration;

namespace AVEVA_Nexus.Services
{
    public class PmlService
    {
        private readonly IAvevaProvider _provider;

        public PmlService()
        {
            _provider = new AvevaProvider();
        }

        public bool Execute(string command)
        {
            return _provider.ExecuteCommand(command);
        }

        public string GetString(string name)
        {
            return _provider.GetStringVariable(PmlInputValidator.VariableName(name));
        }

        public double GetReal(string name)
        {
            return _provider.GetRealVariable(PmlInputValidator.VariableName(name));
        }

        public bool GetBoolean(string name)
        {
            return _provider.GetBooleanVariable(PmlInputValidator.VariableName(name));
        }

        public object[] GetArray(string name)
        {
            return _provider.GetArrayVariable(PmlInputValidator.VariableName(name));
        }
    }
}