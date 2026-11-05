using AVEVA_Nexus.Config;
using AVEVA_Nexus.Integration;
using AVEVA_Nexus.Models;

namespace AVEVA_Nexus.Services
{
    public class SessionService
    {
        private readonly IAvevaProvider _provider;

        public SessionService() : this(new AvevaProvider()) { }

        public SessionService(IAvevaProvider provider)
        {
            _provider = provider;
        }

        public SessionInfo Info()
        {
            SessionInfo info = _provider.GetSessionInfo();
            info.ReadOnly = ReadOnlyMode.IsOn;
            return info;
        }
    }
}
