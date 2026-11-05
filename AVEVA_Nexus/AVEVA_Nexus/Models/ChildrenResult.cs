using System.Collections.Generic;

namespace AVEVA_Nexus.Models
{
    // Data of GET /api/Element/Children. Each child has the keys "dbref", "type" and "name".
    public class ChildrenResult
    {
        public string Owner { get; set; }
        public List<Dictionary<string, object>> Children { get; set; } = new List<Dictionary<string, object>>();
    }
}
