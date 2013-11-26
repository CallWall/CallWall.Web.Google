using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using CallWall.Web;

namespace CallWall.WebHost.Providers
{
    //TODO: Validate that all methods are still used. -LC
    public interface ISecurityProvider
    {
        IPrincipal GetPrincipal(HttpRequest request);
        ISession GetSession(IPrincipal user);
        void SetPrincipal(Controller controller, ISession session);
        void LogOff();

        IAccountAuthentication GetAuthenticationProvider(string account);
        IEnumerable<IAccountConfiguration> GetAccountConfigurations();
        Uri AuthenticationUri(string account, string callBackUri, string[] resource);
        ISession CreateSession(string code, string state);
    }
}