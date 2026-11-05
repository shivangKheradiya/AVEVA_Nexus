using System.Collections.Generic;

namespace AVEVA_Nexus.Models
{
    // Body of POST /api/Element/Get
    public class GetRequest
    {
        public List<string> Refs { get; set; }
        public List<string> Attributes { get; set; }
    }
}
