using System.Collections.Generic;

namespace CallWall.Web.GoogleModule
{
    public class ContactSummary : IContactSummary
    {
        private readonly string _title;
        private readonly IEnumerable<string> _tags;
        private readonly string _primaryAvatar;

        public ContactSummary(string title, string primaryAvatar, IEnumerable<string> tags)
        {
            _title = title;
            _primaryAvatar = primaryAvatar;
            _tags = tags;
        }

        /// <summary>
        /// The title description for the contact. Usually their First and Last name.
        /// </summary>
        public string Title
        {
            get { return _title; }
        }

        public IEnumerable<string> Tags
        {
            get { return _tags; }
        }

        public string PrimaryAvatar
        {
            get { return _primaryAvatar; }
        }
    }
}