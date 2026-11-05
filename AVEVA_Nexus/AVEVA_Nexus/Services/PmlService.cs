using AVEVA_Nexus.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AVEVA_Nexus.Exceptions;
using AVEVA_Nexus.Integration;
using AVEVA_Nexus.Models;

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

        private const int MaxScriptLines = 500;

        public RunResult Run(RunRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Script))
            {
                throw new NexusException("script is required", 400);
            }

            List<string> lines = request.Script.Replace("\r", "").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToList();
            if (lines.Count > MaxScriptLines)
            {
                throw new NexusException("script too long (max " + MaxScriptLines + " lines)", 400);
            }

            List<string> names = (request.Return ?? new List<string>())
                .Select(PmlInputValidator.VariableName)
                .Distinct()
                .ToList();

            return _provider.RunScript(lines, names);
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