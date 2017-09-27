using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Description;
using System.Threading.Tasks;

using Microsoft.Azure.Search.Models;

using iCoreService.AzureServices;
using iCoreService.Models;

using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace iCoreService.Controllers
{
    /// <summary>
    /// Provides data to the story details (aka video) view via accession,
    /// session order, tape order, story order sequence information as parameters.
    /// </summary>
    /// <remarks>
    /// StoryIDViaSequencing formerly presumed that sessionOrder of 1 meant first 
    /// session in the data; but there could be data items published such that the
    /// first session has sessionOrder == 2 or some other non-1 value.  Now the 
    /// values of sessionOrder are inspected when finding the correct story.
    /// </remarks>
    public class StoryDetailsViaSequenceController : ApiController
    {
        private AzureSearch azureSearch = new AzureSearch();
        private AzureStorage azureStorage = new AzureStorage();

        /// <summary>
        /// Get full details for the specified story.
        /// </summary>
        /// <param name="accession">Identifier for the biography (i.e., accession)</param>
        /// <param name="sessionOrder">Session order (first session may or may not be 1)</param>
        /// <param name="tapeOrder">Tape order (first tape is 1 but first in a session may not be 1)</param>
        /// <param name="storyOrder">Story order (first story is 1)</param>
        /// <returns>
        /// The StoryDetails document for the requested story.
        /// </returns>
        [ResponseType(typeof(StoryDetails))]
        public async Task<HttpResponseMessage> GetStoryDetails(string accession = "",
            string sessionOrder = "", string tapeOrder = "", string storyOrder = "")
        {
            if (string.IsNullOrEmpty(accession) ||
                string.IsNullOrEmpty(sessionOrder) ||
                string.IsNullOrEmpty(tapeOrder) ||
                string.IsNullOrEmpty(storyOrder)
                )
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            Int16 sessionOrderAsNumber, tapeOrderNumber, storyOrderNumber;
            if (!Int16.TryParse(sessionOrder, out sessionOrderAsNumber) ||
                !Int16.TryParse(tapeOrder, out tapeOrderNumber) ||
                !Int16.TryParse(storyOrder, out storyOrderNumber)
                )
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            // Of course, this bounds check is only testing a valid lower bound of >= 1.
            // The upper bound is not known a priori.
            if (sessionOrderAsNumber <= 0 || tapeOrderNumber <= 0 || storyOrderNumber <= 0)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            try
            {
                var storyID = await StoryIDViaSequencing(accession,
                    sessionOrderAsNumber, tapeOrderNumber, storyOrderNumber);
                if (string.IsNullOrEmpty(storyID))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                var result = await azureStorage.GetStoryDetails(storyID);

                if (result == null)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                result.BlobStream.Position = 0;

                // NOTE: no query terms allowed for not in this call, so return the raw json
                // without the need to fold in query matches...
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

        /// <summary>
        /// Helper function to return a story ID based on given parameters, "" for not found.
        /// </summary>
        /// <param name="accession">Accession ID for the parent biography</param>
        /// <param name="sessionOrderAsNumber">Session order (first session may or may not be 1), assumed valued >= 1</param>
        /// <param name="tapeOrderNumber">Tape order (note that first tape in a session may not be 1), assumed valued >= 1</param>
        /// <param name="storyOrderNumber">Story sequence order (first story in a tape is 1), assumed valued >= 1</param>
        /// <returns>Story ID if one is found matching parameters, else ""</returns>
        private async Task<string> StoryIDViaSequencing(string accession,
                    Int16 sessionOrderAsNumber, Int16 tapeOrderNumber, Int16 storyOrderNumber)
        {
            var retVal = "";

            // NOTE:  This is NOT optimized for performance.  It uses GetBiographyDetails.
            // It walks the response to find the storyID corresponding to the given parameters.
            // It would be nicer to have more direct access, but callers wishing for direct access should
            // be using GetStoryDetails with a storyID rather than GetStoryDetails with accession, 
            // session, tape, and story order numbers: the latter is provided for a "clean" way to access
            // data from an external caller, knowing that storyIDs are more ephemeral.  Such callers should 
            // expect slower performance in exchange for the clarity of parameters not using storyID.
            var result = await azureStorage.GetBiographyDetails(accession);

            if (result == null)
            {
                return retVal;
            }

            result.BlobStream.Position = 0;
            var reader = new StreamReader(result.BlobStream);

            JsonSerializer serializer = new JsonSerializer();
            BiographyDetails bioDetails = (BiographyDetails)serializer.Deserialize(reader, typeof(BiographyDetails));
#if SCIENCEMAKERSONLY
            // "Eat" the result unless its makers include WebApiApplication.SCIENCEMAKER_FLAG which is the code for ScienceMakers
            if (bioDetails != null)
            {
                var isScienceMaker = false;
                var makerList = bioDetails.MakerCategories;
                for (int makerIndex = 0; makerIndex < makerList.Length && !isScienceMaker; makerIndex++)
                {
                    if (makerList[makerIndex] == WebApiApplication.SCIENCEMAKER_FLAG)
                        isScienceMaker = true; // this person is a confirmed ScienceMaker and can be returned by the call
                }
                if (!isScienceMaker)
                {
                    bioDetails = null; // forget non-ScienceMakers
                    retVal = null;
                }
            }
#endif
            if (bioDetails != null)
            {
                for (int sessionIndex = 0; sessionIndex < bioDetails.Sessions.Count; sessionIndex++)
                {
                    // NOTE:  Session order does NOT correspond to index i for bioDetails.Sessions[i],
                    // e.g., the first and only session might have sessionOrder == 2, so we do a slow walk 
                    // here checking for the given session order value.
                    if (bioDetails.Sessions[sessionIndex].SessionOrder == sessionOrderAsNumber)
                    { // Found the desired session.  Look within it for the desired tape and story.
                        BiographySession oneSession = bioDetails.Sessions[sessionIndex];
                        // NOTE:  tape number does NOT correspond to index i for oneSession.Tapes[i],
                        // so we do a slow walk here checking for the given tape order value.
                        for (int tapeIndex = 0; tapeIndex < oneSession.Tapes.Count; tapeIndex++)
                        {
                            if (oneSession.Tapes[tapeIndex].TapeOrder != null &&
                                oneSession.Tapes[tapeIndex].TapeOrder == tapeOrderNumber)
                            {
                                // Found the correct tape.  See if given story is within it.
                                // NOTE:  Within a tape, story order corresponds to index, i.e., 
                                // story 1 is first in the tape's Stories set,
                                // story 2 is second in tape's Stories set, etc.
                                if (oneSession.Tapes[tapeIndex].Stories.Count >= storyOrderNumber)
                                {
                                    retVal = oneSession.Tapes[tapeIndex].Stories[storyOrderNumber - 1].StoryID; // found the value!
                                }
                                break; // of course, retVal might still be "" here on bogus storyOrderNumber 
                            }
                        }
                    }
                }
            }

            return retVal;
        }

    }
}
