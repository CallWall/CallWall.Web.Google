using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CallWall.Web.GoogleProvider
{
    public sealed class GoogleContactProfileTranslator
    {
        private static readonly XmlNamespaceManager Ns;
        private const string AnonContactAvatar = "/Content/images/AnonContact.svg";

        #region xml namespace resovers

        static GoogleContactProfileTranslator()
        {
            Ns = new XmlNamespaceManager(new NameTable());
            Ns.AddNamespace("x", "http://www.w3.org/2005/Atom");
            Ns.AddNamespace("openSearch", "http://a9.com/-/spec/opensearch/1.1/");
            Ns.AddNamespace("gContact", "http://schemas.google.com/contact/2008");
            Ns.AddNamespace("batch", "http://schemas.google.com/gdata/batch");
            Ns.AddNamespace("gd", "http://schemas.google.com/g/2005");
        }

        private static XName ToXName(string prefix, string name)
        {
            var xNamespace = Ns.LookupNamespace(prefix);
            if (xNamespace == null)
                throw new InvalidOperationException(prefix + " namespace prefix is not valid");
            return XName.Get(name, xNamespace);
        }

        private static class OpenSearch
        {
            static OpenSearch()
            {
                TotalResults = ToXName("openSearch", "totalResults");
                StartIndex = ToXName("openSearch", "startIndex");
                ItemsPerPage = ToXName("openSearch", "itemsPerPage");
            }

            public static XName TotalResults { get; private set; }
            public static XName ItemsPerPage { get; private set; }
            public static XName StartIndex { get; private set; }
        }

        private static class Atom
        {
            static Atom()
            {
                Entry = ToXName("x", "entry");
                Title = ToXName("x", "title");
            }

            public static XName Entry { get; private set; }
            public static XName Title { get; private set; }
        }

        private static class Gd
        {
            static Gd()
            {
                ETag = ToXName("gd", "etag");
                Email = ToXName("gd", "email");
            }

            public static XName ETag { get; private set; }
            public static XName Email { get; private set; }
        }

        #endregion

        public int CalculateNextPageStartIndex(string response)
        {
            var xDoc = XDocument.Parse(response);
            if (xDoc.Root == null)
                return 0;
            var totalResults = xDoc.Root.Element(OpenSearch.TotalResults);
            var startIndex = xDoc.Root.Element(OpenSearch.StartIndex);
            var itemsPerPage = xDoc.Root.Element(OpenSearch.ItemsPerPage);
            //var totalResults = xDoc.Root.Element(ToXName("openSearch", "totalResults"));
            //var startIndex = xDoc.Root.Element(ToXName("openSearch", "startIndex"));
            //var itemsPerPage = xDoc.Root.Element(ToXName("openSearch", "itemsPerPage"));
            if (startIndex == null || itemsPerPage == null || totalResults == null)
                return -1;

            var nextIndex = int.Parse(startIndex.Value) + int.Parse(itemsPerPage.Value);
            if (nextIndex > int.Parse(totalResults.Value))
                return -1;
            return nextIndex;

        }

        public BatchOperationPage<IContactSummary> Translate(string response, string accessToken)
        {
            var xDoc = XDocument.Parse(response);
            if (xDoc.Root == null)
                return null;

            var entries = xDoc.Root.Elements(Atom.Entry);
            var contacts = new List<IContactSummary>();
            foreach (var xContactEntry in entries)
            {
                if (xContactEntry == null)
                    return null;

                var title = GetTitle(xContactEntry);
                var avatar = GetAvatar(xContactEntry, accessToken);
                var tags = GetTags(xContactEntry);

                var contact = new ContactSummary(title, avatar, tags);
                contacts.Add(contact);
            }


            var totalResults = xDoc.Root.Element(OpenSearch.TotalResults);
            var startIndex = xDoc.Root.Element(OpenSearch.StartIndex);
            var itemsPerPage = xDoc.Root.Element(OpenSearch.ItemsPerPage);
            if (startIndex == null || itemsPerPage == null || totalResults == null)
                return new BatchOperationPage<IContactSummary>(contacts, 0, 1, -1);

            return new BatchOperationPage<IContactSummary>(contacts,
                int.Parse(startIndex.Value),
                int.Parse(totalResults.Value),
                int.Parse(itemsPerPage.Value));
        }

        private static string GetTitle(XElement xContactEntry)
        {
            var title = XPathString(xContactEntry, "x:title", Ns);
            if(string.IsNullOrWhiteSpace(title))
                title = XPathString(xContactEntry, "gd:name/gd:fullName", Ns);
            if (string.IsNullOrWhiteSpace(title))
            {
                var givenName = XPathString(xContactEntry, "gd:name/gd:givenName", Ns);
                var familyName = XPathString(xContactEntry, "gd:name/gd:familyName", Ns);
                title = string.Format("{0} {1}", givenName, familyName).Trim();
            }
            if (string.IsNullOrWhiteSpace(title))
                title = GetEmailAddresses(xContactEntry).FirstOrDefault();
            return title;
        }

        private static IEnumerable<string> GetEmailAddresses(XElement xContactEntry)
        {
            //<gd:email rel='http://schemas.google.com/g/2005#home' address='danrowe1978@gmail.com' primary='true'/>
            var emails = from xElement in xContactEntry.XPathSelectElements("gd:email", Ns)
                         orderby (xElement.Attribute("primary")!=null) descending 
                         select xElement.Attribute("address").Value;
            return emails;
        }

        private static string GetAvatar(XElement xContactEntry, string accessToken)
        {
            var googleAvatar = xContactEntry.Elements(ToXName("x", "link"))
                                .Where(x => x.Attribute("rel") != null
                                            && x.Attribute("rel").Value == "http://schemas.google.com/contacts/2008/rel#photo"
                                            && x.Attribute("type") != null
                                            && x.Attribute("type").Value == "image/*"
                                            && x.Attribute("href") != null
                                            && x.Attribute(Gd.ETag) != null)    //The absence of an etag attribute means that there is no image content --https://groups.google.com/forum/#!topic/google-contacts-api/bbIf5tcvhU0
                                .Select(x => x.Attribute("href"))
                                .Where(att => att != null)
                                .Select(att => att.Value + "?access_token=" + accessToken)
                                .FirstOrDefault();
            if (googleAvatar == null)
                return AnonContactAvatar;
            return googleAvatar;
        }

        public IEnumerable<string> GetTags(XElement xContactEntry)
        {
            return Enumerable.Empty<string>();

            //var xDoc = XDocument.Parse(response);
            //var xGroupFeed = xDoc.Root;
            //if (xGroupFeed == null)
            //    return Enumerable.Empty<string>();

            //var groups =
            //    (
            //        from groupEntry in xGroupFeed.Elements(ToXName("x", "entry"))
            //        let id = groupEntry.Element(ToXName("x", "id"))
            //        let title = groupEntry.Element(ToXName("x", "title"))
            //        where id != null && title != null && !string.IsNullOrWhiteSpace(title.Value)
            //        select new { Id = id.Value, Title = title.Value.Replace("System Group: ", string.Empty) }
            //    ).ToDictionary(g => g.Id, g => g.Title);


            //foreach (var groupUri in contactProfile.GroupUris)
            //{
            //    string tag;
            //    if (groups.TryGetValue(groupUri.ToString(), out tag))
            //    {
            //        contactProfile.AddTag(tag);
            //    }
            //}

            //return contactProfile;
        }

        private static string XPathString(XNode source, string expression, IXmlNamespaceResolver ns)
        {
            var result = source.XPathSelectElement(expression, ns);
            return result == null ? null : result.Value;
        }

        private static string ToContactAssociation(XAttribute relAttribute)
        {
            //Could be a look
            if (relAttribute == null || relAttribute.Value == null)
                return "Other";
            var hashIndex = relAttribute.Value.LastIndexOf("#", StringComparison.Ordinal);
            var association = relAttribute.Value.Substring(hashIndex + 1);
            return PascalCase(association);
        }

        private static string PascalCase(string input)
        {
            var head = input.Substring(0, 1).ToUpperInvariant();
            var tail = input.Substring(1);
            return head + tail;
        }
    }
}