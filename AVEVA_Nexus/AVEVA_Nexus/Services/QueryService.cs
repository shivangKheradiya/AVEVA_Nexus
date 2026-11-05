using AVEVA_Nexus.Exceptions;
using AVEVA_Nexus.Integration;
using AVEVA_Nexus.Models;
using AVEVA_Nexus.Query;
using AVEVA_Nexus.Validation;
using System.Collections.Generic;
using System.Linq;

namespace AVEVA_Nexus.Services
{
    public class QueryService
    {
        private readonly IAvevaProvider _provider;

        public QueryService() : this(new AvevaProvider()) { }

        public QueryService(IAvevaProvider provider)
        {
            _provider = provider;
        }

        public TableResult Run(QueryRequest request)
        {
            if (request == null)
            {
                throw new NexusException("request body is required", 400);
            }

            string type = PmlInputValidator.Type(request.Type);
            string scope = PmlInputValidator.Scope(request.Scope);
            string where = PmlInputValidator.Where(request.Where);
            List<string> attributes = (request.Attributes ?? new List<string>())
                .Select(PmlInputValidator.Attribute)
                .Distinct()
                .ToList();
            var paging = Paging.Normalize(request.Offset, request.Limit);

            string[] refs = _provider.CollectRefs(type, scope, where);
            List<string> page = Paging.Slice(refs, paging.offset, paging.limit);

            var result = new TableResult { Total = refs.Length, Offset = paging.offset, Limit = paging.limit };
            result.Columns.Add("dbref");
            result.Columns.AddRange(attributes);

            foreach (string dbref in page)
            {
                var row = new Dictionary<string, object> { { "dbref", dbref } };
                if (attributes.Count > 0)
                {
                    foreach (var pair in _provider.ReadAttributes(dbref, attributes))
                    {
                        row[pair.Key] = pair.Value;
                    }
                }
                result.Rows.Add(row);
            }
            return result;
        }
    }
}
