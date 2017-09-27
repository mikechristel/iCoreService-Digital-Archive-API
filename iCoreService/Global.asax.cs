using System.Linq;
using System.Web.Http;

namespace iCoreService
{
    /// <summary>
    /// The applicaption
    /// </summary>
    public class WebApiApplication : System.Web.HttpApplication
    {
#if SCIENCEMAKERSONLY
        /// <summary>
        /// The "maker" string value for ScienceMaker, used to subset data in a ScienceMaker-specific way.
        /// </summary>
        public static string SCIENCEMAKER_FLAG = "37"; // NOTE: value actually defined within FacetList.json; shared with all here as convenience
#endif
        /// <summary>
        /// It all starts here
        /// </summary>
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        /// <summary>
        /// Application BeginRequest
        /// </summary>
        /// <remarks>This code added to take care of OPTIONS requests for CORS.  The following explanation is taken verbatim from
        /// http://stackoverflow.com/questions/27504256/mvc-web-api-no-access-control-allow-origin-header-is-present-on-the-requested :
        /// "You also need to handle the preflight Options requests with HTTP OPTIONS requests.
        /// Web API needs to respond to the Options request in order to confirm that it is indeed configured to support CORS.
        /// To handle this, all you need to do is send an empty response back.You can do this inside your actions, or you can do it globally like this:"
        /// The necessity for this function to be included in order for CORS to work is noted by the June 17, 2016 comment to this article:
        /// http://www.asp.net/web-api/overview/security/enabling-cross-origin-requests-in-web-api
        /// Hence, the code is left here:</remarks>
        protected void Application_BeginRequest()
        {
            if (Request.Headers.AllKeys.Contains("Origin") && Request.HttpMethod == "OPTIONS")
            {
                Response.Flush();
            }
        }
    }
}
