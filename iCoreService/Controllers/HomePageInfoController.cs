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
    /// Provides data to the home view.
    /// </summary>
    public class HomePageInfoController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();

        /// <summary>
        /// Retrieve corpus statistics used to populate the home view.
        /// </summary>
        /// <returns>
        /// A JSON document with information regarding the number of biographies and stories.
        /// </returns>
        [ResponseType(typeof(HomePageInfo))]
        public async Task<HttpResponseMessage> GetHomePageInfo()
        {
            try
            {
                HomePageInfo homePageInfo = await azureSearch.HomePageInfo();

                var response = Request.CreateResponse(HttpStatusCode.OK, homePageInfo, Configuration.Formatters.JsonFormatter);
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
