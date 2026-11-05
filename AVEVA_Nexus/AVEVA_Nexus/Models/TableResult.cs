using System.Collections.Generic;

namespace AVEVA_Nexus.Models
{
    // Tabular result: every row has the key "dbref" plus one key per requested attribute.
    public class TableResult
    {
        public int Total { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
        public List<Dictionary<string, object>> Rows { get; set; } = new List<Dictionary<string, object>>();
    }
}
