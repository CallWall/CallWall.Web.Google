using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CallWall.Web.Providers.Google
{
    public class GoogleContactsProvider : IContactsProvider
    {
        public IObservable<IContactSummary> GetContacts(ISession session)
        {
            return Observable.Create<IContactSummary>(o =>
                {
                    var pages = GetContactPages(session);
                    var query = from page in pages
                                from contact in page.Items
                                select contact;
                    return query.Subscribe(o);
                });
        }

        private IEnumerable<BatchOperationPage<IContactSummary>> GetContactPages(ISession session)
        {
            var batchPage = GetContactPage(session, 1);

            yield return batchPage;
            while (batchPage.NextPageStartIndex > 0)
            {
                Thread.Sleep(1000);  //HACK:Google doesn't like being DOS'ed.
                batchPage = GetContactPage(session, batchPage.NextPageStartIndex);
                yield return batchPage;
            }
        }
        private BatchOperationPage<IContactSummary> GetContactPage(ISession session, int startIndex)
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.google.com/m8/feeds/contacts/default/full?access_token=" + HttpUtility.UrlEncode(session.AccessToken) + "&start-index=" + startIndex);
            request.Headers.Add("GData-Version", "3.0");

            //TODO: Add error handling (not just exceptions but also non 200 responses -LC
            var response = client.SendAsync(request);
            var contactResponse = response.ContinueWith(r => r.Result.Content.ReadAsStringAsync()).Unwrap().Result;

            var translator = new GoogleContactProfileTranslator();
            var contacts = translator.Translate(contactResponse, session.AccessToken);

            return contacts;
        }
    }
}
