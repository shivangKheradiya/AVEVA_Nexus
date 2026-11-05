using AVEVA_Nexus.Exceptions;
using AVEVA_Nexus.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using ac = Aveva.Core.Utilities.CommandLine;
using Aveva.Core.Database;

namespace AVEVA_Nexus.Integration
{
    public class AvevaProvider : IAvevaProvider
    {
        private ac.Command getCommand = ac.Command.CreateCommand("");

        // Runs a PML command. RunInPdms() returns false for an invalid command (Command.Error carries no text),
        // so a failure is reported with the command itself.
        public bool ExecuteCommand(string command)
        {
            return AvevaGate.Run(() =>
            {
                try
                {
                    ac.Command tempCommand = ac.Command.CreateCommand(command);
                    if (!tempCommand.RunInPdms())
                    {
                        ac.Command.OutputAndClearError();
                        throw new NexusException("PML command failed: " + command, 400);
                    }
                    return true;
                }
                catch (NexusException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new NexusException(ex.Message + "\n" + ex.StackTrace);
                }
            });
        }

        public string GetStringVariable(string name)
        {
            return AvevaGate.Run(() =>
            {
                RequireVariable(name);
                return Guarded(() => getCommand.GetPMLVariableString(name.ToUpper()));
            });
        }

        public double GetRealVariable(string name)
        {
            return AvevaGate.Run(() =>
            {
                RequireVariable(name);
                return Guarded(() => getCommand.GetPMLVariableReal(name.ToUpper()));
            });
        }

        public bool GetBooleanVariable(string name)
        {
            return AvevaGate.Run(() =>
            {
                RequireVariable(name);
                return Guarded(() => getCommand.GetPMLVariableBoolean(name.ToUpper()));
            });
        }

        // Elements come back in PML order (GetStringArrayFromPML needs the "!!" prefix; verified on E3D 3.1).
        public object[] GetArrayVariable(string name)
        {
            return AvevaGate.Run(() =>
            {
                RequireVariable(name);
                return Guarded(() => ac.Command.GetStringArrayFromPML("!!" + name).Cast<object>().ToArray());
            });
        }

        public object GetAttribute(
            string dbref,
            string attribute)
        {
            return AvevaGate.Run<object>(() =>
            {
                DbAttribute dbAttribute = DbAttribute.GetDbAttribute(attribute);
                DbElement dbElement = DbElement.GetElement(dbref);
                return dbElement.GetAsString(dbAttribute);
            });
        }

        // GetElementType("FOO") with an unknown name crashes the E3D session, so every type is checked against
        // the list of real types first (cached; 1884 types on E3D 3.1).
        private static HashSet<string> _typeNames;

        private static bool IsKnownType(string type)
        {
            if (_typeNames == null)
            {
                _typeNames = new HashSet<string>(
                    DbElementType.GetAllElementTypes().Select(t => t.Name),
                    StringComparer.OrdinalIgnoreCase);
            }
            return _typeNames.Contains(type);
        }

        public string[] CollectRefs(string type, string scope, string where)
        {
            return AvevaGate.Run(() =>
            {
                if (!IsKnownType(type))
                {
                    throw new NexusException("Unknown element type '" + type + "'", 400);
                }

                string command = "var !!nexusQ collect all " + type
                    + (where != null ? " with (" + where + ")" : "")
                    + (scope != null ? " for " + scope : "");

                if (!ac.Command.CreateCommand(command).RunInPdms())
                {
                    ac.Command.OutputAndClearError();
                    throw new NexusException("PML collect failed (check the where condition and scope): " + command, 400);
                }

                string[] refs = ac.Command.GetStringArrayFromPML("!!nexusQ");

                // best-effort cleanup of the temporary global
                if (!ac.Command.CreateCommand("!!nexusQ.delete()").RunInPdms())
                {
                    ac.Command.OutputAndClearError();
                }
                return refs;
            });
        }

        public RunResult RunScript(IList<string> lines, IList<string> returnVariables)
        {
            return AvevaGate.Run(() =>
            {
                var result = new RunResult();
                foreach (string line in lines)
                {
                    if (!ac.Command.CreateCommand(line).RunInPdms())
                    {
                        ac.Command.OutputAndClearError();
                        result.Errors.Add("PML command failed: " + line);
                    }
                }
                foreach (string name in returnVariables)
                {
                    result.Values[name] = ReadGlobal(name);
                }
                result.Ok = result.Errors.Count == 0;
                return result;
            });
        }

        // Typed read of a global variable: arrays as string[], REAL as double, BOOLEAN as bool, the rest as text.
        // Undefined (or unreadable) variables give null.
        private object ReadGlobal(string name)
        {
            if (!VariableExists(name))
            {
                return null;
            }

