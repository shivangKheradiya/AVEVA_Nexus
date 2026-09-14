using System.Collections.Specialized;

namespace AVEVA_Nexus.Http
{
    public class RouteContext
    {
        public string Method { get; set; }

        public string Path { get; set; }

        public NameValueCollection Query { get; set; }

        public string Body { get; set; } = string.Empty;
    }
}