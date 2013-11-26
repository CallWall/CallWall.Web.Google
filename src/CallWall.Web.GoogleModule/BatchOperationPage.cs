using System;
using System.Collections.Generic;

namespace CallWall.Web.GoogleModule
{
    public sealed class BatchOperationPage<T>
    {
        private readonly IList<T> _items;
        private readonly int _totalResults;
        private readonly int _pageIndex;
        private readonly int _pageCount;
        private readonly int _pageSize;

        public BatchOperationPage(IList<T> items, int startIndex, int totalResults, int itemsPerPage)
        {
            _items = items;
            _totalResults = totalResults;
            _pageIndex = (startIndex - 1) / itemsPerPage;
            _pageCount = (int)Math.Ceiling(totalResults / (double)itemsPerPage);
            _pageSize = itemsPerPage;
        }

        public int TotalResults
        {
            get { return _totalResults; }
        }

        public int NextPageStartIndex
        {
            get
            {
                return (_pageIndex == _pageCount - 1)
                    ? -1
                    : ((_pageIndex + 1) * _pageSize) + 1;
            }
        }

        public IList<T> Items
        {
            get { return _items; }
        }
    }
}