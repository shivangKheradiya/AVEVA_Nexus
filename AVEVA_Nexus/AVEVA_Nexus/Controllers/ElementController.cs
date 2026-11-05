using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AVEVA_Nexus.Models;
using AVEVA_Nexus.Services;
using Newtonsoft.Json;

namespace AVEVA_Nexus.Controllers
{
    public class ElementController
    {
        private readonly ElementService _service;

        public ElementController()
        {
            _service = new ElementService();
        }

        public ApiResponse Query(string json)
        {
            QueryRequest request = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonConvert.DeserializeObject<QueryRequest>(json);

            return new ApiResponse
            {
                Success = true,
                Data = new QueryService().Run(request)
            };
        }

        public ApiResponse GetAttribute(
            string dbref,
            string attribute)
        {
            return new ApiResponse
            {
                Success = true,
                Data = _service.GetAttribute(
                    dbref,
                    attribute)
            };
        }
    }
}
