using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AVEVA_Nexus.Exceptions;
using AVEVA_Nexus.Models;
using AVEVA_Nexus.Services;
using Newtonsoft.Json;

namespace AVEVA_Nexus.Controllers
{
    public class PmlController
    {
        private readonly PmlService _service;

        public PmlController()
        {
            _service = new PmlService();
        }

        public ApiResponse Execute(
            string json)
        {
            ExecuteRequest request = string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<ExecuteRequest>(json);

            if (request == null || string.IsNullOrWhiteSpace(request.Command))
            {
                throw new NexusException("command is required", 400);
            }

            bool result =
                _service.Execute(
                    request.Command);

            return new ApiResponse
            {
                Success = result
            };
        }

        public ApiResponse GetString(string name)
        {
            return new ApiResponse
            {
                Success = true,
                Data = _service.GetString(name)
            };
        }

        public ApiResponse GetReal(string name)
        {
            return new ApiResponse
            {
                Success = true,
                Data = _service.GetReal(name)
            };
        }

        public ApiResponse GetBoolean(string name)
        {
            return new ApiResponse
            {
                Success = true,
                Data = _service.GetBoolean(name)
            };
        }

        public ApiResponse GetArray(string name)
        {
            return new ApiResponse
            {
                Success = true,
                Data = _service.GetArray(name)
            };
        }
    }
}