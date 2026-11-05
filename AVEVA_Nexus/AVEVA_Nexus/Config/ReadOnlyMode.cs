using AVEVA_Nexus.Exceptions;
using System;

namespace AVEVA_Nexus.Config
{
    // Read-only is the default. Only NEXUS_READONLY=0 allows PML execution (Execute / Run).
    // Pure logic: must not reference Aveva.* (it is compiled by the unit-test runner).
    public static class ReadOnlyMode
    {
        public const string VariableName = "NEXUS_READONLY";
        public const string BlockedMessage = "Nexus is in read-only mode (set NEXUS_READONLY=0 to allow PML execution)";

        public static bool IsOn
        {
            get { return Environment.GetEnvironmentVariable(VariableName) != "0"; }
        }

        public static void EnsureWritable()
        {
            if (IsOn)
            {
                throw new NexusException(BlockedMessage, 403);
            }
        }
    }
}
