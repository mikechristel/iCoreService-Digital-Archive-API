using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Description;
using System.Threading.Tasks;

using iCoreService.Models;
using iCoreService.AzureServices;

namespace iCoreService.Controllers
{
    /// <summary>
    /// Provide data to the story search view.
    /// </summary>
    public class StorySearchController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();

        /// <summary>
        /// Performs full-text search of the stories index.
        /// </summary>
        /// <param name="query">The query terms.</param>
        /// <param name="pageSize">Number of results to return.</param>
        /// <param name="currentPage">Retreive the nth page of results based on given page size.</param>
        /// <param name="parentBiographyID">Only retrieve stories belonging to this biography (if the ID is non-empty).</param>
        /// <param name="searchFields">Comma-separated list of fields to be searched.</param>
        /// <param name="interviewYearFilterLowerBound">Only return stories with interview date on or after this year (if given).</param>
        /// <param name="interviewYearFilterUpperBound">Only return stories with interview date on or before this year (if given).</param>
        /// <param name="genderFacet">Filter results by the given gender.</param>
        /// <param name="yearFacet">Filter results by the given year.</param>
        /// <param name="makerFacet">Filter results by the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filter results by the given comma-separated list of job types.</param>
        /// <param name="sortField">Sort results by this field, e.g., interviewDate; default is empty (meaning query relevance ranking is used to sort).</param>
        /// <param name="sortInDescendingOrder">If sortField is given, sort results in descending order if true; default is false so default sort order is ascending.</param>
        /// <returns>
        /// A StoryResultSet document containing the results of the search operation.
        /// </returns>        
        /// <remarks>
        /// Optional parameters are created by assigning them default values.
        /// Early versions had a searchMode parameter of any/all with default "any" BUT advice from
        /// https://docs.microsoft.com/en-us/rest/api/searchservice/simple-query-syntax-in-azure-search
        /// ...notes that negated terms will not be understood with match any, e.g., that buffalo -soldier will return 10000s of
        /// stories, all those NOT containing soldier unioned with all the stories matching buffalo.  Instead, to get
        /// buffalo AND not soldier, the underlying searchMode should be all.  The same is true for when complex
        /// queries are given with precedence and with and (+) and or (|) as in buffalo+(soldier|bill).  This is interpreted 
        /// fine as long as the underlying searchMode is all (not any).  Hence, it is now locked down to "all."
        /// Also, if searchFields is "all" or not specified, it will be taken to be title,transcript.
        /// </remarks>
        [ResponseType(typeof(StoryResultSet))]
        public async Task<HttpResponseMessage> GetStorySearch(string query = "", 
                                                              int? pageSize = 20, 
                                                              int? currentPage = 1, 
                                                              string parentBiographyID = "", 
                                                              string searchFields = "all", 
                                                              int? interviewYearFilterLowerBound = 0,
                                                              int? interviewYearFilterUpperBound = 0,
                                                              string genderFacet = "", 
                                                              string yearFacet = "", 
                                                              string makerFacet = "", 
                                                              string jobFacet = "",
                                                              string sortField = "",
                                                              bool? sortInDescendingOrder = false)
        {
            try
            {
                var results = await this.azureSearch.StorySearch(
                    (query == null) ? string.Empty : query,
                    (pageSize == null) ? 20 : (int)pageSize,
                    (currentPage == null) ? 1 : (int)currentPage,
                    (parentBiographyID == null) ? string.Empty : parentBiographyID,
                    (searchFields == null) ? "all" : searchFields,
                    (interviewYearFilterLowerBound == null) ? 0 : (int)interviewYearFilterLowerBound,
                    (interviewYearFilterUpperBound == null) ? 0 : (int)interviewYearFilterUpperBound,
                    (genderFacet == null) ? string.Empty : genderFacet,
                    (yearFacet == null) ? string.Empty : yearFacet,                    
                    (makerFacet == null) ? string.Empty : makerFacet,
                    (jobFacet == null) ? string.Empty : jobFacet,
                    (sortField == null) ? "" : sortField,
                    (sortInDescendingOrder == null) ? false : (bool)sortInDescendingOrder
                );

                var stories = new StoryResultSet
                {
                    Facets = results.Facets,
                    Stories = results.Results,
                    Count = results.Count
                };

                var response = Request.CreateResponse(HttpStatusCode.OK, stories, Configuration.Formatters.JsonFormatter);
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
