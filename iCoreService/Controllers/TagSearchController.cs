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
    /// Provides data to the tags (aka HistoryMaker) view.
    /// </summary>
    public class TagSearchController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();

        /// <summary>
        /// Search for stories containing the given list of tags.
        /// </summary>
        /// <param name="csvTagList">A comma-separated list of tag identifiers.</param>
        /// <param name="pageSize">Number of results to return.</param>
        /// <param name="currentPage">Retreive the nth page of results based on given page size.</param>
        /// <param name="genderFacet">Filter results by the given gender.</param>
        /// <param name="yearFacet">Filter results by the given year.</param>
        /// <param name="makerFacet">Filter results by the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filter results by the given comma-separated list of job types.</param>
        /// <returns>
        /// </returns>
        [ResponseType(typeof(StoryResultSet))]
        public async Task<HttpResponseMessage> GetTagSearch(string csvTagList, 
                                                            int? pageSize = 20,
                                                            int? currentPage = 1,
                                                            string genderFacet = "",
                                                            string yearFacet = "",
                                                            string makerFacet = "",
                                                            string jobFacet = "")
        {
            try
            {
                var results = await this.azureSearch.StorySearchByTags(
                    (csvTagList == null) ? "" : csvTagList,
                    (pageSize == null) ? 20 : (int)pageSize,
                    (currentPage == null) ? 1 : (int)currentPage,
                    (genderFacet == null) ? string.Empty : genderFacet,
                    (yearFacet == null) ? string.Empty : yearFacet,
                    (makerFacet == null) ? string.Empty : makerFacet,
                    (jobFacet == null) ? string.Empty : jobFacet
                );

                var stories = new StoryResultSet
                {
                    Facets = results.Facets,
                    Stories = results.Results,
                    Count = results.Count
                };

                var response = Request.CreateResponse(HttpStatusCode.OK, stories, Configuration.Formatters.JsonFormatter);
#if !NO_CACHE_FOR_TESTING_API
                response.Headers.CacheControl = new CacheControlHeaderValue() { MaxAge = TimeSpan.FromDays(1), Public = true };
#endif
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
