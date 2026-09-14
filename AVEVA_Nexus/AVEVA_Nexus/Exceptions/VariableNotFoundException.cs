namespace AVEVA_Nexus.Exceptions
{
    public class VariableNotFoundException
        : NexusException
    {
        public VariableNotFoundException(string variableName)
            : base(
                $"Variable '{variableName}' was not found.",
                404)
        {
        }
    }
}