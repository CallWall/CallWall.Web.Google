using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CallWall.Web.GoogleProvider
{
    public class GoogleContactsProvider : IContactsProvider
    {
        public IObservable<IFeed<IContactSummary>> GetContactsFeed(ISession session)
        {
            return Observable.Create<ContactFeed>(o =>
            {
                try
                {
                    var feed = new ContactFeed(session);
                    return Observable.Return(feed).Subscribe(o);
                }
                catch (Exception ex)
                {
                    return Observable.Throw<ContactFeed>(ex).Subscribe(o);
                }
            });
        }

        private sealed class ContactFeed : IFeed<IContactSummary>
        {
            private readonly int _totalResults;
            private readonly IObservable<IContactSummary> _values;

            public ContactFeed(ISession session)
            {
                var batchPage = GetContactPage(session, 1);
                _totalResults = batchPage.TotalResults;
                _values = GenerateValues(session, batchPage);
            }

            public int TotalResults { get { return _totalResults; } }

            public IObservable<IContactSummary> Values { get { return _values; } }

            private IObservable<IContactSummary> GenerateValues(ISession session, BatchOperationPage<IContactSummary> batchPage)
            {
                return Observable.Create<IContactSummary>(o =>
                {
                    var pages = GetPages(session, batchPage);
                    var query = from page in pages
                                from contact in page.Items
                                select contact;
                    return query.Subscribe(o);
                });
            }

            private static IEnumerable<BatchOperationPage<IContactSummary>> GetPages(ISession session, BatchOperationPage<IContactSummary> batchPage)
            {
                yield return batchPage;
                while (batchPage.NextPageStartIndex > 0)
                {
                    //HACK:Google doesn't like being DOS'ed.
                    //Thread.Sleep(1000);  //HACK:Google doesn't like being DOS'ed.
                    Thread.Sleep(500);  
                    //Thread.Sleep(250);  
                    batchPage = GetContactPage(session, batchPage.NextPageStartIndex);
                    yield return batchPage;
                }
            }

            private static BatchOperationPage<IContactSummary> GetContactPage(ISession session, int startIndex)
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
}
