using Aveva.Core.Commands;
using Aveva.Core.PMLNet;
using Aveva.Core.Utilities.CommandLine;
using AVEVA_Nexus.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ac = Aveva.Core.Utilities.CommandLine;
using Aveva.Core.Database;

namespace AVEVA_Nexus.Integration
{
    public class AvevaProvider : IAvevaProvider
    {
        private ac.Command getCommand = ac.Command.CreateCommand("");

        public bool ExecuteCommand(string command)
        {
            ac.Command tempCommand = ac.Command.CreateCommand(command);
            try
            {
                tempCommand.RunInPdms();
                return true;
            }
            catch (Exception ex)
            {
                throw new NexusException( ex.Message + "\n" + ex.StackTrace);
            }
        }

        public string GetStringVariable(string name)
        {
            if (!VariableExists(name))
            {
                throw new VariableNotFoundException(name);
            }

            try
            {
                return getCommand.GetPMLVariableString(name.ToUpper());
            }
            catch (Exception ex)
            {
                throw new NexusException(ex.Message + "\n" + ex.StackTrace);
            }
        }

        public double GetRealVariable(string name)
        {
            if (!VariableExists(name))
            {
                throw new VariableNotFoundException(name);
            }

            try
            {
                return getCommand.GetPMLVariableReal(name.ToUpper());
            }
            catch (Exception ex)
            {
                throw new NexusException(ex.Message + "\n" + ex.StackTrace);
            }
        }

        public bool GetBooleanVariable(string name)
        {
            if (!VariableExists(name))
            {
                throw new VariableNotFoundException(name);
            }

            try
            {
                return getCommand.GetPMLVariableBoolean(name.ToUpper());
            }
            catch (Exception ex)
            {
                throw new NexusException(ex.Message + "\n" + ex.StackTrace);
            }
        }


        public object[] GetArrayVariable(string name)
        {
            if (!VariableExists(name))
            {
                throw new VariableNotFoundException(name);
            }

            var PmlObject = PMLNetAny.createInstance("PMLOBJECT", new object[] { }, 0, true);
            object pmlArrayObject = new Hashtable();
            PmlObject.invokeMethod("getArray", new object[] { name }, 1, ref pmlArrayObject, true, true);

            List<object> result = new List<object>();

            foreach (DictionaryEntry item in (Hashtable)pmlArrayObject)
            {
                result.Add(item.Value);
            }

            return result.ToArray();
        }

        public object GetAttribute(
            string dbref,
            string attribute)
        {
            DbAttribute dbAttribute = DbAttribute.GetDbAttribute(attribute);
            DbElement dbElement = DbElement.GetElement(dbref);
            return dbElement.GetAsString(dbAttribute);
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