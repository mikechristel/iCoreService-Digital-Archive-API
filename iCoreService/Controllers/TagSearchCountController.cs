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
    public class TagSearchCountController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();

        /// <summary>
        /// Return the number of stories matching each tag filtered by the given list of tags.
        /// </summary>
        /// <param name="csvTagList">A comma-separated list of tag identifiers.</param>
        /// <returns>A StoryResultSet containing the results of the search operation.</returns>
        [ResponseType(typeof(StoryResultSet))]
        public async Task<HttpResponseMessage> GetTagSearchCount(string csvTagList)
        {
            try
            {
                var results = await this.azureSearch.StorySearchTagCounts(csvTagList);

                var stories = new StoryResultSet
                {
                    Facets = results.Facets,
                    Stories = null,
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
