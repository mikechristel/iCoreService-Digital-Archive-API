using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Threading.Tasks;

using iCoreService.AzureServices;

namespace iCoreService.Controllers
{
    /// <summary>
    /// Provide type-ahead hints for the biography search box.
    /// </summary>
    public class BiographySearchSuggestionController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();

        /// <summary>
        /// Provide type-ahead hints for the biography search box.
        /// </summary>
        /// <param name="term">Contents of the search box so far.</param>
        /// <returns>As list of query suggestions.</returns>
        [ResponseType(typeof(List<string>))]
        public async Task<HttpResponseMessage> GetBiographySearchSuggestion(string term)
        {
            try
            {
                var suggestionSet = await azureSearch.BiographySuggest(term, false);

                var suggestions = new List<string>();
                foreach(var suggestion in suggestionSet.Results)
                {
                    suggestions.Add(suggestion.Text);
                }

                var response = Request.CreateResponse(HttpStatusCode.OK, suggestions, Configuration.Formatters.JsonFormatter);

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
