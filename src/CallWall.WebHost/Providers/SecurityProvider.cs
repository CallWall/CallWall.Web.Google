using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using CallWall.Web;

namespace CallWall.WebHost.Providers
{
    public class SecurityProvider : ISecurityProvider
    {
        private readonly IEnumerable<IAccountAuthentication> _authenticationProviders;
        internal const string ProviderTypeKey = "http://callwall.com/identity/Provider";
        internal const string AccessTokenTypeKey = "http://callwall.com/identity/AccessToken";
        internal const string RefreshTokenTypeKey = "http://callwall.com/identity/RefreshToken";
        internal const string ExpiryTypeKey = "http://callwall.com/identity/Expires";
        internal const string ResourceTypeKey = "http://callwall.com/identity/Resource";

        public SecurityProvider(IEnumerable<IAccountAuthentication> authenticationProviders)
        {
            _authenticationProviders = authenticationProviders;
        }

        public ISession GetSession(IPrincipal user)
        {
            FormsIdentity ident = user.Identity as FormsIdentity;
            if (ident != null)
            {
                return GetSession(ident.Ticket);
            }
            return null;
        }


        public void SetPrincipal(Controller controller, ISession session)
        {
            var state = session.Serialize();
            var authTicket = new FormsAuthenticationTicket(1, "userNameGoesHere", DateTime.UtcNow, DateTime.MaxValue, true, state, "CallWallAuth");
            var encTicket = FormsAuthentication.Encrypt(authTicket);
            var faCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
            controller.Response.Cookies.Add(faCookie);
        }

        public void LogOff()
        {
            FormsAuthentication.SignOut();
        }

        public IAccountAuthentication GetAuthenticationProvider(string account)
        {
            return _authenticationProviders.Single(ap => string.Equals(ap.Configuration.Name, account, StringComparison.Ordinal));
        }

        public IEnumerable<IAccountConfiguration> GetAccountConfigurations()
        {
            return _authenticationProviders.Select(ap => ap.Configuration);
        }

        public Uri AuthenticationUri(string account, string callBackUri, string[] resources)
        {
            var ap = GetAuthenticationProvider(account);
            return ap.AuthenticationUri(callBackUri, resources);
        }

        public ISession CreateSession(string code, string state)
        {
            var authProvider = _authenticationProviders.Single(ap =>ap.CanCreateSessionFromState(code, state));
            var session = authProvider.CreateSession(code, state);
            return session;
        }


        public IPrincipal GetPrincipal(HttpRequest request)
        {
            if (!FormsAuthentication.CookiesSupported) return null;
            HttpCookie authCookie = request.Cookies[FormsAuthentication.FormsCookieName];

            if (authCookie != null)
            {
                var authTicket = FormsAuthentication.Decrypt(authCookie.Value);
                if (authTicket == null)
                    return null;

                var session = GetSession(authTicket);
                if (session != null)
                {
                    return SessionToPrincipal(session);
                }
            }
            return null;
        }

        private static IPrincipal SessionToPrincipal(ISession session)
        {
            var claims = session.AuthorizedResources
                                .Select(r => new Claim(ResourceTypeKey, r.ToString()))
                                .ToList();
            claims.AddRange(new[]
                {
                    new Claim(ProviderTypeKey, session.Provider),
                    new Claim(AccessTokenTypeKey, session.AccessToken),
                    new Claim(RefreshTokenTypeKey, session.RefreshToken),
                    new Claim(ExpiryTypeKey, session.Expires.ToString("o"))
                });

            var principal = new ClaimsPrincipal(new[]
                {
                    new ClaimsIdentity(claims, AuthenticationTypes.Password)
                });
            return principal;
        }

        private ISession GetSession(FormsAuthenticationTicket ticket)
        {
            var sessionPayload = ticket.UserData;
            foreach (var authenticationProvider in _authenticationProviders)
            {
                ISession session = null;
                if (authenticationProvider.TryDeserialiseSession(sessionPayload, out session))
                {
                    return session;
                }
            }
            return null;
        }
    }

    public static class SecurityExtensions
    {
        //public static ISession ToSession(this IPrincipal user)
        //{
        //    var principal = user as ClaimsPrincipal;
        //    if (principal == null) return null;

        //    string provider = principal.FindFirst(SecurityProvider.ProviderTypeKey).Value;
        //    string accessToken = principal.FindFirst(SecurityProvider.AccessTokenTypeKey).Value;
        //    string refreshToken = principal.FindFirst(SecurityProvider.RefreshTokenTypeKey).Value;
        //    string strExpiry = principal.FindFirst(SecurityProvider.ExpiryTypeKey).Value;
        //    var expiry = DateTimeOffset.ParseExact(strExpiry, "o", CultureInfo.InvariantCulture);
        //    var resources = principal.FindAll(SecurityProvider.ResourceTypeKey)
        //                             .Select(c => new Uri(c.Value));
        //    var session = new Session(accessToken, refreshToken, expiry, resources);

        //    return session;
        //}
    }
}