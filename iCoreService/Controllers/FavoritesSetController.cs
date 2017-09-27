using System;
using System.Collections.Generic;
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
    /// Provides data to the starred stories (a.k.a favorites) view.
    /// </summary>
    public class FavoritesSetController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();

        /// <summary>
        /// Retrieve an ordered set of stories.
        /// </summary>
        /// <param name="csvStoryIDs">The query terms.</param>
        /// <param name="genderFacet">Filter results by the given gender.</param>
        /// <param name="yearFacet">Filter results by the given year.</param>
        /// <param name="makerFacet">Filter results by the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filter results by the given comma-separated list of job types.</param>
        /// <returns>
        /// A StoryResultSet containing the stories specified by the given list.
        /// </returns>        
        [ResponseType(typeof(StoryResultSet))]
        public async Task<HttpResponseMessage> GetFavoritesSet( string csvStoryIDs = "",
                                                                string genderFacet = "",
                                                                string yearFacet = "",
                                                                string makerFacet = "",
                                                                string jobFacet = "")
    {
            var storyList = new List<string>();
                
            foreach(var item in csvStoryIDs.Split(','))
            {
                var id = item.Trim();
                if (id != "") storyList.Add(id);
            }

            try
            {
                var results = await this.azureSearch.StorySet(
                    storyList, 
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
