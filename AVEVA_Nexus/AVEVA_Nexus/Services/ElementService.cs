using System.Collections.Generic;
using System.Linq;

using AVEVA_Nexus.Exceptions;
using AVEVA_Nexus.Integration;
using AVEVA_Nexus.Models;
using AVEVA_Nexus.Validation;

namespace AVEVA_Nexus.Services
{
    public class ElementService
    {
        private const int MaxRefsPerGet = 1000;

        private readonly IAvevaProvider _provider;

        public ElementService() : this(new AvevaProvider()) { }

        public ElementService(IAvevaProvider provider)
        {
            _provider = provider;
        }

        public object GetAttribute(
            string dbref,
            string attribute)
        {
            return _provider.GetAttribute(
                dbref,
                attribute);
        }

        // Batch read: one row per ref, in input order. Any ref that does not exist fails the whole request (404).
        public TableResult Get(GetRequest request)
        {
            if (request == null || request.Refs == null || request.Refs.Count == 0)
            {
                throw new NexusException("refs is required", 400);
            }
            if (request.Refs.Count > MaxRefsPerGet)
            {
                throw new NexusException("too many refs (max " + MaxRefsPerGet + ")", 400);
            }

            List<string> refs = request.Refs.Select(PmlInputValidator.DbRef).ToList();
            List<string> attributes = (request.Attributes ?? new List<string>())
                .Select(PmlInputValidator.Attribute)
                .Distinct()
                .ToList();

            foreach (string dbref in refs)
            {
                RequireExists(dbref);
            }

            var result = new TableResult { Total = refs.Count, Offset = 0, Limit = refs.Count };
            result.Columns.Add("dbref");
            result.Columns.AddRange(attributes);

            foreach (string dbref in refs)
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

        public ChildrenResult Children(string dbref, string type)
        {
            string reference = PmlInputValidator.DbRef(dbref);
            string childType = string.IsNullOrWhiteSpace(type) ? null : PmlInputValidator.Type(type);
            RequireExists(reference);

            var result = new ChildrenResult { Owner = _provider.GetOwnerRef(reference) };
            var wanted = new List<string> { "TYPE", "NAME" };
            foreach (string childRef in _provider.GetChildRefs(reference, childType))
            {
                var values = _provider.ReadAttributes(childRef, wanted);
                result.Children.Add(new Dictionary<string, object>
                {
                    { "dbref", childRef },
                    { "type", values["TYPE"] },
                    { "name", values["NAME"] }
                });
            }
            return result;
        }

        public string[] AttributeList(string type)
        {
            return _provider.GetAttributeNames(PmlInputValidator.Type(type));
        }

        private void RequireExists(string dbref)
        {
            if (!_provider.ElementExists(dbref))
            {
                throw new NexusException("Element " + dbref + " not found", 404);
            }
        }
    }
}
