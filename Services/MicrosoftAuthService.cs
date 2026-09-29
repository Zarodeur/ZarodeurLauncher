using System.Threading.Tasks;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;

namespace ZarodeurLauncher.Services;

public class MicrosoftAuthService
{
    private readonly JELoginHandler _loginHandler;

    public MicrosoftAuthService()
    {
        _loginHandler =
            JELoginHandlerBuilder.BuildDefault();
    }

    public async Task<MSession> LoginAsync()
    {
        return await _loginHandler.Authenticate();
    }
}