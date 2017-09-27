using Microsoft.Azure.Search.Models;
using System.Collections.Generic;

namespace iCoreService.Models
{
    /// <summary>
    /// The set of biographies returned by an Azure Search query.
    /// </summary>
    public class BiographyResultSet
    {
        /// <summary>
        /// The facets for the current result set as returned by Azure Search.
        /// </summary>
        public FacetResults Facets { get; set; }

        /// <summary>
        /// The list biography documents matching the search criteria.
        /// </summary>
        public IList<SearchResult> Biographies { get; set; }

        /// <summary>
        /// Total number of biographies matching the search criteria.
        /// </summary>
        public long? Count { get; set; }
    }
}
