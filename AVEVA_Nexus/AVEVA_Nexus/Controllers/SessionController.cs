using AVEVA_Nexus.Models;
using AVEVA_Nexus.Services;

namespace AVEVA_Nexus.Controllers
{
    public class SessionController
    {
        private readonly SessionService _service = new SessionService();

        public ApiResponse Info()
        {
            return new ApiResponse { Success = true, Data = _service.Info() };
        }
    }
}
