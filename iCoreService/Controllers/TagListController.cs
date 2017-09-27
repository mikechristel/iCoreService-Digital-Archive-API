using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Description;

namespace iCoreService.Controllers
{
    /// <summary>
    /// Provides data to the tags (aka HistoryMaker) view.
    /// </summary>
    public class TagListController : ApiController
    {
        private static string json;

        /// <summary>
        /// Static constructor
        /// </summary>
        static TagListController()
        {
            string path = System.Web.Hosting.HostingEnvironment.MapPath(@"~/App_Data/TagList.json");
            json = File.ReadAllText(path);
        }

        /// <summary>
        /// Retrieve the list of tags used by the stories index.
        /// </summary>
        /// <returns>A JSON object containing the mapping.</returns>
        [ResponseType(typeof(iCoreService.Models.TagTree))]
        public HttpResponseMessage GetTagList()
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
