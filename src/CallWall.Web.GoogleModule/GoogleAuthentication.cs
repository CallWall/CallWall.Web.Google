using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CallWall.Web.GoogleModule
{
    public class GoogleAuthentication : IAccountAuthentication
    {
        private readonly string _clientId;
        private readonly string _clientSecret;

        public GoogleAuthentication()
        {
            var config = GoogleConfigSection.GetConfig();
            _clientId = config.ClientId;
            _clientSecret = config.ClientSecret;
        }

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
            uriBuilder.Append("&client_id=");
            uriBuilder.Append(_clientId);

            var state = new AuthState { RedirectUri = redirectUri, Scopes = scopes };
            uriBuilder.Append("&state=");
            uriBuilder.Append(state.ToUrlEncoded());

            return new Uri(uriBuilder.ToString());
        }

        public bool CanCreateSessionFromState(string code, string state)
        {
            return AuthState.IsValidOAuthState(state);
        }

        public ISession CreateSession(string code, string state)
        {
            var authState = AuthState.Deserialize(state);
            var request = CreateTokenRequest(code, authState.RedirectUri);

            var client = new HttpClient();
            var response = client.SendAsync(request);
            var accessTokenResponse = response.Result.Content.ReadAsStringAsync();
            var json = JObject.Parse(accessTokenResponse.Result);
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

        public bool TryDeserialiseSession(string payload, out ISession session)
        {
            session = null;
            try
            {
                var json = JObject.Parse(payload);

                var authorizedResources = json["AuthorizedResources"].ToObject<IEnumerable<Uri>>();

                session = new Session(
                    (string)json["AccessToken"],
                    (string)json["RefreshToken"],
                    (DateTimeOffset)json["Expires"],
                    authorizedResources);
                return true;
            }
            catch (Exception)
            {
                //TODO: Log this failure as Trace/Debug
                return false;
            }
        }

        private HttpRequestMessage CreateTokenRequest(string code, string redirectUri)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, @"https://accounts.google.com/o/oauth2/token");
            var postParameters = new Dictionary<string, string>
            {
                {"code", code},
                {"client_id", _clientId},
                {"redirect_uri", redirectUri},
                {"client_secret", _clientSecret},
                {"grant_type", "authorization_code"}
            };

            request.Content = new FormUrlEncodedContent(postParameters);
            return request;
        }

        private class AuthState
        {
            private const string _account = "Google";

            public static bool IsValidOAuthState(string state)
            {
                var json = JObject.Parse(state);

                JToken account;
                if (json.TryGetValue("Account", out account))
                {
                    if (account.ToString() == _account)
                    {
                        return true;
                    }
                }
                return false;
            }

            public static AuthState Deserialize(string state)
            {
                return JsonConvert.DeserializeObject<AuthState>(state);
            }

            public string Account { get { return _account; } }

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