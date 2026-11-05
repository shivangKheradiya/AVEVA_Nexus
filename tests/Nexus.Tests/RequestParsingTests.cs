using AVEVA_Nexus.Models;
using Newtonsoft.Json;

namespace Nexus.Tests
{
    public static class RequestParsingTests
    {
        public static void RunAll()
        {
            Program.Run("QueryRequest_deserializes_camel_and_pascal_case", () =>
            {
                var camel = JsonConvert.DeserializeObject<QueryRequest>(
                    "{\"type\":\"PIPE\",\"scope\":\"=1/2\",\"where\":\"(BORE GT 50)\",\"attributes\":[\"NAME\",\"BORE\"],\"limit\":5,\"offset\":10}");
                Check.Equal("PIPE", camel.Type, "type");
                Check.Equal("=1/2", camel.Scope, "scope");
                Check.Equal("(BORE GT 50)", camel.Where, "where");
                Check.Equal(2, camel.Attributes.Count, "attributes");
                Check.Equal(5, camel.Limit.Value, "limit");
                Check.Equal(10, camel.Offset.Value, "offset");

                var pascal = JsonConvert.DeserializeObject<QueryRequest>("{\"Type\":\"SITE\"}");
                Check.Equal("SITE", pascal.Type, "pascal type");
                Check.True(pascal.Limit == null && pascal.Offset == null, "optional numbers default to null");
            });

            Program.Run("GetRequest_deserializes_refs_and_attributes", () =>
            {
                var r = JsonConvert.DeserializeObject<GetRequest>("{\"refs\":[\"=1/2\",\"=3/4\"],\"attributes\":[\"NAME\"]}");
                Check.Equal(2, r.Refs.Count, "refs");
                Check.Equal("=3/4", r.Refs[1], "second ref");
                Check.Equal("NAME", r.Attributes[0], "attribute");
            });

            Program.Run("TableResult_serializes_with_dbref_and_columns", () =>
            {
                var t = new TableResult { Total = 1, Offset = 0, Limit = 100 };
                t.Columns.Add("dbref");
                t.Rows.Add(new System.Collections.Generic.Dictionary<string, object> { { "dbref", "=1/2" } });
                string json = JsonConvert.SerializeObject(t);
                Check.True(json.Contains("\"Total\":1") && json.Contains("\"dbref\":\"=1/2\""), "json " + json);
            });
        }
    }
}
