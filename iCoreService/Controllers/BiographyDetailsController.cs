using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Description;
using System.Threading.Tasks;

using iCoreService.AzureServices;

#if SCIENCEMAKERSONLY
using System.IO; // needed for StreamReader
using Newtonsoft.Json; // needed for JsonSerializer
#endif

namespace iCoreService.Controllers
{
    /// <summary>
    /// Provide data to the biography details view.
    /// </summary>
    public class BiographyDetailsController : ApiController
    {
        private AzureStorage azureStorage = new AzureStorage();

        /// <summary>
        /// Get full details for the specified biography.
        /// </summary>
        /// <param name="accession">A unique biography identifier.</param>
        /// <returns>
        /// The BiographyDetails document for the requested biography.
        /// </returns>
        [ResponseType(typeof(iCoreService.Models.BiographyDetails))]
        public async Task<HttpResponseMessage> GetBiographyDetails(string accession = "")
        {
            if (string.IsNullOrEmpty(accession))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            try
            {
                var result = await azureStorage.GetBiographyDetails(accession);
#if SCIENCEMAKERSONLY
                // "Eat" the result unless its makers include WebApiApplication.SCIENCEMAKER_FLAG which is the code for ScienceMakers
                if (result != null)
                {
                    var isScienceMaker = false;
                    result.BlobStream.Position = 0;
                    var reader = new StreamReader(result.BlobStream);

                    JsonSerializer serializer = new JsonSerializer();
                    iCoreService.Models.BiographyDetails bioDetails = (iCoreService.Models.BiographyDetails)serializer.Deserialize(reader, typeof(iCoreService.Models.BiographyDetails));
                    if (bioDetails != null)
                    {
                        var makerList = bioDetails.MakerCategories;
                        for (int makerIndex = 0; makerIndex < makerList.Length && !isScienceMaker; makerIndex++)
                        {
                            if (makerList[makerIndex] == WebApiApplication.SCIENCEMAKER_FLAG)
                                isScienceMaker = true; // this person is a confirmed ScienceMaker and can be returned by the call
                        }
                    }
                    if (!isScienceMaker)
                        result = null; // forget non-ScienceMakers
                }
#endif
                if (result == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                result.BlobStream.Position = 0;

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(result.BlobStream)
                };

                response.Content.Headers.ContentLength = result.BlobLength;
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(result.BlobContentType);
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
