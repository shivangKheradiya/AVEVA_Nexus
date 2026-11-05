using System.Collections.Generic;

namespace AVEVA_Nexus.Integration
{
    public interface IAvevaProvider
    {
        bool ExecuteCommand(string command);

        string GetStringVariable(string name);

        double GetRealVariable(string name);

        bool GetBooleanVariable(string name);

        object[] GetArrayVariable(string name);

        object GetAttribute(
            string dbref,
            string attribute);

        // dbrefs (e.g. "=123/45") of every element of <type>, optionally filtered by a PML condition and limited
        // to a scope (dbref or "/NAME"). Inputs must already be validated. Throws NexusException(400) on bad type/PML.
        string[] CollectRefs(string type, string scope, string where);

        // Attribute values of one element as E3D text; unknown attribute or unreadable value -> null.
        Dictionary<string, object> ReadAttributes(string dbref, IList<string> attributes);
    }
}