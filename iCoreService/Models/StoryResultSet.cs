using System.Collections.Generic;
using Microsoft.Azure.Search.Models;

namespace iCoreService.Models
{
    /// <summary>
    /// The set of stories returned by an Azure Search query.
    /// </summary>
    public class StoryResultSet
    {
        /// <summary>
        /// The facets for the current result set as returned by Azure Search.
        /// </summary>
        public FacetResults Facets { get; set; }

        /// <summary>
        /// The list story documents matching the search criteria.
        /// </summary>
        public IList<SearchResult> Stories { get; set; }

        /// <summary>
        /// Total number of stories matching the search criteria.
        /// </summary>
        public long? Count { get; set; }
    }
}