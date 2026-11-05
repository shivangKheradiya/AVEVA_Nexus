using System.Collections.Generic;

namespace AVEVA_Nexus.Models
{
    // Body of POST /api/Element/Query
    public class QueryRequest
    {
        public string Type { get; set; }
        public string Scope { get; set; }
        public string Where { get; set; }
        public List<string> Attributes { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
    }
}
