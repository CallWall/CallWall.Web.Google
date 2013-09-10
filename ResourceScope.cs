using System;
using System.Collections.ObjectModel;

namespace CallWall.Web.Providers.Google
{
    public sealed class ResourceScope : IResourceScope
    {
        public static readonly ResourceScope Contacts;
        public static readonly ResourceScope Gmail;
        public static readonly ResourceScope Calendar;

        private static readonly ReadOnlyCollection<ResourceScope> _availableResourceScopes;

        private readonly string _name;
        private readonly Uri _image;
        private readonly Uri _resource;

        static ResourceScope()
        {
            Contacts = new ResourceScope("Contacts", "Contacts_48x48.png", new Uri(@"https://www.google.com/m8/feeds/"));
            Gmail = new ResourceScope("Email", "Email_48x48.png", new Uri(@"https://mail.google.com/"));
            Calendar = new ResourceScope("Calendar", "Calendar_48x48.png", null);
            _availableResourceScopes = new ReadOnlyCollection<ResourceScope>(new[]
                {
                    Contacts,
                    Gmail,
                    //Calendar
                });
        }

        private ResourceScope(string name, string image, Uri resource)
        {
            _name = name;
            _image = new Uri(string.Format("/Content/Images/Google/{0}", image), UriKind.Relative);
            _resource = resource;
        }

        public static ReadOnlyCollection<ResourceScope> AvailableResourceScopes
        {
            get { return _availableResourceScopes; }
        }

        public string Name { get { return _name; } }

        public Uri Resource { get { return _resource; } }

        public Uri Image { get { return _image; } }

        public override string ToString()
        {
            return string.Format("Google.ResourceScope{{{0}}}", Name);
        }
    }
}