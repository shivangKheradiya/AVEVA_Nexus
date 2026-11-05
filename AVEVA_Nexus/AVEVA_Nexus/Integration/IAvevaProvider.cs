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

        // False for an invalid or non-existent dbref (E3D returns an element with IsValid == false, no exception).
        bool ElementExists(string dbref);

        // Member dbrefs of an existing element, optionally only of one type (404 if the element does not exist,
        // 400 if the type is unknown).
        string[] GetChildRefs(string dbref, string type);

        string GetOwnerRef(string dbref);

        // System attributes then UDAs (e.g. ":TP-MTO-AREA") valid for an element type. 400 if the type is unknown.
        string[] GetAttributeNames(string type);

        // Attribute values of one element as E3D text; unknown attribute or unreadable value -> null.
        Dictionary<string, object> ReadAttributes(string dbref, IList<string> attributes);
    }
}