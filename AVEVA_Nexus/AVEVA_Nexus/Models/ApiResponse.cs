using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVEVA_Nexus.Models
{
    public class ApiResponse
    {
        public bool Success { get; set; }

        public object Data { get; set; }

        public string Error { get; set; }

        public string Message { get; set; }

        public string RequestId { get; set; }
    }
}
