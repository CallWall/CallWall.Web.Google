using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CallWall.Web.GoogleProvider
{
    public sealed class Session : ISession
    {
        private readonly string _accessToken;
        private readonly string _refreshToken;
        private readonly DateTimeOffset _expires;
        private readonly ISet<Uri> _authorizedResources;

        public Session(string accessToken, string refreshToken, TimeSpan accessPeriod, DateTimeOffset requested, IEnumerable<Uri> authorizedResources)
            : this(accessToken, refreshToken, requested + accessPeriod, authorizedResources)
        {
        }

        public Session(string accessToken, string refreshToken, DateTimeOffset expires, IEnumerable<Uri> authorizedResources)
        {
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            _expires = expires;
            _authorizedResources = new HashSet<Uri>(authorizedResources);
        }

        public string Provider { get { return "Google"; } }
        public string AccessToken { get { return _accessToken; } }
        public string RefreshToken { get { return _refreshToken; } }
        public DateTimeOffset Expires { get { return _expires; } }

        public bool HasExpired()
        {
            return DateTimeOffset.Now > _expires;
        }

        public ISet<Uri> AuthorizedResources
        {
            get { return _authorizedResources; }
        }

        public string Serialize()
        {
            var jObject = JObject.FromObject(this);
            var json = jObject.ToString(Formatting.None);
            return json;
        }

        public override string ToString()
        {
            return string.Format("Session {{ AccessToken : '{0}', RefreshToken : '{1}', Expires : '{2:o}', AuthorizedResources : '{3}'}}", AccessToken, RefreshToken, Expires, string.Join(";", AuthorizedResources));
        }
    }
}