using AVEVA_Nexus.Exceptions;
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
