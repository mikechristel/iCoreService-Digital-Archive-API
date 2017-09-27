using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Threading.Tasks;
using System.Net.Http.Headers;

using iCoreService.Models;
using iCoreService.AzureServices;

namespace iCoreService.Controllers
{
    /// <summary>
    /// Provide data to the biography search (aka HistoryMakers) view.
    /// </summary>
    public class BiographySearchController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();

        /// <summary>
        /// Performs a full text search of the biography index.
        /// </summary>
        /// <param name="query">The query terms.</param>
        /// <param name="pageSize">Number of results to return.</param>
        /// <param name="currentPage">Retreive the nth page of results based on given page size.</param>
        /// <param name="searchFields">Comma-separated list of fields to be searched.</param>
        /// <param name="genderFacet">Filter results by the given gender.</param>
        /// <param name="yearFacet">Filter results by the given year.</param>
        /// <param name="makerFacet">Filter results by the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filter results by the given comma-separated list of job types.</param>
        /// <param name="lastInitialFacet">Filter results by first letter of last name, e.g., C is filtering to all last names starting with C.</param>
        /// <param name="sortField">Sort results by this field, e.g., lastName;  default is empty (meaning query relevance ranking is used to sort)</param>
        /// <param name="sortInDescendingOrder">Sort results in descending order if true; default is false so default sort order is ascending.</param>
        /// <returns>
        /// A BiographyResultSet document containing the results of the search operation.
        /// </returns>
        /// <remarks>
        /// If searchFields is "all" or not specified, it will be taken to be descriptionShort, accession, lastName, preferredName.
        /// If query is "all" via "*" wildcard and sortField is not specified, query relevance ranking will be taken to be "lastName ascending".
        /// </remarks>
        [ResponseType(typeof(BiographyResultSet))]
        public async Task<HttpResponseMessage> GetBiographySearch(string query = "", 
                                                                  int? pageSize = 20, 
                                                                  int? currentPage = 1, 
                                                                  string searchFields = "all", 
                                                                  string genderFacet = "", 
                                                                  string yearFacet = "", 
                                                                  string makerFacet = "", 
                                                                  string jobFacet = "", 
                                                                  string lastInitialFacet = "",
                                                                  string sortField = "",
                                                                  bool? sortInDescendingOrder = false)
        {
            try
            {
                // NOTE: One override: to add in a sortField of "lastName" if the query is empty or just the wildcard * for all results,
                // and the sortField is also not originally specified, with sorting on last name to be in ascending order.
                var sortFieldOverride = sortField;
                var sortInDescendingOrderOverride = sortInDescendingOrder;
                if ((string.IsNullOrEmpty(query) || query.Trim() == "*") && string.IsNullOrEmpty(sortField))
                {
                    sortFieldOverride = "lastName";
                    sortInDescendingOrderOverride = false;
                }
#if SCIENCEMAKERSONLY
                var results = await this.azureSearch.BiographySearch(
                    (query == null) ? "" : query,
                    (pageSize == null) ? 20 : (int)pageSize,
                    (currentPage == null) ? 1 : (int)currentPage,
                    (searchFields == null) ? "all" : searchFields,
                    (genderFacet == null) ? "" : genderFacet,
                    (yearFacet == null) ? "" : yearFacet,
                    (makerFacet == null) ? WebApiApplication.SCIENCEMAKER_FLAG : WebApiApplication.SCIENCEMAKER_FLAG,
                    (jobFacet == null) ? "" : jobFacet,
                    (lastInitialFacet == null) ? "" : lastInitialFacet,
                    (sortFieldOverride == null) ? "" : sortFieldOverride,
                    (sortInDescendingOrderOverride == null) ? false : (bool)sortInDescendingOrderOverride
                );
#else
                var results = await this.azureSearch.BiographySearch(
                    (query == null) ? "" : query,
                    (pageSize == null) ? 20 : (int)pageSize,
                    (currentPage == null) ? 1 : (int)currentPage,
                    (searchFields == null) ? "all" : searchFields,
                    (genderFacet == null) ? "" : genderFacet,
                    (yearFacet == null) ? "" : yearFacet,
                    (makerFacet == null) ? "" : makerFacet,
                    (jobFacet == null) ? "" : jobFacet,
                    (lastInitialFacet == null) ? "" : lastInitialFacet,
                    (sortFieldOverride == null) ? "" : sortFieldOverride,
                    (sortInDescendingOrderOverride == null) ? false : (bool)sortInDescendingOrderOverride
                );
#endif

                var toc = new BiographyResultSet
                {
                    Facets = results.Facets,
                    Biographies = results.Results,
                    Count = results.Count
                };

                var response = Request.CreateResponse(HttpStatusCode.OK, toc, Configuration.Formatters.JsonFormatter);
                response.Headers.CacheControl = new CacheControlHeaderValue() { MaxAge = TimeSpan.FromDays(1), Public = true };

                return response;
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(ex.Message)
                };
            }
        }
    }
}