using System.Collections.Generic;

namespace AVEVA_Nexus.Models
{
    // Body of POST /api/PML/Run: PML lines to execute, then global variables (names without "!!") to read back.
    public class RunRequest
    {
        public string Script { get; set; }
        public List<string> Return { get; set; }
    }
}
