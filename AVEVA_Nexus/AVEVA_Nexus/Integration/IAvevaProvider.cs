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
    }
}