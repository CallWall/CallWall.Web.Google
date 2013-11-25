using System;
using System.Collections.Generic;

namespace CallWall.Web.GoogleProvider
{
    public sealed class AccountConfiguration : IAccountConfiguration
    {
        public static readonly IAccountConfiguration Instance = new AccountConfiguration();

        private AccountConfiguration()
        {}

        public string Name { get { return "Google"; } }
        public Uri Image { get { return new Uri("/Content/Images/Google/GoogleIcon.svg", UriKind.Relative); } }
        public IEnumerable<IResourceScope> Resources { get { return ResourceScope.AvailableResourceScopes; } }
    }
}