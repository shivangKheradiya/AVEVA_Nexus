using AVEVA_Nexus.Exceptions;
using System;
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
