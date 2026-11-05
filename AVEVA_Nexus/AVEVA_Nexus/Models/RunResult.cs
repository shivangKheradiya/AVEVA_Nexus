using System.Collections.Generic;

namespace AVEVA_Nexus.Models
{
    // Data of POST /api/PML/Run. Ok is false when any line failed; the remaining lines still ran.
    // Values: string, double, bool or string[] per variable type; null if the variable is not defined.
    public class RunResult
    {
        public bool Ok { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
    }
}