            string kind = QueryText("!!nexusKind = !!" + name + ".objectType()", "NEXUSKIND");
            if (kind == "ARRAY")
            {
                return ac.Command.GetStringArrayFromPML("!!" + name);
            }

            string text = QueryText("!!nexusText = !!" + name + ".string()", "NEXUSTEXT");
            if (text == null)
            {
                return null;
            }
            switch (kind)
            {
                case "REAL":
                    double number;
                    return double.TryParse(text, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out number) ? (object)number : text;
                case "BOOLEAN":
                    return string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase);
                default:
                    return text;
            }
        }

        private string QueryText(string command, string variable)
        {
            if (!ac.Command.CreateCommand(command).RunInPdms())
            {
                ac.Command.OutputAndClearError();
                return null;
            }
            return getCommand.GetPMLVariableString(variable);
        }

        // Module has no PML/API accessor; the TTY module process runs with "-modulename=Design", so it is read from
        // the command line. Version comes from the running executable. Each value is null if unavailable.
        public SessionInfo GetSessionInfo()
        {
            return AvevaGate.Run(() => new SessionInfo
            {
                Project = Safe(() => Project.CurrentProject.Code),
                Mdb = Safe(() => MDB.CurrentMDB.Name),
                User = Safe(() => Project.CurrentProject.UserName),
                Module = Safe(() =>
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        Environment.CommandLine, @"-modulename=(\S+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    return match.Success ? match.Groups[1].Value : null;
                }),
                E3dVersion = Safe(() => System.Diagnostics.Process.GetCurrentProcess().MainModule.FileVersionInfo.ProductVersion)
            });
        }

        private static string Safe(Func<string> read)
        {
            try { return read(); }
            catch (Exception) { return null; }
        }

        public bool ElementExists(string dbref)
        {
            return AvevaGate.Run(() => DbElement.GetElement(dbref).IsValid);
        }

        private static DbElement RequireElement(string dbref)
        {
            DbElement element = DbElement.GetElement(dbref);
            if (!element.IsValid)
            {
                throw new NexusException("Element " + dbref + " not found", 404);
            }
            return element;
        }

        private static string RefOf(DbElement element)
        {
            return element.GetAsString(DbAttribute.GetDbAttribute("REF"));
        }

        public string[] GetChildRefs(string dbref, string type)
        {
            return AvevaGate.Run(() =>
            {
                DbElement element = RequireElement(dbref);
                if (type != null && !IsKnownType(type))
                {
                    throw new NexusException("Unknown element type '" + type + "'", 400);
                }

                var refs = new List<string>();
                foreach (DbElement member in element.Members())
                {
                    if (type != null && !string.Equals(member.GetElementType().Name, type, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    refs.Add(RefOf(member));
                }
                return refs.ToArray();
            });
        }

        public string GetOwnerRef(string dbref)
        {
            return AvevaGate.Run(() => RefOf(RequireElement(dbref).Owner));
        }

        public string[] GetAttributeNames(string type)
        {
            return AvevaGate.Run(() =>
            {
                if (!IsKnownType(type))
                {
                    throw new NexusException("Unknown element type '" + type + "'", 400);
                }

                DbElementType elementType = DbElementType.GetElementType(type);
                return elementType.SystemAttributes()
                    .Concat(elementType.GetAllUdas())
                    .Select(a => a.Name)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            });
        }

        public Dictionary<string, object> ReadAttributes(string dbref, IList<string> attributes)
        {
            return AvevaGate.Run(() =>
            {
                var values = new Dictionary<string, object>();
                DbElement element = DbElement.GetElement(dbref);
                foreach (string name in attributes)
                {
                    object value = null;
                    try
                    {
                        DbAttribute attribute = DbAttribute.GetDbAttribute(name);
                        if (attribute != null && element.IsValid)
                        {
                            value = element.GetAsString(attribute);
                        }
                    }
                    catch (Exception)
                    {
                        value = null;
                    }
                    values[name] = value;
                }
                return values;
            });
        }

        private void RequireVariable(string name)
        {
            if (!VariableExists(name))
            {
                throw new VariableNotFoundException(name);
            }
        }

        private static T Guarded<T>(Func<T> read)
        {
            try
            {
                return read();
            }
            catch (Exception ex)
            {
                throw new NexusException(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private bool VariableExists(string name)
        {
            try
            {
                getCommand.CommandString =
                    $"!!varExist = object boolean()";
                getCommand.RunInPdms();

                getCommand.CommandString =
                    $"!!varExist = defined( !!{name} )";
                getCommand.RunInPdms();

                return getCommand.GetPMLVariableBoolean("VAREXIST");
            }
            catch
            {
                return false;
            }
        }
    }
}
