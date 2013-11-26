using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Security.Authentication;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Practices.Unity.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CallWall.Web.Providers.Google
{
    public interface IGoogleAccountAuthentication : IAccountAuthentication
    {
        IObservable<ISession> GetSession(IPrincipal user);
    }
    public class GoogleAccountAuthentication : IGoogleAccountAuthentication
    {
        //HACK: Move to configuration -LC
        private const string ClientId = "410654176090-8fk01hicm60blfbmjfrfruvpabnvat6s.apps.googleusercontent.com";
        //HACK: Move to configuration -LC
        private const string ClientSecret = "cl6V2rzrB0uit3mHDB2jAmnG";

        public IAccountConfiguration Configuration { get { return AccountConfiguration.Instance; } }

        public Uri AuthenticationUri(string redirectUri, IList<string> scopes)
        {
            var uriBuilder = new StringBuilder();
            uriBuilder.Append("https://accounts.google.com/o/oauth2/auth");
            uriBuilder.Append("?scope=");
            var scopeSsv = string.Join(" ", scopes);
            uriBuilder.Append(HttpUtility.UrlEncode(scopeSsv));

            uriBuilder.Append("&redirect_uri=");
            uriBuilder.Append(HttpUtility.UrlEncode(redirectUri));

            uriBuilder.Append("&response_type=code");
            
            uriBuilder.Append("&client_id=410654176090-8fk01hicm60blfbmjfrfruvpabnvat6s.apps.googleusercontent.com");

            //Required to get a refresh token. Even though the user is online we don't want to pop to a login screen when trying to use SignalR. -LC
            uriBuilder.Append("&access_type=offline");
            
            var state = new AuthState { RedirectUri = redirectUri, Scopes = scopes };
            uriBuilder.Append("&state=");
            uriBuilder.Append(state.ToUrlEncoded());

            return new Uri(uriBuilder.ToString());
        }
        public async Task<ISession> CreateSession(string code, string state)
        {
            var client = new HttpClient();
            var authState = AuthState.Deserialize(state);
            var request = CreateTokenRequest(code, authState.RedirectUri);

            var response = await client.SendAsync(request);
            if(!response.IsSuccessStatusCode)
                throw new HttpResponseException(response);

            var accessTokenResponse = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(accessTokenResponse);
            var resources = authState.Scopes.Select(s => new Uri(s));

            if (json["error"] != null)
                throw new AuthenticationException((string)json["error"]);

            return new Session(
                (string)json["access_token"],
                (string)json["refresh_token"],
                TimeSpan.FromSeconds((int)json["expires_in"]),
                DateTimeOffset.Now,
                resources);
        }
        public ISession CreateSessionSync(string code, string state)
        {
            var client = new HttpClient();
            var authState = AuthState.Deserialize(state);
            var request = CreateTokenRequest(code, authState.RedirectUri);

            var response = client.SendAsync(request).Result;
            if(!response.IsSuccessStatusCode)
                throw new HttpResponseException(response);

            var accessTokenResponse = response.Content.ReadAsStringAsync().Result;
            var json = JObject.Parse(accessTokenResponse);
            var resources = authState.Scopes.Select(s => new Uri(s));

            if (json["error"] != null)
                throw new AuthenticationException((string)json["error"]);

            return new Session(
                (string)json["access_token"],
                (string)json["refresh_token"],
                TimeSpan.FromSeconds((int)json["expires_in"]),
                DateTimeOffset.Now,
                resources);
        }

        public IObservable<ISession> GetSession(IPrincipal user)
        {
            return EnsureRefreshSession(user.ToSession());
        }

        //TODO: Add cancelation support -LC
        //TODO: Somehow needs to update the cookie?! -LC
        private IObservable<ISession> EnsureRefreshSession(ISession session)
        {
            return Observable.Create<ISession>(async o =>
                {
                    if (session == null)
                        return Observable.Empty<ISession>().Subscribe(o);
                    if (!session.HasExpired())
                        return Observable.Return(session).Subscribe(o);
                    var client = new HttpClient();
                    var requestedDate = DateTimeOffset.Now;
                    var request = CreateRefreshTokenRequest(session.RefreshToken);

                    var response = await client.SendAsync(request);
                    if(!response.IsSuccessStatusCode)
                        o.OnError(new HttpResponseException(response));

                    var refreshTokenResponse = await response.Content.ReadAsStringAsync();
                    var newSession = CreateRefreshedSession(refreshTokenResponse, requestedDate, session.RefreshToken, session.AuthorizedResources);

                    return Observable.Return(newSession).Subscribe(o);
                });
        }

        public ISession CreateRefreshedSession(string refreshTokenResponse, DateTimeOffset requestedAt, string refreshToken, IEnumerable<Uri> requestedResources)
        {
            var payload = JObject.Parse(refreshTokenResponse);
            return new Session(
                (string)payload["access_token"],
                refreshToken,
                TimeSpan.FromSeconds((int)payload["expires_in"]),
                requestedAt,
                requestedResources);
        }

        private HttpRequestMessage CreateTokenRequest(string code, string redirectUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, @"https://accounts.google.com/o/oauth2/token");
            var postParameters = new Dictionary<string, string>
            {
                {"code", code},
                {"client_id", ClientId},
                {"redirect_uri", redirectUri},
                {"client_secret", ClientSecret},
                {"grant_type", "authorization_code"},
            };

            request.Content = new FormUrlEncodedContent(postParameters);
            return request;
        }

        public HttpRequestMessage CreateRefreshTokenRequest(string refreshToken)
        {
            Guard.ArgumentNotNullOrEmpty(refreshToken, "refreshToken");

            var request = new HttpRequestMessage(HttpMethod.Post, @"https://accounts.google.com/o/oauth2/token");

            var postParameters = new Dictionary<string, string>
                {
                    {"client_id", ClientId},
                    {"client_secret", ClientSecret},
                    {"grant_type", "refresh_token"},
                    {"refresh_token", refreshToken},
                };

            request.Content = new FormUrlEncodedContent(postParameters);
            return request;
        }

        private class AuthState
        {
            public static AuthState Deserialize(string state)
            {
                return JsonConvert.DeserializeObject<AuthState>(state);
            }

            public string Account { get { return "Google"; } }

            public string RedirectUri { get; set; }

            public IEnumerable<string> Scopes { get; set; }

            public string ToUrlEncoded()
            {
                var data = JsonConvert.SerializeObject(this);
                return HttpUtility.UrlEncode(data);
            }
        }

    }
}