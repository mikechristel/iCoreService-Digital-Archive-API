using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Net.Http.Headers;

namespace iCoreService.Controllers
{
    /// <summary>
    /// Provides information about facets in use by the search engine.
    /// </summary>
    public class FacetListController : ApiController
    {
        private static string json;

        static FacetListController()
        {
            string path = System.Web.Hosting.HostingEnvironment.MapPath(@"~/App_Data/FacetList.json");
            json = File.ReadAllText(path);
        }

        /// <summary>
        /// Get the list mapping search facet identifiers to readable descriptions.
        /// </summary>
        /// <returns>A FacetList document containing all possible values.</returns>
        [ResponseType(typeof(iCoreService.Models.FacetList))]
        public HttpResponseMessage GetFacetList()
        {
            try
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };

                response.Content.Headers.ContentLength = json.Length;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
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
