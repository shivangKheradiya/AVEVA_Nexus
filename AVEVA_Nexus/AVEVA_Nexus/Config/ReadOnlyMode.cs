using System;

namespace AVEVA_Nexus.Config
{
    // Read-only is the default. Only NEXUS_READONLY=0 allows PML execution (Execute / Run).
    // Pure logic: must not reference Aveva.* (it is compiled by the unit-test runner).
    public static class ReadOnlyMode
    {
        public const string VariableName = "NEXUS_READONLY";

        public static bool IsOn
        {
            get { return Environment.GetEnvironmentVariable(VariableName) != "0"; }
        }
    }
}
