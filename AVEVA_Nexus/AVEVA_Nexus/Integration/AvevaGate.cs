using System;

namespace AVEVA_Nexus.Integration
{
    // E3D/PML is not thread-safe: simultaneous requests crashed the whole E3D session (observed in testing).
    // Every call into AVEVA goes through this single process-wide lock. The lock is re-entrant, so provider
    // methods may call each other.
    public static class AvevaGate
    {
        static readonly object Lock = new object();

        public static T Run<T>(Func<T> work)
        {
            lock (Lock) { return work(); }
        }
    }
}
