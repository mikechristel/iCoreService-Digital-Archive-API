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
    /// Provides a list of biographies (people) born this day.
    /// </summary>
    public class PeopleBornThisDayController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();

        /// <summary>
        /// Retrieve a list of people born this day with optional filtering.
        /// </summary>
        /// <param name="pageSize">Number of results to return.</param>
        /// <param name="currentPage">Retreive the nth page of results based on given page size.</param>
        /// <param name="genderFacet">Filter results by the given gender.</param>
        /// <param name="yearFacet">Filter results by the given year.</param>
        /// <param name="makerFacet">Filter results by the given comma-separated list of maker categories.</param>
        /// <param name="jobFacet">Filter results by the given comma-separated list of job types.</param>
        /// <param name="lastInitialFacet">Filter results by first letter of last name, e.g., C is filtering to all last names starting with C.</param>
        /// <param name="dateTodayFacet">Today's date.</param>
        /// <returns>
        /// A BiographyResultSet containing the results of the search operation.
        /// </returns>        
        [ResponseType(typeof(BiographyResultSet))]
        public async Task<HttpResponseMessage> GetPeopleBornThisDay(int? pageSize = 20,
                                                                     int? currentPage = 1,
                                                                     string genderFacet = "",
                                                                     string yearFacet = "",
                                                                     string makerFacet = "",
                                                                     string jobFacet = "",
                                                                     string lastInitialFacet = "",
                                                                     string dateTodayFacet = "")
        {
            try
            {
                var results = await this.azureSearch.GetPeopleBornThisDay(
                    (pageSize == null) ? 20 : (int)pageSize,
                    (currentPage == null) ? 1 : (int)currentPage,
                    (genderFacet == null) ? "" : genderFacet,
                    (yearFacet == null) ? "" : yearFacet,
                    (makerFacet == null) ? "" : makerFacet,
                    (jobFacet == null) ? "" : jobFacet,
                    (lastInitialFacet == null) ? "" : lastInitialFacet,
                    (dateTodayFacet == null) ? "" : dateTodayFacet
                );

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
